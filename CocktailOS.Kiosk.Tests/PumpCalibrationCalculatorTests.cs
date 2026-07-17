using CocktailOS.Kiosk.Services;

namespace CocktailOS.Kiosk.Tests;

public sealed class PumpCalibrationCalculatorTests
{
    [Theory]
    [InlineData(new[] { 101 }, 10.1)]
    [InlineData(new[] { 101, 100 }, 10.05)]
    [InlineData(new[] { 100, 101, 102 }, 10.1)]
    [InlineData(new[] { 1, 2, 2 }, 0.167)]
    public void Calculate_UsesAllMeasurementsAndRoundsToThreeDecimals(int[] volumes, decimal expected)
    {
        var result = PumpCalibrationCalculator.Calculate(volumes);

        Assert.Equal(expected, result.FlowRateMlPerSecond);
    }

    [Fact]
    public void Calculate_WarnsWhenSpreadExceedsTenPercent()
    {
        var result = PumpCalibrationCalculator.Calculate([100, 120]);

        Assert.Equal(18.18m, result.DeviationPercentage);
        Assert.True(result.HasDeviationWarning);
    }

    [Fact]
    public void Calculate_DoesNotWarnAtTenPercent()
    {
        var result = PumpCalibrationCalculator.Calculate([95, 105]);

        Assert.Equal(10m, result.DeviationPercentage);
        Assert.False(result.HasDeviationWarning);
    }

    [Theory]
    [MemberData(nameof(InvalidMeasurements))]
    public void Calculate_RejectsInvalidMeasurements(int[] volumes)
    {
        Assert.ThrowsAny<ArgumentException>(() => PumpCalibrationCalculator.Calculate(volumes));
    }

    public static TheoryData<int[]> InvalidMeasurements => new()
    {
        Array.Empty<int>(),
        new[] { 1, 2, 3, 4 },
        new[] { 0 },
        new[] { 10_001 }
    };
}
