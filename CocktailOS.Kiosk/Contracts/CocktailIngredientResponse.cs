namespace CocktailOS.Kiosk.Contracts;
public sealed record CocktailIngredientResponse(int IngredientId, string Name, decimal AmountMl, decimal? AlcoholPercentage);
