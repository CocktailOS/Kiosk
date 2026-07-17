namespace CocktailOS.Kiosk.Services;

public static class PumpCalibrationCalculator
{
    public const int MinimumMeasuredVolumeMl = 1;
    public const int MaximumMeasuredVolumeMl = 10_000;
    public const int MaximumMeasurements = 3;
    public const decimal DeviationWarningPercentage = 10m;

    public static PumpCalibrationCalculation Calculate(IReadOnlyList<int> measuredVolumesMl)
    {
        ArgumentNullException.ThrowIfNull(measuredVolumesMl);
        if (measuredVolumesMl.Count is < 1 or > MaximumMeasurements)
            throw new ArgumentException($"Es sind ein bis {MaximumMeasurements} Messwerte erforderlich.", nameof(measuredVolumesMl));
        if (measuredVolumesMl.Any(volume => volume is < MinimumMeasuredVolumeMl or > MaximumMeasuredVolumeMl))
            throw new ArgumentOutOfRangeException(nameof(measuredVolumesMl), $"Messwerte müssen zwischen {MinimumMeasuredVolumeMl} und {MaximumMeasuredVolumeMl} ml liegen.");

        var averageVolume = measuredVolumesMl.Sum(volume => (decimal)volume) / measuredVolumesMl.Count;
        var flowRate = decimal.Round(
            averageVolume / DispenseService.CalibrationDurationSeconds,
            3,
            MidpointRounding.AwayFromZero);
        var deviation = measuredVolumesMl.Count == 1
            ? 0m
            : decimal.Round(
                (measuredVolumesMl.Max() - measuredVolumesMl.Min()) / averageVolume * 100m,
                2,
                MidpointRounding.AwayFromZero);

        return new PumpCalibrationCalculation(flowRate, deviation, deviation > DeviationWarningPercentage);
    }
}

public sealed record PumpCalibrationCalculation(
    decimal FlowRateMlPerSecond,
    decimal DeviationPercentage,
    bool HasDeviationWarning);
