namespace CocktailOS.Kiosk.Models;

public sealed class Ingredient
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal? AlcoholPercentage { get; set; }
    public ICollection<CocktailIngredient> Cocktails { get; set; } = [];
    public Pump? Pump { get; set; }
}
