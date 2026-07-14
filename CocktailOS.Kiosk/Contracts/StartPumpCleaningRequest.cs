namespace CocktailOS.Kiosk.Contracts;
public sealed record StartPumpCleaningRequest(IReadOnlyList<int>? PumpIds, int DurationSeconds);
