using CocktailOS.Kiosk.Contracts;
using CocktailOS.Kiosk.Data;
using CocktailOS.Kiosk.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CocktailOS.Kiosk.Endpoints;

public static class IngredientEndpointExtensions
{
    public static RouteGroupBuilder MapIngredientEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/ingredients", GetAsync); api.MapPost("/ingredients", CreateAsync);
        api.MapPut("/ingredients/{id:int}", UpdateAsync); api.MapDelete("/ingredients/{id:int}", DeleteAsync);
        api.MapPost("/ingredients/{id:int}/refill", RefillAsync);
        return api;
    }

    private static async Task<IResult> GetAsync(AppDbContext db, CancellationToken ct) =>
        Results.Ok((await db.Ingredients.AsNoTracking().Include(x => x.Pump).OrderBy(x => x.Name).ToListAsync(ct)).Select(x => x.ToResponse()));

    private static async Task<IResult> CreateAsync(IngredientWriteRequest request, AppDbContext db, CancellationToken ct)
    {
        var validation = Validate(request); if (validation is not null) return validation;
        var ingredient = new Ingredient
        {
            Name = request.Name.Trim(),
            AlcoholPercentage = request.AlcoholPercentage,
            BottleSizeMl = request.BottleSizeMl,
            RemainingVolumeMl = request.RemainingVolumeMl
        };
        db.Ingredients.Add(ingredient); await db.SaveChangesAsync(ct);
        return Results.Created($"/api/ingredients/{ingredient.Id}", ingredient.ToResponse());
    }

    private static async Task<IResult> UpdateAsync(int id, IngredientWriteRequest request, AppDbContext db, CancellationToken ct)
    {
        var validation = Validate(request); if (validation is not null) return validation;
        var ingredient = await db.Ingredients.Include(x => x.Pump).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (ingredient is null) return Results.NotFound();
        ingredient.Name = request.Name.Trim();
        ingredient.AlcoholPercentage = request.AlcoholPercentage;
        ingredient.BottleSizeMl = request.BottleSizeMl;
        ingredient.RemainingVolumeMl = request.RemainingVolumeMl;
        await db.SaveChangesAsync(ct); return Results.Ok(ingredient.ToResponse());
    }

    private static async Task<IResult> RefillAsync(int id, AppDbContext db, CancellationToken ct)
    {
        var ingredient = await db.Ingredients.Include(x => x.Pump).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (ingredient is null) return Results.NotFound();
        ingredient.RemainingVolumeMl = ingredient.BottleSizeMl;
        await db.SaveChangesAsync(ct);
        return Results.Ok(ingredient.ToResponse());
    }

    private static async Task<IResult> DeleteAsync(int id, AppDbContext db, CancellationToken ct)
    {
        var ingredient = await db.Ingredients.Include(x => x.Cocktails).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (ingredient is null) return Results.NotFound();
        if (ingredient.Cocktails.Count > 0)
            return Results.Conflict(new ProblemDetails { Title = "Zutat wird verwendet", Detail = "Entferne die Zutat zuerst aus allen Cocktails." });
        db.Ingredients.Remove(ingredient); await db.SaveChangesAsync(ct); return Results.NoContent();
    }

    private static IResult? Validate(IngredientWriteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100)
            return EndpointResults.Validation("name", "Der Name muss zwischen 1 und 100 Zeichen lang sein.");
        if (request.AlcoholPercentage is < 0 or > 100)
            return EndpointResults.Validation("alcoholPercentage", "Der Alkoholwert muss zwischen 0 und 100 % liegen.");
        if (request.BottleSizeMl is <= 0 or > InventoryDefaults.MaximumBottleSizeMl)
            return EndpointResults.Validation("bottleSizeMl", $"Die Flaschengröße muss zwischen 1 und {InventoryDefaults.MaximumBottleSizeMl:0} ml liegen.");
        return request.RemainingVolumeMl < 0 || request.RemainingVolumeMl > request.BottleSizeMl
            ? EndpointResults.Validation("remainingVolumeMl", "Der Restbestand muss zwischen 0 ml und der Flaschengröße liegen.")
            : null;
    }
}
