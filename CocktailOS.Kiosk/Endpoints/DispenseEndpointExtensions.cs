using CocktailOS.Kiosk.Contracts;
using CocktailOS.Kiosk.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CocktailOS.Kiosk.Endpoints;

public static class DispenseEndpointExtensions
{
    public static RouteGroupBuilder MapDispenseEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/dispenses/current", GetStatus); api.MapPost("/dispenses", StartDispenseAsync); api.MapPost("/dispenses/current/stop", StopAsync);
        api.MapGet("/cleaning/current", GetStatus); api.MapPost("/cleaning", StartCleaningAsync); api.MapPost("/cleaning/current/stop", StopAsync);
        api.MapGet("/priming/current", GetStatus); api.MapPost("/priming", StartPrimingAsync); api.MapPost("/priming/current/stop", StopAsync);
        api.MapGet("/calibrations/current", GetStatus); api.MapPost("/calibrations", StartCalibrationAsync); api.MapPost("/calibrations/current/stop", StopAsync);
        return api;
    }

    private static IResult GetStatus(DispenseService service) => Results.Ok(service.GetStatus());
    private static async Task<IResult> StopAsync(DispenseService service) => Results.Ok(await service.StopAsync());

    private static async Task<IResult> StartDispenseAsync(StartDispenseRequest request, DispenseService service, CancellationToken ct)
    {
        try { return Results.Accepted("/api/dispenses/current", await service.StartAsync(request.CocktailId, request.SizeId, ct)); }
        catch (DispenseValidationException ex) { return Results.BadRequest(new ProblemDetails { Title = "Ausschank nicht möglich", Detail = ex.Message }); }
        catch (DispenseConflictException ex) { return Results.Conflict(new ProblemDetails { Title = "Ausschank läuft bereits", Detail = ex.Message }); }
    }

    private static async Task<IResult> StartCleaningAsync(StartPumpCleaningRequest request, DispenseService service, CancellationToken ct)
    {
        try { return Results.Accepted("/api/cleaning/current", await service.StartCleaningAsync(request.PumpIds, request.DurationSeconds, ct)); }
        catch (DispenseValidationException ex) { return Results.BadRequest(new ProblemDetails { Title = "Reinigung nicht möglich", Detail = ex.Message }); }
        catch (DispenseConflictException ex) { return Results.Conflict(new ProblemDetails { Title = "Pumpen sind belegt", Detail = ex.Message }); }
    }

    private static async Task<IResult> StartPrimingAsync(StartPumpPrimingRequest request, DispenseService service, CancellationToken ct)
    {
        try { return Results.Accepted("/api/priming/current", await service.StartPrimingAsync(request.PumpId, ct)); }
        catch (DispenseValidationException ex) { return Results.BadRequest(new ProblemDetails { Title = "Vorbereitung nicht möglich", Detail = ex.Message }); }
        catch (DispenseConflictException ex) { return Results.Conflict(new ProblemDetails { Title = "Pumpen sind belegt", Detail = ex.Message }); }
    }

    private static async Task<IResult> StartCalibrationAsync(StartPumpCalibrationRequest request, DispenseService service, CancellationToken ct)
    {
        try { return Results.Accepted("/api/calibrations/current", await service.StartCalibrationAsync(request.PumpId, ct)); }
        catch (DispenseValidationException ex) { return Results.BadRequest(new ProblemDetails { Title = "Kalibrierung nicht möglich", Detail = ex.Message }); }
        catch (DispenseConflictException ex) { return Results.Conflict(new ProblemDetails { Title = "Pumpen sind belegt", Detail = ex.Message }); }
    }
}
