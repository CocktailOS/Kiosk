using CocktailOS.Kiosk.Services;
using Microsoft.AspNetCore.Http;

namespace CocktailOS.Kiosk.Endpoints;

public static class BackupEndpointExtensions
{
    public static RouteGroupBuilder MapBackupEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/backup", DownloadAsync);
        api.MapPost("/backup/restore", RestoreAsync).DisableAntiforgery();
        return api;
    }

    private static async Task<IResult> DownloadAsync(BackupService backups, CancellationToken ct)
    {
        var (stream, fileName) = await backups.CreateAsync(ct);
        return Results.File(stream, "application/zip", fileName);
    }

    private static async Task<IResult> RestoreAsync(IFormFile file, BackupService backups, DispenseService dispense, CancellationToken ct)
    {
        if (dispense.IsRunning) return Results.Conflict(new { title = "Ausschank aktiv", detail = "Eine Wiederherstellung ist während eines aktiven Vorgangs nicht möglich." });
        try
        {
            await backups.RestoreAsync(file, ct);
            return Results.NoContent();
        }
        catch (InvalidDataException exception)
        {
            return Results.BadRequest(new { title = "Backup ungültig", detail = exception.Message });
        }
    }
}
