using CocktailOS.Kiosk.Contracts;
using CocktailOS.Kiosk.Data;
using CocktailOS.Kiosk.Models;
using CocktailOS.Kiosk.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace CocktailOS.Kiosk.Endpoints;

public static class SystemEndpointExtensions
{
    public static RouteGroupBuilder MapSystemEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/system", GetAsync);
        api.MapPut("/system", UpdateAsync);
        api.MapPut("/system/theme", UpdateThemeAsync);
        api.MapGet("/network-access/status", GetNetworkAccessStatusAsync);
        api.MapPost("/network-access/authenticate", AuthenticateNetworkAccessAsync);
        api.MapGet("/admin-access/status", GetAdminAccessStatusAsync);
        api.MapPost("/admin-access/setup", SetupAdminAccessAsync);
        api.MapPost("/admin-access/authenticate", AuthenticateAdminAccessAsync);
        api.MapPost("/admin-access/logout", LogoutAdminAccessAsync);
        api.MapGet("/app-info", GetApplicationInfo);
        api.MapGet("/app-update", GetApplicationUpdateAsync);
        api.MapPost("/app-update", StartApplicationUpdateAsync);
        return api;
    }

    private static IResult GetApplicationInfo() => Results.Ok(new ApplicationInfoResponse(GetApplicationVersion()));

    private static async Task<IResult> GetApplicationUpdateAsync(ApplicationUpdateService updateService,
        CancellationToken ct)
    {
        return Results.Ok(await updateService.GetStatusAsync(GetApplicationVersion(), ct));
    }

    private static async Task<IResult> StartApplicationUpdateAsync(ApplicationUpdateService updateService,
        CancellationToken ct)
    {
        var update = await updateService.GetStatusAsync(GetApplicationVersion(), ct);
        if (!update.IsAvailable || string.IsNullOrWhiteSpace(update.LatestVersion))
            return Results.Conflict(new ProblemDetails
            {
                Title = "Kein Update verfügbar",
                Detail = "Die Anwendung ist bereits aktuell oder die Update-Prüfung ist nicht verfügbar."
            });
        if (!updateService.TryStartUpdate(out var error))
            return Results.Problem(title: "Update konnte nicht gestartet werden", detail: error,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        return Results.Accepted("/api/app-update", new ApplicationUpdateStartResponse(update.LatestVersion));
    }

    private static string GetApplicationVersion()
    {
        var version = System.Reflection.Assembly.GetEntryAssembly()?
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>().SingleOrDefault()?.InformationalVersion;
        return string.IsNullOrWhiteSpace(version) ? "0.0.0" : version.Split('+')[0];
    }

    private static bool IsLocalRequest(HttpContext context) =>
        context.Connection.RemoteIpAddress is { } address && IPAddress.IsLoopback(address);

    private static async Task<IResult> GetAsync(AppDbContext db, CancellationToken ct)
    {
        var configuration = await db.MachineConfigurations.AsNoTracking()
            .SingleAsync(x => x.Id == MachineConfiguration.SingletonId, ct);
        return Results.Ok(ToResponse(configuration));
    }

    private static async Task<IResult> UpdateAsync(MachineConfigurationRequest request, AppDbContext db,
        DispenseService dispenseService, NetworkAccessPolicy networkAccessPolicy, CancellationToken ct)
    {
        if (dispenseService.IsRunning)
            return Results.Conflict(new ProblemDetails
            {
                Title = "Ausschank aktiv",
                Detail = "Die Hardwarekonfiguration kann während eines Ausschanks nicht geändert werden."
            });
        var driver =
            PumpDriverNames.All.SingleOrDefault(x => x.Equals(request.PumpDriver, StringComparison.OrdinalIgnoreCase));
        var scheme = PinNumberingSchemes.All.SingleOrDefault(x =>
            x.Equals(request.PinNumberingScheme, StringComparison.OrdinalIgnoreCase));
        var theme = ThemeNames.All.SingleOrDefault(x => x.Equals(request.Theme, StringComparison.OrdinalIgnoreCase));
        if (driver is null || scheme is null || theme is null)
            return EndpointResults.Validation("configuration",
                "Treiber muss Dummy oder Gpio, die Nummerierung Logical oder Board und das Design Light oder Dark sein.");
        var configuration =
            await db.MachineConfigurations.SingleAsync(x => x.Id == MachineConfiguration.SingletonId, ct);
        if (request.NetworkAccessEnabled && string.IsNullOrWhiteSpace(configuration.NetworkAccessPinHash))
            return EndpointResults.Validation("appPin",
                "Lege zuerst einen vierstelligen App-PIN fest, bevor du den Netzwerkzugriff aktivierst.");
        configuration.PumpDriver = driver;
        configuration.PinNumberingScheme = scheme;
        configuration.Theme = theme;
        configuration.NetworkAccessEnabled = request.NetworkAccessEnabled;
        await db.SaveChangesAsync(ct);
        networkAccessPolicy.SetEnabled(configuration.NetworkAccessEnabled);
        return Results.Ok(ToResponse(configuration));
    }

    private static async Task<IResult> UpdateThemeAsync(ThemeRequest request, AppDbContext db, CancellationToken ct)
    {
        var theme = ThemeNames.All.SingleOrDefault(x => x.Equals(request.Theme, StringComparison.OrdinalIgnoreCase));
        if (theme is null) return EndpointResults.Validation("theme", "Das Design muss Light oder Dark sein.");
        var configuration =
            await db.MachineConfigurations.SingleAsync(x => x.Id == MachineConfiguration.SingletonId, ct);
        configuration.Theme = theme;
        await db.SaveChangesAsync(ct);
        return Results.Ok(ToResponse(configuration));
    }

    private static async Task<IResult> GetNetworkAccessStatusAsync(HttpContext context, AppDbContext db,
        NetworkAccessSessionService sessions, CancellationToken ct)
    {
        var configuration = await db.MachineConfigurations.AsNoTracking()
            .SingleAsync(x => x.Id == MachineConfiguration.SingletonId, ct);
        var isRemoteRequest = !IsLocalRequest(context);
        var isAuthenticated = !isRemoteRequest || sessions.IsValid(context.Request.Cookies["CocktailOS.NetworkAccess"]);
        var pinConfigured = !string.IsNullOrWhiteSpace(configuration.NetworkAccessPinHash);
        return Results.Ok(new NetworkAccessStatusResponse(
            configuration.NetworkAccessEnabled,
            pinConfigured,
            isRemoteRequest && configuration.NetworkAccessEnabled && pinConfigured && !isAuthenticated,
            isAuthenticated,
            configuration.Theme));
    }

    private static async Task<IResult> AuthenticateNetworkAccessAsync(HttpContext context, NetworkPinRequest request,
        AppDbContext db, NetworkAccessPinService pinService, NetworkAccessSessionService sessions, CancellationToken ct)
    {
        if (IsLocalRequest(context)) return Results.NoContent();
        var configuration = await db.MachineConfigurations.AsNoTracking()
            .SingleAsync(x => x.Id == MachineConfiguration.SingletonId, ct);
        if (!configuration.NetworkAccessEnabled || !pinService.Verify(request.Pin, configuration.NetworkAccessPinHash))
            return Results.Unauthorized();

        context.Response.Cookies.Append("CocktailOS.NetworkAccess", sessions.Create(), new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            MaxAge = TimeSpan.FromHours(12),
            SameSite = SameSiteMode.Strict
        });
        return Results.NoContent();
    }

    private static async Task<IResult> GetAdminAccessStatusAsync(HttpContext context, AppDbContext db,
        NetworkAccessSessionService sessions, CancellationToken ct)
    {
        var configuration = await db.MachineConfigurations.AsNoTracking()
            .SingleAsync(x => x.Id == MachineConfiguration.SingletonId, ct);
        return Results.Ok(new AdminAccessStatusResponse(
            !string.IsNullOrWhiteSpace(configuration.NetworkAccessPinHash),
            sessions.IsValid(context.Request.Cookies["CocktailOS.AdminAccess"])));
    }

    private static async Task<IResult> SetupAdminAccessAsync(HttpContext context, NetworkPinRequest request,
        AppDbContext db, NetworkAccessPinService pinService, NetworkAccessSessionService sessions, CancellationToken ct)
    {
        if (!pinService.IsValid(request.Pin))
            return EndpointResults.Validation("pin", "Der App-PIN muss aus genau vier Ziffern bestehen.");
        var configuration =
            await db.MachineConfigurations.SingleAsync(x => x.Id == MachineConfiguration.SingletonId, ct);
        if (!string.IsNullOrWhiteSpace(configuration.NetworkAccessPinHash))
            return Results.Conflict(new ProblemDetails { Title = "App-PIN bereits eingerichtet" });
        configuration.NetworkAccessPinHash = pinService.Hash(request.Pin);
        await db.SaveChangesAsync(ct);
        SetAdminAccessCookie(context, sessions);
        return Results.NoContent();
    }

    private static async Task<IResult> AuthenticateAdminAccessAsync(HttpContext context, NetworkPinRequest request,
        AppDbContext db, NetworkAccessPinService pinService, NetworkAccessSessionService sessions, CancellationToken ct)
    {
        var configuration = await db.MachineConfigurations.AsNoTracking()
            .SingleAsync(x => x.Id == MachineConfiguration.SingletonId, ct);
        if (!pinService.Verify(request.Pin, configuration.NetworkAccessPinHash)) return Results.Unauthorized();
        SetAdminAccessCookie(context, sessions);
        return Results.NoContent();
    }

    private static IResult LogoutAdminAccessAsync(HttpContext context, NetworkAccessSessionService sessions)
    {
        sessions.Revoke(context.Request.Cookies["CocktailOS.AdminAccess"]);
        context.Response.Cookies.Delete("CocktailOS.AdminAccess");
        return Results.NoContent();
    }

    private static void SetAdminAccessCookie(HttpContext context, NetworkAccessSessionService sessions) =>
        context.Response.Cookies.Append("CocktailOS.AdminAccess", sessions.Create(), new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            MaxAge = TimeSpan.FromHours(12),
            SameSite = SameSiteMode.Strict
        });

    private static MachineConfigurationResponse ToResponse(MachineConfiguration configuration) => new(
        configuration.PumpDriver,
        configuration.PinNumberingScheme,
        configuration.Theme,
        configuration.NetworkAccessEnabled,
        !string.IsNullOrWhiteSpace(configuration.NetworkAccessPinHash),
        configuration.NetworkAccessEnabled ? GetNetworkAddress() : null,
        DispenseService.MaximumParallelPumps);

    private static string? GetNetworkAddress()
    {
        var address = NetworkInterface.GetAllNetworkInterfaces()
            .Where(network => network.OperationalStatus == OperationalStatus.Up &&
                              network.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(network => network.GetIPProperties().UnicastAddresses)
            .Select(unicast => unicast.Address)
            .FirstOrDefault(candidate => candidate.AddressFamily == AddressFamily.InterNetwork
                                         && !IPAddress.IsLoopback(candidate)
                                         && !candidate.ToString().StartsWith("169.254.", StringComparison.Ordinal));
        return address is null ? null : $"{address}:{GetListenPort()}";
    }

    private static int GetListenPort()
    {
        var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
        var firstUrl = urls?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        return Uri.TryCreate(firstUrl, UriKind.Absolute, out var uri) && uri.Port > 0 ? uri.Port : 5149;
    }
}