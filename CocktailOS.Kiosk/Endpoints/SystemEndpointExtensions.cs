using CocktailOS.Kiosk.Contracts;
using CocktailOS.Kiosk.Data;
using CocktailOS.Kiosk.Models;
using CocktailOS.Kiosk.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace CocktailOS.Kiosk.Endpoints;

public static class SystemEndpointExtensions
{
    public static RouteGroupBuilder MapSystemEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/system", GetAsync); api.MapPut("/system", UpdateAsync); api.MapPut("/system/theme", UpdateThemeAsync); api.MapGet("/app-info", GetApplicationInfo); api.MapGet("/app-update", GetApplicationUpdateAsync); api.MapPost("/app-update", StartApplicationUpdateAsync);
        return api;
    }

    private static IResult GetApplicationInfo() => Results.Ok(new ApplicationInfoResponse(GetApplicationVersion()));

    private static async Task<IResult> GetApplicationUpdateAsync(HttpContext context, ApplicationUpdateService updateService, CancellationToken ct)
    {
        if (!IsLocalRequest(context)) return Results.Forbid();
        return Results.Ok(await updateService.GetStatusAsync(GetApplicationVersion(), ct));
    }

    private static async Task<IResult> StartApplicationUpdateAsync(HttpContext context, ApplicationUpdateService updateService, CancellationToken ct)
    {
        if (!IsLocalRequest(context)) return Results.Forbid();
        var update = await updateService.GetStatusAsync(GetApplicationVersion(), ct);
        if (!update.IsAvailable || string.IsNullOrWhiteSpace(update.LatestVersion))
            return Results.Conflict(new ProblemDetails { Title = "Kein Update verfügbar", Detail = "Die Anwendung ist bereits aktuell oder die Update-Prüfung ist nicht verfügbar." });
        if (!updateService.TryStartUpdate(out var error))
            return Results.Problem(title: "Update konnte nicht gestartet werden", detail: error, statusCode: StatusCodes.Status503ServiceUnavailable);
        return Results.Accepted("/api/app-update", new ApplicationUpdateStartResponse(update.LatestVersion));
    }

    private static string GetApplicationVersion()
    {
        var version = System.Reflection.Assembly.GetEntryAssembly()?
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>().SingleOrDefault()?.InformationalVersion;
        return string.IsNullOrWhiteSpace(version) ? "0.0.0" : version.Split('+')[0];
    }

    private static bool IsLocalRequest(HttpContext context) => context.Connection.RemoteIpAddress is { } address && IPAddress.IsLoopback(address);

    private static async Task<IResult> GetAsync(AppDbContext db, CancellationToken ct)
    {
        var configuration = await db.MachineConfigurations.AsNoTracking().SingleAsync(x => x.Id == MachineConfiguration.SingletonId, ct);
        return Results.Ok(ToResponse(configuration));
    }

    private static async Task<IResult> UpdateAsync(MachineConfigurationRequest request, AppDbContext db, DispenseService dispenseService, CancellationToken ct)
    {
        if (dispenseService.IsRunning) return Results.Conflict(new ProblemDetails { Title = "Ausschank aktiv", Detail = "Die Hardwarekonfiguration kann während eines Ausschanks nicht geändert werden." });
        var driver = PumpDriverNames.All.SingleOrDefault(x => x.Equals(request.PumpDriver, StringComparison.OrdinalIgnoreCase));
        var scheme = PinNumberingSchemes.All.SingleOrDefault(x => x.Equals(request.PinNumberingScheme, StringComparison.OrdinalIgnoreCase));
        var theme = ThemeNames.All.SingleOrDefault(x => x.Equals(request.Theme, StringComparison.OrdinalIgnoreCase));
        if (driver is null || scheme is null || theme is null) return EndpointResults.Validation("configuration", "Treiber muss Dummy oder Gpio, die Nummerierung Logical oder Board und das Design Light oder Dark sein.");
        var configuration = await db.MachineConfigurations.SingleAsync(x => x.Id == MachineConfiguration.SingletonId, ct);
        configuration.PumpDriver = driver; configuration.PinNumberingScheme = scheme; configuration.Theme = theme;
        await db.SaveChangesAsync(ct); return Results.Ok(ToResponse(configuration));
    }

    private static async Task<IResult> UpdateThemeAsync(ThemeRequest request, AppDbContext db, CancellationToken ct)
    {
        var theme = ThemeNames.All.SingleOrDefault(x => x.Equals(request.Theme, StringComparison.OrdinalIgnoreCase));
        if (theme is null) return EndpointResults.Validation("theme", "Das Design muss Light oder Dark sein.");
        var configuration = await db.MachineConfigurations.SingleAsync(x => x.Id == MachineConfiguration.SingletonId, ct);
        configuration.Theme = theme; await db.SaveChangesAsync(ct); return Results.Ok(ToResponse(configuration));
    }

    private static MachineConfigurationResponse ToResponse(MachineConfiguration configuration) => new(configuration.PumpDriver, configuration.PinNumberingScheme, configuration.Theme, DispenseService.MaximumParallelPumps);
}
