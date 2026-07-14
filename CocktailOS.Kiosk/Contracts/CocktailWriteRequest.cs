namespace CocktailOS.Kiosk.Contracts;
public sealed record CocktailWriteRequest(string Name, string? Description, string? ImagePath, int StandardSizeId, IReadOnlyList<CocktailIngredientWriteRequest> Ingredients);
