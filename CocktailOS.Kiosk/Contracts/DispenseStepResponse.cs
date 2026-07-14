namespace CocktailOS.Kiosk.Contracts;
public sealed record DispenseStepResponse(string IngredientName, decimal AmountMl, int PumpId, double DurationSeconds);
