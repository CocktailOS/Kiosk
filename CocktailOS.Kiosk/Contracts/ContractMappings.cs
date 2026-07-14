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

        return new CocktailResponse(
            cocktail.Id,
            cocktail.Name,
            cocktail.Description,
            cocktail.ImagePath,
            cocktail.StandardSize.ToResponse(),
            cocktail.Ingredients
                .OrderByDescending(x => x.AmountMl)
                .Select(x => new CocktailIngredientResponse(
                    x.IngredientId,
                    x.Ingredient.Name,
                    x.AmountMl,
                    x.Ingredient.AlcoholPercentage))
                .ToArray(),
            decimal.Round(alcohol, 1));
    }

    public static IngredientResponse ToResponse(this Ingredient ingredient) =>
        new(ingredient.Id, ingredient.Name, ingredient.AlcoholPercentage, ingredient.Pump is not null);

    public static PumpResponse ToResponse(this Pump pump) =>
        new(
            pump.Id,
            pump.Name,
            pump.GpioPin,
            pump.FlowRateMlPerSecond,
            pump.ActiveHigh,
            pump.IsEnabled,
            pump.IngredientId,
            pump.Ingredient?.Name);

    public static SizeResponse ToResponse(this CocktailSize size) =>
        new(size.Id, size.Name, size.VolumeMl, size.SortOrder);
}
