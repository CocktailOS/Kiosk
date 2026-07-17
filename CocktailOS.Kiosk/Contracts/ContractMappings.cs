using CocktailOS.Kiosk.Models;

namespace CocktailOS.Kiosk.Contracts;

public static class ContractMappings
{
    public static CocktailResponse ToResponse(this Cocktail cocktail)
    {
        var total = cocktail.Ingredients.Sum(x => x.AmountMl);
        var alcohol = total <= 0
            ? 0
            : cocktail.Ingredients.Sum(x => x.AmountMl * (x.Ingredient.AlcoholPercentage ?? 0m)) / total;

        var ingredients = cocktail.Ingredients
            .OrderByDescending(x => x.AmountMl)
            .Select(x => new CocktailIngredientResponse(
                x.IngredientId,
                x.Ingredient.Name,
                x.AmountMl,
                x.Ingredient.AlcoholPercentage,
                x.Ingredient.BottleSizeMl,
                x.Ingredient.RemainingVolumeMl,
                x.Ingredient.Pump is { IsEnabled: true }))
            .ToArray();
        var unavailableIngredients = ingredients
            .Where(x => !x.HasActivePump || x.RemainingVolumeMl < x.AmountMl)
            .Select(x => x.Name)
            .ToArray();
        var availabilityStatus = unavailableIngredients.Length > 0
            ? InventoryStatuses.Unavailable
            : ingredients.Any(x => x.RemainingVolumeMl <= InventoryDefaults.LowStockThresholdMl)
                ? InventoryStatuses.Low
                : InventoryStatuses.Available;

        return new CocktailResponse(
            cocktail.Id,
            cocktail.Name,
            cocktail.Description,
            cocktail.ImagePath,
            cocktail.StandardSize.ToResponse(),
            ingredients,
            decimal.Round(alcohol, 1),
            availabilityStatus,
            unavailableIngredients);
    }

    public static IngredientResponse ToResponse(this Ingredient ingredient)
    {
        var stockPercentage = ingredient.BottleSizeMl <= 0
            ? 0
            : decimal.Round(ingredient.RemainingVolumeMl / ingredient.BottleSizeMl * 100m, 1);
        var stockStatus = ingredient.RemainingVolumeMl <= 0
            ? InventoryStatuses.Unavailable
            : ingredient.RemainingVolumeMl <= InventoryDefaults.LowStockThresholdMl
                ? InventoryStatuses.Low
                : InventoryStatuses.Available;

        return new IngredientResponse(
            ingredient.Id,
            ingredient.Name,
            ingredient.AlcoholPercentage,
            ingredient.Pump is not null,
            ingredient.BottleSizeMl,
            ingredient.RemainingVolumeMl,
            stockPercentage,
            stockStatus);
    }

    public static PumpResponse ToResponse(this Pump pump) =>
        new(
            pump.Id,
            pump.Name,
            pump.GpioPin,
            pump.FlowRateMlPerSecond,
            pump.ActiveHigh,
            pump.IsEnabled,
            pump.IngredientId,
            pump.Ingredient?.Name,
            pump.FlowRateSource,
            GetCalibrationStatus(pump),
            pump.LastCalibratedAt);

    private static string GetCalibrationStatus(Pump pump)
    {
        if (!pump.FlowRateSource.Equals(FlowRateSources.Calibrated, StringComparison.OrdinalIgnoreCase)
            || pump.LastCalibratedAt is null)
        {
            return CalibrationStatuses.Manual;
        }

        return pump.IngredientId is not null && pump.IngredientId == pump.CalibratedIngredientId
            ? CalibrationStatuses.Current
            : CalibrationStatuses.Stale;
    }

    public static SizeResponse ToResponse(this CocktailSize size) =>
        new(size.Id, size.Name, size.VolumeMl, size.SortOrder);
}
