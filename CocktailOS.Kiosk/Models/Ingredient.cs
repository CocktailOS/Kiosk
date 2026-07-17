namespace CocktailOS.Kiosk.Models;

public sealed class Ingredient
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal? AlcoholPercentage { get; set; }
    public decimal BottleSizeMl { get; set; } = InventoryDefaults.DefaultBottleSizeMl;
    public decimal RemainingVolumeMl { get; set; } = InventoryDefaults.DefaultBottleSizeMl;
    public ICollection<CocktailIngredient> Cocktails { get; set; } = [];
    public Pump? Pump { get; set; }
}
