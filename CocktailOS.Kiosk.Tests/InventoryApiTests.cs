using System.Net;
using System.Net.Http.Json;
using CocktailOS.Kiosk.Contracts;
using CocktailOS.Kiosk.Models;
using CocktailOS.Kiosk.Services;

namespace CocktailOS.Kiosk.Tests;

public sealed class InventoryApiTests
{
    [Fact]
    public async Task Refill_SetsRemainingVolumeToBottleSize()
    {
        using var factory = new KioskApplicationFactory();
        using var client = factory.CreateClient();
        var ingredient = (await client.GetFromJsonAsync<IngredientResponse[]>("/api/ingredients"))!
            .Single(x => x.Name == "Cola");
        var update = new IngredientWriteRequest(ingredient.Name, ingredient.AlcoholPercentage, 700m, 125m);
        var updateResponse = await client.PutAsJsonAsync($"/api/ingredients/{ingredient.Id}", update);
        updateResponse.EnsureSuccessStatusCode();

        var refillResponse = await client.PostAsync($"/api/ingredients/{ingredient.Id}/refill", null);
        var refilled = await refillResponse.Content.ReadFromJsonAsync<IngredientResponse>();

        Assert.Equal(HttpStatusCode.OK, refillResponse.StatusCode);
        Assert.Equal(700m, refilled?.BottleSizeMl);
        Assert.Equal(700m, refilled?.RemainingVolumeMl);
        Assert.Equal(100m, refilled?.StockPercentage);
        Assert.Equal(InventoryStatuses.Available, refilled?.StockStatus);
    }

    [Theory]
    [InlineData(0, 0, "bottleSizeMl")]
    [InlineData(700, 701, "remainingVolumeMl")]
    [InlineData(10001, 10001, "bottleSizeMl")]
    public async Task IngredientUpdate_RejectsInvalidInventory(decimal bottleSizeMl, decimal remainingVolumeMl, string expectedField)
    {
        using var factory = new KioskApplicationFactory();
        using var client = factory.CreateClient();
        var ingredient = (await client.GetFromJsonAsync<IngredientResponse[]>("/api/ingredients"))!.First();
        var response = await client.PutAsJsonAsync(
            $"/api/ingredients/{ingredient.Id}",
            new IngredientWriteRequest(ingredient.Name, ingredient.AlcoholPercentage, bottleSizeMl, remainingVolumeMl));
        var problem = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(expectedField, problem, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CocktailResponse_MarksInsufficientStockAsUnavailable()
    {
        using var factory = new KioskApplicationFactory();
        using var client = factory.CreateClient();
        await factory.UpdateIngredientAsync("Cola", ingredient => ingredient.RemainingVolumeMl = 20m);

        var cocktails = await client.GetFromJsonAsync<CocktailResponse[]>("/api/cocktails");
        var cubaLibre = cocktails!.Single(x => x.Name == "Cuba Libre");

        Assert.Equal(InventoryStatuses.Unavailable, cubaLibre.AvailabilityStatus);
        Assert.Contains("Cola", cubaLibre.UnavailableIngredients);
    }

    [Fact]
    public async Task StartDispense_RejectsInsufficientStock()
    {
        using var factory = new KioskApplicationFactory();
        using var client = factory.CreateClient();
        await factory.UpdateIngredientAsync("Cola", ingredient => ingredient.RemainingVolumeMl = 20m);
        var cocktails = await client.GetFromJsonAsync<CocktailResponse[]>("/api/cocktails");
        var cubaLibre = cocktails!.Single(x => x.Name == "Cuba Libre");

        var response = await client.PostAsJsonAsync(
            "/api/dispenses",
            new StartDispenseRequest(cubaLibre.Id, cubaLibre.StandardSize.Id));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Nicht genügend Vorrat", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CompletedDispense_DeductsExactRecipeAmounts()
    {
        using var factory = new KioskApplicationFactory();
        using var client = factory.CreateClient();
        await factory.SetAllPumpFlowRatesAsync(10_000m);
        var cocktails = await client.GetFromJsonAsync<CocktailResponse[]>("/api/cocktails");
        var cubaLibre = cocktails!.Single(x => x.Name == "Cuba Libre");
        var before = await factory.GetIngredientAsync("Cola");

        var start = await client.PostAsJsonAsync(
            "/api/dispenses",
            new StartDispenseRequest(cubaLibre.Id, cubaLibre.StandardSize.Id));
        start.EnsureSuccessStatusCode();
        await WaitForStatusAsync(client, DispenseStatuses.Completed, TimeSpan.FromSeconds(2));
        var after = await factory.GetIngredientAsync("Cola");

        Assert.Equal(before.RemainingVolumeMl - 190m, after.RemainingVolumeMl);
    }

    [Fact]
    public async Task StoppedDispense_DeductsOnlyActualPumpRuntime()
    {
        using var factory = new KioskApplicationFactory();
        using var client = factory.CreateClient();
        await factory.SetAllPumpFlowRatesAsync(10m);
        var cocktails = await client.GetFromJsonAsync<CocktailResponse[]>("/api/cocktails");
        var cubaLibre = cocktails!.Single(x => x.Name == "Cuba Libre");
        var before = await factory.GetIngredientAsync("Cola");
        await client.PostAsJsonAsync(
            "/api/dispenses",
            new StartDispenseRequest(cubaLibre.Id, cubaLibre.StandardSize.Id));
        await factory.PumpOutput.WaitUntilStartedAsync(TimeSpan.FromSeconds(1));
        await Task.Delay(200);

        var stop = await client.PostAsync("/api/dispenses/current/stop", null);
        stop.EnsureSuccessStatusCode();
        var after = await factory.GetIngredientAsync("Cola");
        var consumed = before.RemainingVolumeMl - after.RemainingVolumeMl;

        Assert.InRange(consumed, 0.5m, 10m);
    }

    private static async Task<DispenseStatusResponse> WaitForStatusAsync(HttpClient client, string expectedStatus, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        DispenseStatusResponse? status = null;
        while (DateTimeOffset.UtcNow < deadline)
        {
            status = await client.GetFromJsonAsync<DispenseStatusResponse>("/api/dispenses/current");
            if (status?.Status == expectedStatus) return status;
            await Task.Delay(20);
        }

        throw new Xunit.Sdk.XunitException($"Status {expectedStatus} wurde nicht erreicht. Letzter Status: {status?.Status ?? "unbekannt"}.");
    }
}
