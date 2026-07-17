using System.Net;
using System.Net.Http.Json;
using CocktailOS.Kiosk.Contracts;
using CocktailOS.Kiosk.Models;
using CocktailOS.Kiosk.Services;

namespace CocktailOS.Kiosk.Tests;

public sealed class CalibrationApiTests
{
    [Fact]
    public async Task StartCalibration_RejectsDummyDriver()
    {
        using var factory = new KioskApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/calibrations", new StartPumpCalibrationRequest(1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task StartAndStopCalibration_UsesSharedSafetyPath()
    {
        using var factory = new KioskApplicationFactory();
        using var client = factory.CreateClient();
        await factory.UseGpioDriverAsync();

        var response = await client.PostAsJsonAsync("/api/calibrations", new StartPumpCalibrationRequest(1));
        var started = await response.Content.ReadFromJsonAsync<DispenseStatusResponse>();

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(started);
        Assert.Equal(PumpOperationModes.Calibration, started.Mode);
        Assert.Equal(DispenseService.CalibrationDurationSeconds, started.EstimatedDurationSeconds);
        await factory.PumpOutput.WaitUntilStartedAsync(TimeSpan.FromSeconds(2));

        var conflict = await client.PostAsJsonAsync("/api/calibrations", new StartPumpCalibrationRequest(2));
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);

        var stoppedResponse = await client.PostAsync("/api/calibrations/current/stop", null);
        var stopped = await stoppedResponse.Content.ReadFromJsonAsync<DispenseStatusResponse>();

        Assert.Equal(DispenseStatuses.Stopped, stopped?.Status);
        Assert.All(factory.PumpOutput.States.Values, Assert.False);
    }

    [Fact]
    public async Task Calibration_HardwareFailureMarksOperationFailedAndStopsOutputs()
    {
        using var factory = new KioskApplicationFactory();
        using var client = factory.CreateClient();
        await factory.UseGpioDriverAsync();
        factory.PumpOutput.FailOnStart = true;

        await client.PostAsJsonAsync("/api/calibrations", new StartPumpCalibrationRequest(1));
        var status = await WaitForStatusAsync(client, DispenseStatuses.Failed, TimeSpan.FromSeconds(2));

        Assert.Equal(DispenseStatuses.Failed, status.Status);
        Assert.All(factory.PumpOutput.States.Values, Assert.False);
    }

    [Fact]
    public async Task StartCalibration_RejectsUnknownPump()
    {
        using var factory = new KioskApplicationFactory();
        using var client = factory.CreateClient();
        await factory.UseGpioDriverAsync();

        var response = await client.PostAsJsonAsync("/api/calibrations", new StartPumpCalibrationRequest(int.MaxValue));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Calibration_CompletesAutomaticallyAfterTenSeconds()
    {
        using var factory = new KioskApplicationFactory();
        using var client = factory.CreateClient();
        await factory.UseGpioDriverAsync();
        await client.PostAsJsonAsync("/api/calibrations", new StartPumpCalibrationRequest(1));

        DispenseStatusResponse? status = null;
        var timeout = DateTimeOffset.UtcNow.AddSeconds(12);
        while (DateTimeOffset.UtcNow < timeout)
        {
            status = await client.GetFromJsonAsync<DispenseStatusResponse>("/api/calibrations/current");
            if (status?.Status == DispenseStatuses.Completed) break;
            await Task.Delay(100);
        }

        Assert.Equal(DispenseStatuses.Completed, status?.Status);
        Assert.All(factory.PumpOutput.States.Values, Assert.False);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task StartCalibration_RejectsDisabledPumpOrMissingIngredient(bool enabled, bool hasIngredient)
    {
        using var factory = new KioskApplicationFactory();
        using var client = factory.CreateClient();
        await factory.UseGpioDriverAsync();
        await factory.UpdatePumpAsync(1, pump =>
        {
            pump.IsEnabled = enabled;
            if (!hasIngredient) pump.IngredientId = null;
        });

        var response = await client.PostAsJsonAsync("/api/calibrations", new StartPumpCalibrationRequest(1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SaveCalibration_PersistsRateAndTracksStaleAndManualStates()
    {
        using var factory = new KioskApplicationFactory();
        using var client = factory.CreateClient();
        await factory.UseGpioDriverAsync();
        var original = await factory.GetPumpAsync();

        var savedResponse = await client.PutAsJsonAsync("/api/pumps/1/calibration", new PumpCalibrationRequest([101, 100]));
        var saved = await savedResponse.Content.ReadFromJsonAsync<PumpResponse>();

        Assert.Equal(HttpStatusCode.OK, savedResponse.StatusCode);
        Assert.NotNull(saved);
        Assert.Equal(10.05m, saved.FlowRateMlPerSecond);
        Assert.Equal(FlowRateSources.Calibrated, saved.FlowRateSource);
        Assert.Equal(CalibrationStatuses.Current, saved.CalibrationStatus);
        Assert.NotNull(saved.LastCalibratedAt);

        var replacementIngredientId = await factory.CreateIngredientAsync("Testzutat");
        var stale = await UpdatePumpAsync(client, saved, replacementIngredientId, saved.FlowRateMlPerSecond);
        Assert.Equal(CalibrationStatuses.Stale, stale.CalibrationStatus);

        var current = await UpdatePumpAsync(client, stale, original.IngredientId, stale.FlowRateMlPerSecond);
        Assert.Equal(CalibrationStatuses.Current, current.CalibrationStatus);

        var manual = await UpdatePumpAsync(client, current, original.IngredientId, current.FlowRateMlPerSecond + 1m);
        Assert.Equal(FlowRateSources.Manual, manual.FlowRateSource);
        Assert.Equal(CalibrationStatuses.Manual, manual.CalibrationStatus);
        Assert.Null(manual.LastCalibratedAt);
    }

    [Theory]
    [MemberData(nameof(InvalidCalibrationVolumes))]
    public async Task SaveCalibration_RejectsInvalidVolumes(int[] volumes)
    {
        using var factory = new KioskApplicationFactory();
        using var client = factory.CreateClient();
        await factory.UseGpioDriverAsync();

        var response = await client.PutAsJsonAsync("/api/pumps/1/calibration", new PumpCalibrationRequest(volumes));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    public static TheoryData<int[]> InvalidCalibrationVolumes => new()
    {
        Array.Empty<int>(),
        new[] { 1, 2, 3, 4 },
        new[] { 0 },
        new[] { 10_001 }
    };

    private static async Task<PumpResponse> UpdatePumpAsync(HttpClient client, PumpResponse pump, int? ingredientId, decimal flowRate)
    {
        var response = await client.PutAsJsonAsync($"/api/pumps/{pump.Id}", new PumpWriteRequest(
            pump.Name,
            pump.GpioPin,
            flowRate,
            pump.ActiveHigh,
            pump.IsEnabled,
            ingredientId));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PumpResponse>())!;
    }

    private static async Task<DispenseStatusResponse> WaitForStatusAsync(HttpClient client, string expectedStatus, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        DispenseStatusResponse? status = null;
        while (DateTimeOffset.UtcNow < deadline)
        {
            status = await client.GetFromJsonAsync<DispenseStatusResponse>("/api/calibrations/current");
            if (status?.Status == expectedStatus) return status;
            await Task.Delay(25);
        }

        throw new Xunit.Sdk.XunitException($"Status {expectedStatus} wurde nicht erreicht. Letzter Status: {status?.Status ?? "unbekannt"}.");
    }
}
