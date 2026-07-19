using CocktailOS.Kiosk.Data;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace CocktailOS.Kiosk.Endpoints;

public static class GuestMenuEndpointExtensions
{
    public static RouteGroupBuilder MapGuestMenuEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/menu/cocktails", GetCocktailsAsync);
        api.MapGet("/menu/qr", GetQrCode);
        return api;
    }

    private static async Task<IResult> GetCocktailsAsync(AppDbContext db, CancellationToken ct) =>
        Results.Ok(await db.Cocktails.AsNoTracking().AsSplitQuery()
            .Include(x => x.StandardSize).Include(x => x.Ingredients).ThenInclude(x => x.Ingredient)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Description, x.ImagePath, VolumeMl = x.StandardSize.VolumeMl, AlcoholPercentage = x.Ingredients.Sum(i => i.AmountMl) == 0 ? 0 : x.Ingredients.Sum(i => i.AmountMl * (i.Ingredient.AlcoholPercentage ?? 0)) / x.Ingredients.Sum(i => i.AmountMl), Ingredients = x.Ingredients.Select(i => new { Name = i.Ingredient.Name, i.AmountMl }) })
            .ToListAsync(ct));

    private static IResult GetQrCode(HttpRequest request)
    {
        var address = NetworkInterface.GetAllNetworkInterfaces().SelectMany(x => x.GetIPProperties().UnicastAddresses)
            .Select(x => x.Address).FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(x))?.ToString()
            ?? request.Host.Host;
        var url = $"http://{address}:{request.Host.Port ?? 8080}/menu";
        using var data = QRCodeGenerator.GenerateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        return Results.Content(new SvgQRCode(data).GetGraphic(8), "image/svg+xml");
    }
}
