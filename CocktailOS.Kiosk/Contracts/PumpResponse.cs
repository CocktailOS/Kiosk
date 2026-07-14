namespace CocktailOS.Kiosk.Contracts;
public sealed record PumpResponse(int Id, string Name, int GpioPin, decimal FlowRateMlPerSecond, bool ActiveHigh, bool IsEnabled, int? IngredientId, string? IngredientName);
