namespace CocktailOS.Kiosk.Models;

public sealed class CocktailSize
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int VolumeMl { get; set; }
    public int SortOrder { get; set; }
    public ICollection<Cocktail> StandardForCocktails { get; set; } = [];
}
