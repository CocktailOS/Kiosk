namespace CocktailOS.Kiosk.Endpoints;

public static class ApiEndpointExtensions
{
    public static IEndpointRouteBuilder MapCocktailOsApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api");
        api.MapCocktailEndpoints();
        api.MapIngredientEndpoints();
        api.MapPumpEndpoints();
        api.MapSizeEndpoints();
        api.MapSystemEndpoints();
        api.MapImageEndpoints();
        api.MapDispenseEndpoints();
        return endpoints;
    }
}
