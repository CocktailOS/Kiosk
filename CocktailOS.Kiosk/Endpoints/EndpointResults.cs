using Microsoft.AspNetCore.Http;

namespace CocktailOS.Kiosk.Endpoints;

internal static class EndpointResults
{
    public static IResult Validation(string key, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [key] = [message] });

    public static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
