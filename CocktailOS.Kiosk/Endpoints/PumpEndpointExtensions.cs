using CocktailOS.Kiosk.Contracts;
using CocktailOS.Kiosk.Data;
using CocktailOS.Kiosk.Models;
using CocktailOS.Kiosk.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CocktailOS.Kiosk.Endpoints;

public static class PumpEndpointExtensions
{
    public static RouteGroupBuilder MapPumpEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/pumps", GetAsync); api.MapPost("/pumps", CreateAsync);
        api.MapPut("/pumps/{id:int}", UpdateAsync); api.MapDelete("/pumps/{id:int}", DeleteAsync);
        api.MapPut("/pumps/{id:int}/calibration", SaveCalibrationAsync);
        return api;
    }

    private static async Task<IResult> GetAsync(AppDbContext db, CancellationToken ct) =>
        Results.Ok((await db.Pumps.AsNoTracking().Include(x => x.Ingredient).OrderBy(x => x.Name).ToListAsync(ct)).Select(x => x.ToResponse()));

    private static async Task<IResult> CreateAsync(PumpWriteRequest request, AppDbContext db, CancellationToken ct)
    {
        if (await db.Pumps.CountAsync(ct) >= DispenseService.MaximumParallelPumps)
            return Results.Conflict(new ProblemDetails { Title = "Pumpenlimit erreicht", Detail = "Es können maximal 8 Pumpen angelegt werden." });
        var validation = await ValidateAsync(request, null, db, ct); if (validation is not null) return validation;
        var pump = new Pump { Name = request.Name.Trim() }; Apply(pump, request); db.Pumps.Add(pump);
        await db.SaveChangesAsync(ct); await db.Entry(pump).Reference(x => x.Ingredient).LoadAsync(ct);
        return Results.Created($"/api/pumps/{pump.Id}", pump.ToResponse());
    }

    private static async Task<IResult> UpdateAsync(int id, PumpWriteRequest request, AppDbContext db, CancellationToken ct)
    {
        var pump = await db.Pumps.Include(x => x.Ingredient).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (pump is null) return Results.NotFound();
        var validation = await ValidateAsync(request, id, db, ct); if (validation is not null) return validation;
        var flowRateChanged = pump.FlowRateMlPerSecond != request.FlowRateMlPerSecond;
        Apply(pump, request);
        if (flowRateChanged)
        {
            pump.FlowRateSource = FlowRateSources.Manual;
            pump.CalibratedIngredientId = null;
            pump.LastCalibratedAt = null;
        }
        await db.SaveChangesAsync(ct); await db.Entry(pump).Reference(x => x.Ingredient).LoadAsync(ct);
        return Results.Ok(pump.ToResponse());
    }

    private static async Task<IResult> SaveCalibrationAsync(int id, PumpCalibrationRequest request, AppDbContext db, CancellationToken ct)
    {
        var pump = await db.Pumps.Include(x => x.Ingredient).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (pump is null) return Results.NotFound();
        if (!pump.IsEnabled) return EndpointResults.Validation("pump", "Die Pumpe ist deaktiviert.");
        if (pump.IngredientId is null) return EndpointResults.Validation("ingredientId", "Ordne der Pumpe vor der Kalibrierung eine Zutat zu.");

        var configuration = await db.MachineConfigurations.AsNoTracking()
            .SingleAsync(x => x.Id == MachineConfiguration.SingletonId, ct);
        if (!configuration.PumpDriver.Equals(PumpDriverNames.Gpio, StringComparison.OrdinalIgnoreCase))
            return EndpointResults.Validation("pumpDriver", "Die Kalibrierung ist nur mit dem GPIO-Pumpentreiber möglich.");

        var volumes = request.MeasuredVolumesMl;
        if (volumes is null || volumes.Count is < 1 or > PumpCalibrationCalculator.MaximumMeasurements)
            return EndpointResults.Validation("measuredVolumesMl", "Gib ein bis drei Messwerte an.");
        if (volumes.Any(volume => volume is < PumpCalibrationCalculator.MinimumMeasuredVolumeMl or > PumpCalibrationCalculator.MaximumMeasuredVolumeMl))
            return EndpointResults.Validation("measuredVolumesMl", "Jeder Messwert muss zwischen 1 und 10.000 ml liegen.");

        var flowRate = PumpCalibrationCalculator.Calculate(volumes).FlowRateMlPerSecond;
        if (flowRate is <= 0 or > 1000)
            return EndpointResults.Validation("flowRateMlPerSecond", "Die berechnete Förderrate muss größer als 0 und höchstens 1000 ml/s sein.");

        pump.FlowRateMlPerSecond = flowRate;
        pump.FlowRateSource = FlowRateSources.Calibrated;
        pump.CalibratedIngredientId = pump.IngredientId;
        pump.LastCalibratedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(pump.ToResponse());
    }

    private static async Task<IResult> DeleteAsync(int id, AppDbContext db, CancellationToken ct)
    {
        var pump = await db.Pumps.FindAsync([id], ct); if (pump is null) return Results.NotFound();
        db.Pumps.Remove(pump); await db.SaveChangesAsync(ct); return Results.NoContent();
    }

    private static async Task<IResult?> ValidateAsync(PumpWriteRequest request, int? pumpId, AppDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100) return EndpointResults.Validation("name", "Der Name muss zwischen 1 und 100 Zeichen lang sein.");
        var configuration = await db.MachineConfigurations.AsNoTracking().SingleAsync(x => x.Id == MachineConfiguration.SingletonId, ct);
        if (!RaspberryPiHeaderPins.IsConfigurablePin(configuration.PinNumberingScheme, request.GpioPin))
            return EndpointResults.Validation("gpioPin", configuration.PinNumberingScheme.Equals(PinNumberingSchemes.Board, StringComparison.OrdinalIgnoreCase)
                ? "Der ausgewählte Pin ist kein gpio-Pin des physischen 40-Pin-Headers."
                : "Der gpio-Pin muss zwischen 0 und 27 liegen.");
        if (request.FlowRateMlPerSecond is <= 0 or > 1000) return EndpointResults.Validation("flowRateMlPerSecond", "Die Förderrate muss größer als 0 und höchstens 1000 ml/s sein.");
        if (request.IngredientId is not null && !await db.Ingredients.AnyAsync(x => x.Id == request.IngredientId, ct)) return EndpointResults.Validation("ingredientId", "Die ausgewählte Zutat existiert nicht.");
        if (await db.Pumps.AnyAsync(x => x.Id != pumpId && x.GpioPin == request.GpioPin, ct)) return EndpointResults.Validation("gpioPin", "Dieser gpio-Pin ist bereits einer anderen Pumpe zugeordnet.");
        return request.IngredientId is not null && await db.Pumps.AnyAsync(x => x.Id != pumpId && x.IngredientId == request.IngredientId, ct)
            ? EndpointResults.Validation("ingredientId", "Diese Zutat ist bereits einer anderen Pumpe zugeordnet.") : null;
    }

    private static void Apply(Pump pump, PumpWriteRequest request)
    {
        pump.Name = request.Name.Trim(); pump.GpioPin = request.GpioPin; pump.FlowRateMlPerSecond = request.FlowRateMlPerSecond;
        pump.ActiveHigh = request.ActiveHigh; pump.IsEnabled = request.IsEnabled; pump.IngredientId = request.IngredientId;
    }
}
