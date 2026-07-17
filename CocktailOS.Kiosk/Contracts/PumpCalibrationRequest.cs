namespace CocktailOS.Kiosk.Contracts;

public sealed record PumpCalibrationRequest(IReadOnlyList<int>? MeasuredVolumesMl);
