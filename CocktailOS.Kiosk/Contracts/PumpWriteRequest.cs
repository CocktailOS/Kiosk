namespace CocktailOS.Kiosk.Contracts;
public sealed record PumpWriteRequest(string Name, int GpioPin, decimal FlowRateMlPerSecond, bool ActiveHigh, bool IsEnabled, int? IngredientId);
