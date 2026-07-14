using CocktailOS.Kiosk.Contracts;
using CocktailOS.Kiosk.Data;
using CocktailOS.Kiosk.Models;
using CocktailOS.Kiosk.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CocktailOS.Kiosk.Endpoints;

public static class SystemEndpointExtensions
{
    public static RouteGroupBuilder MapSystemEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/system", GetAsync); api.MapPut("/system", UpdateAsync); api.MapPut("/system/theme", UpdateThemeAsync); api.MapGet("/app-info", GetApplicationInfo);
        return api;
    }

    private static IResult GetApplicationInfo()
    {
        var version = System.Reflection.Assembly.GetEntryAssembly()?
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>().SingleOrDefault()?.InformationalVersion;
        return Results.Ok(new ApplicationInfoResponse(string.IsNullOrWhiteSpace(version) ? "0.0.0" : version.Split('+')[0]));
    }

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
