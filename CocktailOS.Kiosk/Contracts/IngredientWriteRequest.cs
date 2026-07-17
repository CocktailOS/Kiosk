namespace CocktailOS.Kiosk.Contracts;

public sealed record IngredientWriteRequest(
    string Name,
    decimal? AlcoholPercentage,
    decimal BottleSizeMl,
    decimal RemainingVolumeMl);
