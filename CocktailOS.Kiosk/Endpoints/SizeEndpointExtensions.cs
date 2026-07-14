using CocktailOS.Kiosk.Contracts;
using CocktailOS.Kiosk.Data;
using CocktailOS.Kiosk.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CocktailOS.Kiosk.Endpoints;

public static class SizeEndpointExtensions
{
    public static RouteGroupBuilder MapSizeEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/sizes", GetAsync); api.MapPost("/sizes", CreateAsync);
        api.MapPut("/sizes/{id:int}", UpdateAsync); api.MapDelete("/sizes/{id:int}", DeleteAsync);
        return api;
    }

    private static async Task<IResult> GetAsync(AppDbContext db, CancellationToken ct) =>
        Results.Ok((await db.Sizes.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.VolumeMl).ToListAsync(ct)).Select(x => x.ToResponse()));

    private static async Task<IResult> CreateAsync(SizeWriteRequest request, AppDbContext db, CancellationToken ct)
    {
        var validation = Validate(request); if (validation is not null) return validation;
        var size = new CocktailSize { Name = request.Name.Trim() }; Apply(size, request); db.Sizes.Add(size);
        await db.SaveChangesAsync(ct); return Results.Created($"/api/sizes/{size.Id}", size.ToResponse());
    }

    private static async Task<IResult> UpdateAsync(int id, SizeWriteRequest request, AppDbContext db, CancellationToken ct)
    {
        var validation = Validate(request); if (validation is not null) return validation;
        var size = await db.Sizes.Include(x => x.StandardForCocktails).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (size is null) return Results.NotFound();
        if (size.VolumeMl != request.VolumeMl && size.StandardForCocktails.Count > 0)
            return Results.Conflict(new ProblemDetails { Title = "Größe wird als Standard verwendet", Detail = "Ändere zuerst die Standardgröße der betroffenen Cocktails oder lege eine neue Größe an." });
        Apply(size, request); await db.SaveChangesAsync(ct); return Results.Ok(size.ToResponse());
    }

    private static async Task<IResult> DeleteAsync(int id, AppDbContext db, CancellationToken ct)
    {
        var size = await db.Sizes.Include(x => x.StandardForCocktails).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (size is null) return Results.NotFound();
        if (size.StandardForCocktails.Count > 0)
            return Results.Conflict(new ProblemDetails { Title = "Größe wird verwendet", Detail = "Die Größe ist noch Standardgröße mindestens eines Cocktails." });
        db.Sizes.Remove(size); await db.SaveChangesAsync(ct); return Results.NoContent();
    }

    private static IResult? Validate(SizeWriteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 50) return EndpointResults.Validation("name", "Der Name muss zwischen 1 und 50 Zeichen lang sein.");
        return request.VolumeMl is < 10 or > 2000 ? EndpointResults.Validation("volumeMl", "Das Volumen muss zwischen 10 und 2000 ml liegen.") : null;
    }

    private static void Apply(CocktailSize size, SizeWriteRequest request) { size.Name = request.Name.Trim(); size.VolumeMl = request.VolumeMl; size.SortOrder = request.SortOrder; }
}
