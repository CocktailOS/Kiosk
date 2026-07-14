namespace CocktailOS.Kiosk.Contracts;
public sealed record IngredientResponse(int Id, string Name, decimal? AlcoholPercentage, bool HasPump);
