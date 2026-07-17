using CocktailOS.Kiosk.Contracts;
using CocktailOS.Kiosk.Data;
using CocktailOS.Kiosk.Models;
using CocktailOS.Kiosk.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CocktailOS.Kiosk.Endpoints;

public static class CocktailEndpointExtensions
{
    public static RouteGroupBuilder MapCocktailEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/cocktails", GetAsync);
        api.MapGet("/cocktails/{id:int}", GetByIdAsync);
        api.MapPost("/cocktails", CreateAsync);
        api.MapPut("/cocktails/{id:int}", UpdateAsync);
        api.MapDelete("/cocktails/{id:int}", DeleteAsync);
        return api;
    }

    private static async Task<IResult> GetAsync(AppDbContext db, CancellationToken ct) =>
        Results.Ok((await Query(db).OrderBy(x => x.Name).ToListAsync(ct)).Select(x => x.ToResponse()));

    private static async Task<IResult> GetByIdAsync(int id, AppDbContext db, CancellationToken ct)
    {
        var cocktail = await Query(db).SingleOrDefaultAsync(x => x.Id == id, ct);
        return cocktail is null ? Results.NotFound() : Results.Ok(cocktail.ToResponse());
    }

    private static async Task<IResult> CreateAsync(CocktailWriteRequest request, AppDbContext db, CancellationToken ct)
    {
        var validation = await ValidateAsync(request, db, ct);
        if (validation is not null) return validation;
        var cocktail = new Cocktail { Name = request.Name.Trim(), StandardSizeId = request.StandardSizeId };
        Apply(cocktail, request);
        db.Cocktails.Add(cocktail);
        await db.SaveChangesAsync(ct);
        var created = await Query(db).SingleAsync(x => x.Id == cocktail.Id, ct);
        return Results.Created($"/api/cocktails/{cocktail.Id}", created.ToResponse());
    }

    private static async Task<IResult> UpdateAsync(int id, CocktailWriteRequest request, AppDbContext db, CancellationToken ct)
    {
        var cocktail = await db.Cocktails.Include(x => x.Ingredients).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (cocktail is null) return Results.NotFound();
        var validation = await ValidateAsync(request, db, ct);
        if (validation is not null) return validation;
        db.CocktailIngredients.RemoveRange(cocktail.Ingredients);
        cocktail.Ingredients.Clear();
        Apply(cocktail, request);
        await db.SaveChangesAsync(ct);
        var updated = await Query(db).SingleAsync(x => x.Id == id, ct);
        return Results.Ok(updated.ToResponse());
    }

    private static async Task<IResult> DeleteAsync(int id, AppDbContext db, CancellationToken ct)
    {
        var cocktail = await db.Cocktails.FindAsync([id], ct);
        if (cocktail is null) return Results.NotFound();
        db.Cocktails.Remove(cocktail);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static IQueryable<Cocktail> Query(AppDbContext db) => db.Cocktails.AsNoTracking().AsSplitQuery()
        .Include(x => x.StandardSize)
        .Include(x => x.Ingredients)
        .ThenInclude(x => x.Ingredient)
        .ThenInclude(x => x.Pump);

    private static void Apply(Cocktail cocktail, CocktailWriteRequest request)
    {
        cocktail.Name = request.Name.Trim();
        cocktail.Description = EndpointResults.NullIfWhiteSpace(request.Description);
        cocktail.ImagePath = EndpointResults.NullIfWhiteSpace(request.ImagePath);
        cocktail.StandardSizeId = request.StandardSizeId;
        foreach (var item in request.Ingredients)
            cocktail.Ingredients.Add(new CocktailIngredient { IngredientId = item.IngredientId, AmountMl = item.AmountMl });
    }

    private static async Task<IResult?> ValidateAsync(CocktailWriteRequest request, AppDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100)
            return EndpointResults.Validation("name", "Der Name muss zwischen 1 und 100 Zeichen lang sein.");
        if (request.Description?.Length > 500)
            return EndpointResults.Validation("description", "Die Beschreibung darf höchstens 500 Zeichen lang sein.");
        var size = await db.Sizes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.StandardSizeId, ct);
        if (size is null) return EndpointResults.Validation("standardSizeId", "Die Standardgröße existiert nicht.");
        if (request.Ingredients.Count is < 1 or > DispenseService.MaximumParallelPumps)
            return EndpointResults.Validation("ingredients", $"Ein Cocktail benötigt 1 bis {DispenseService.MaximumParallelPumps} Zutaten.");
        if (request.Ingredients.Any(x => x.AmountMl <= 0))
            return EndpointResults.Validation("ingredients", "Alle Zutatenmengen müssen größer als 0 ml sein.");
        if (request.Ingredients.Select(x => x.IngredientId).Distinct().Count() != request.Ingredients.Count)
            return EndpointResults.Validation("ingredients", "Eine Zutat darf pro Cocktail nur einmal vorkommen.");
        var ingredientIds = request.Ingredients.Select(x => x.IngredientId).ToArray();
        if (await db.Ingredients.CountAsync(x => ingredientIds.Contains(x.Id), ct) != ingredientIds.Length)
            return EndpointResults.Validation("ingredients", "Mindestens eine ausgewählte Zutat existiert nicht.");
        var total = request.Ingredients.Sum(x => x.AmountMl);
        return Math.Abs(total - size.VolumeMl) > .5m
            ? EndpointResults.Validation("ingredients", $"Die Zutaten müssen zusammen {size.VolumeMl} ml ergeben (aktuell {total:0.##} ml).")
            : null;
    }
}
