using System.Diagnostics;
using System.Text.Json;
using CocktailOS.Kiosk.Contracts;

namespace CocktailOS.Kiosk.Services;

public sealed class ApplicationUpdateService(HttpClient client, ILogger<ApplicationUpdateService> logger)
{
    private const string ReleasesUrl = "https://api.github.com/repos/CocktailOS/Kiosk/releases?per_page=50";
    private const string UpdateScriptPath = "/usr/local/lib/cocktailos-kiosk/update";
    private const string SudoersPath = "/etc/sudoers.d/cocktailos-kiosk-update";
    private const string SudoPath = "/usr/bin/sudo";
    private const string SystemctlPath = "/usr/bin/systemctl";
    private const string UpdateServiceName = "cocktailos-kiosk-update.service";

    public async Task<ApplicationUpdateResponse> GetStatusAsync(string currentVersion, CancellationToken cancellationToken)
    {
        if (!CanInstallUpdates() || !SemanticVersion.TryParse(currentVersion, out var current) || current is null)
            return new(false, currentVersion, null, null);

        try
        {
            using var response = await client.GetAsync(ReleasesUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));

            foreach (var release in document.RootElement.EnumerateArray())
            {
                if (release.GetProperty("draft").GetBoolean()) continue;
                var tag = release.GetProperty("tag_name").GetString();
                if (string.IsNullOrWhiteSpace(tag) || !SemanticVersion.TryParse(tag, out var candidate) || candidate is null || candidate.CompareTo(current) <= 0) continue;
                var releaseUrl = release.TryGetProperty("html_url", out var url) ? url.GetString() : null;
                return new(true, currentVersion, tag, releaseUrl);
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            logger.LogWarning(exception, "GitHub-Releases konnten nicht auf Updates geprüft werden.");
        }

        return new(false, currentVersion, null, null);
    }

    public bool TryStartUpdate(out string? error)
    {
        error = null;
        if (!CanInstallUpdates())
        {
            error = "Die automatische Aktualisierung ist auf diesem Gerät nicht eingerichtet.";
            return false;
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = SudoPath,
                UseShellExecute = false,
                ArgumentList = { "-n", SystemctlPath, "start", "--no-block", UpdateServiceName }
            });
            if (process is null)
            {
                error = "Der Aktualisierungsdienst konnte nicht gestartet werden.";
                return false;
            }

            if (!process.WaitForExit(TimeSpan.FromSeconds(5)))
            {
                error = "Der Aktualisierungsdienst antwortet nicht.";
                return false;
            }
            if (process.ExitCode == 0) return true;
            error = "Der Aktualisierungsdienst konnte nicht gestartet werden.";
            return false;
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            logger.LogError(exception, "Der Aktualisierungsdienst konnte nicht gestartet werden.");
            error = "Der Aktualisierungsdienst konnte nicht gestartet werden.";
            return false;
        }
    }

    private static bool CanInstallUpdates() => OperatingSystem.IsLinux()
        && File.Exists(UpdateScriptPath)
        && File.Exists(SudoersPath)
        && File.Exists(SudoPath)
        && File.Exists(SystemctlPath);
}
