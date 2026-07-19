using CocktailOS.Kiosk.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;

namespace CocktailOS.Kiosk.Services;

public sealed class BackupService(IWebHostEnvironment environment, IServiceScopeFactory scopeFactory)
{
    private const string DatabaseFileName = "cocktailos.db";
    private const long MaximumArchiveBytes = 100 * 1024 * 1024;
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    private string UploadDirectory => Path.Combine(environment.WebRootPath, "uploads");

    public async Task<(Stream Stream, string FileName)> CreateAsync(CancellationToken ct)
    {
        var stream = new MemoryStream();
        var databasePath = await GetDatabasePathAsync(ct);
        var dataDirectory = Path.GetDirectoryName(databasePath)!;
        var snapshotDirectory = Path.Combine(dataDirectory, $".backup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(snapshotDirectory);
        try
        {
            var snapshotPath = Path.Combine(snapshotDirectory, DatabaseFileName);
            await CreateDatabaseSnapshotAsync(databasePath, snapshotPath, ct);
            SqliteConnection.ClearAllPools();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                await AddFileAsync(archive, snapshotPath, DatabaseFileName, ct);
                if (Directory.Exists(UploadDirectory))
                    foreach (var imagePath in Directory.EnumerateFiles(UploadDirectory, "*", SearchOption.TopDirectoryOnly))
                        await AddFileAsync(archive, imagePath, $"uploads/{Path.GetFileName(imagePath)}", ct);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            TryDeleteDirectory(snapshotDirectory);
        }
        stream.Position = 0;
        return (stream, $"cocktailos-backup-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.zip");
    }

    public async Task RestoreAsync(IFormFile archiveFile, CancellationToken ct)
    {
        if (archiveFile.Length is <= 0 or > MaximumArchiveBytes || !Path.GetExtension(archiveFile.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Wähle eine gültige CocktailOS-Backupdatei im ZIP-Format bis 100 MB.");

        var databasePath = await GetDatabasePathAsync(ct);
        var dataDirectory = Path.GetDirectoryName(databasePath)!;
        var staging = Path.Combine(dataDirectory, $".restore-{Guid.NewGuid():N}");
        Directory.CreateDirectory(staging);
        try
        {
            var stagedArchive = Path.Combine(staging, "backup.zip");
            await using (var target = File.Create(stagedArchive)) await archiveFile.CopyToAsync(target, ct);
            using (var archive = ZipFile.OpenRead(stagedArchive))
            {
                var databaseFound = false;
                foreach (var entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue;
                    var normalized = entry.FullName.Replace('\\', '/');
                    if (normalized == DatabaseFileName) databaseFound = true;
                    else if (!normalized.StartsWith("uploads/", StringComparison.Ordinal) || normalized["uploads/".Length..].Contains('/') || !ImageExtensions.Contains(Path.GetExtension(entry.Name)))
                        throw new InvalidDataException("Die Backupdatei enthält einen ungültigen Eintrag.");
                    var outputPath = Path.GetFullPath(Path.Combine(staging, normalized));
                    if (!outputPath.StartsWith(Path.GetFullPath(staging) + Path.DirectorySeparatorChar, StringComparison.Ordinal)) throw new InvalidDataException("Ungültiger Dateipfad im Backup.");
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                    await using var output = File.Create(outputPath);
                    await using var input = entry.Open();
                    await input.CopyToAsync(output, ct);
                }
                if (!databaseFound || !File.Exists(Path.Combine(staging, DatabaseFileName))) throw new InvalidDataException("Die Backupdatei enthält keine CocktailOS-Datenbank.");
            }

            await ValidateDatabaseAsync(Path.Combine(staging, DatabaseFileName), ct);
            SqliteConnection.ClearAllPools();
            var safetyCopy = Path.Combine(dataDirectory, $"cocktailos-before-restore-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.db");
            File.Copy(databasePath, safetyCopy, overwrite: false);
            File.Copy(Path.Combine(staging, DatabaseFileName), databasePath, overwrite: true);

            var stagedUploads = Path.Combine(staging, "uploads");
            var previousUploads = Path.Combine(dataDirectory, $"uploads-before-restore-{DateTimeOffset.Now:yyyyMMdd-HHmmss}");
            if (Directory.Exists(UploadDirectory)) Directory.Move(UploadDirectory, previousUploads);
            if (Directory.Exists(stagedUploads)) Directory.Move(stagedUploads, UploadDirectory); else Directory.CreateDirectory(UploadDirectory);
        }
        finally
        {
            TryDeleteDirectory(staging);
        }
    }

    private static async Task AddFileAsync(ZipArchive archive, string sourcePath, string entryName, CancellationToken ct)
    {
        if (!File.Exists(sourcePath)) throw new FileNotFoundException("Die CocktailOS-Datenbank wurde nicht gefunden.", sourcePath);
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        await using var target = entry.Open();
        await using var source = File.OpenRead(sourcePath);
        await source.CopyToAsync(target, ct);
    }

    private static void TryDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
        catch (IOException) { }
    }

    private async Task CreateDatabaseSnapshotAsync(string databasePath, string snapshotPath, CancellationToken ct)
    {
        var sourceConnectionString = new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadWrite }.ToString();
        var targetConnectionString = new SqliteConnectionStringBuilder { DataSource = snapshotPath, Mode = SqliteOpenMode.ReadWriteCreate }.ToString();
        await using var source = new SqliteConnection(sourceConnectionString);
        await using var target = new SqliteConnection(targetConnectionString);
        await source.OpenAsync(ct);
        await target.OpenAsync(ct);
        source.BackupDatabase(target);
    }

    private async Task<string> GetDatabasePathAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.OpenConnectionAsync(ct);
        var path = db.Database.GetDbConnection().DataSource;
        await db.Database.CloseConnectionAsync();
        if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("Der Datenbankpfad konnte nicht ermittelt werden.");
        return Path.GetFullPath(path);
    }

    private static async Task ValidateDatabaseAsync(string path, CancellationToken ct)
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadOnly }.ToString();
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'MachineConfigurations';";
        if (await command.ExecuteScalarAsync(ct) is not string) throw new InvalidDataException("Die Datei ist keine gültige CocktailOS-Sicherung.");
    }
}
