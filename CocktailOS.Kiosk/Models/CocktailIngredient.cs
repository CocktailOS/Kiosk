namespace CocktailOS.Kiosk.Models;

public sealed class CocktailIngredient
{
    public int CocktailId { get; set; }
    public Cocktail Cocktail { get; set; } = null!;
    public int IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;
    public decimal AmountMl { get; set; }
}
