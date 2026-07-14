namespace CocktailOS.Kiosk.Models;

public sealed class Cocktail
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? ImagePath { get; set; }
    public int StandardSizeId { get; set; }
    public CocktailSize StandardSize { get; set; } = null!;
    public ICollection<CocktailIngredient> Ingredients { get; set; } = [];
}

public sealed class CocktailIngredient
{
    public int CocktailId { get; set; }
    public Cocktail Cocktail { get; set; } = null!;
    public int IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;
    public decimal AmountMl { get; set; }
}

public sealed class Ingredient
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal? AlcoholPercentage { get; set; }
    public ICollection<CocktailIngredient> Cocktails { get; set; } = [];
    public Pump? Pump { get; set; }
}

public sealed class Pump
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int GpioPin { get; set; }
    public decimal FlowRateMlPerSecond { get; set; }
    public bool ActiveHigh { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int? IngredientId { get; set; }
    public Ingredient? Ingredient { get; set; }
}

public sealed class CocktailSize
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int VolumeMl { get; set; }
    public int SortOrder { get; set; }
    public ICollection<Cocktail> StandardForCocktails { get; set; } = [];
}

public sealed class MachineConfiguration
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;
    public required string PumpDriver { get; set; } = PumpDriverNames.Dummy;
    public required string PinNumberingScheme { get; set; } = PinNumberingSchemes.Logical;
    public required string Theme { get; set; } = ThemeNames.Dark;
}

public static class PumpDriverNames
{
    public const string Dummy = "Dummy";
    public const string Gpio = "Gpio";
    public static readonly string[] All = [Dummy, Gpio];
}

public static class PinNumberingSchemes
{
    public const string Logical = "Logical";
    public const string Board = "Board";
    public static readonly string[] All = [Logical, Board];
}

public static class ThemeNames
{
    public const string Dark = "Dark";
    public const string Light = "Light";
    public static readonly string[] All = [Dark, Light];
}
