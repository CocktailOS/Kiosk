namespace CocktailOS.Kiosk.Contracts;
public sealed record CocktailResponse(int Id, string Name, string? Description, string? ImagePath, SizeResponse StandardSize, IReadOnlyList<CocktailIngredientResponse> Ingredients, decimal AlcoholPercentage);
