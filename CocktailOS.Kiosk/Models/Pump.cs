namespace CocktailOS.Kiosk.Models;

public sealed class Pump
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int GpioPin { get; set; }
    public decimal FlowRateMlPerSecond { get; set; }
    public bool ActiveHigh { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public int? IngredientId { get; set; }
    public Ingredient? Ingredient { get; set; }
    public string FlowRateSource { get; set; } = FlowRateSources.Manual;
    public int? CalibratedIngredientId { get; set; }
    public Ingredient? CalibratedIngredient { get; set; }
    public DateTimeOffset? LastCalibratedAt { get; set; }
}
