using CocktailOS.Kiosk.Contracts;
using Microsoft.AspNetCore.Http;

namespace CocktailOS.Kiosk.Endpoints;

public static class ImageEndpointExtensions
{
    public static RouteGroupBuilder MapImageEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/images", UploadAsync).DisableAntiforgery();
        return api;
    }

    private static async Task<IResult> UploadAsync(IFormFile file, IWebHostEnvironment environment, CancellationToken ct)
    {
        const long maximumBytes = 5 * 1024 * 1024;
        var allowedTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg", ["image/png"] = ".png", ["image/webp"] = ".webp", ["image/gif"] = ".gif"
        };
        if (file.Length is <= 0 or > maximumBytes || !allowedTypes.TryGetValue(file.ContentType, out var extension))
            return EndpointResults.Validation("file", "Erlaubt sind JPG, PNG, WebP oder GIF bis maximal 5 MB.");
        var directory = Path.Combine(environment.WebRootPath, "uploads"); Directory.CreateDirectory(directory);
        var fileName = $"{Guid.NewGuid():N}{extension}"; var targetPath = Path.Combine(directory, fileName);
        await using var stream = File.Create(targetPath); await file.CopyToAsync(stream, ct);
        return Results.Ok(new ImageUploadResponse($"/uploads/{fileName}"));
    }
}
