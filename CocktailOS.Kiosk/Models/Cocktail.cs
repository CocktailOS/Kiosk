namespace CocktailOS.Kiosk.Models;

public sealed class Cocktail
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? ImagePath { get; set; }
    public int StandardSizeId { get; set; }
    public CocktailSize StandardSize { get; set; } = null!;
    public ICollection<CocktailIngredient> Ingredients { get; set; } = [];
}
