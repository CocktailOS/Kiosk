using CocktailOS.Kiosk.Models;
using CocktailOS.Kiosk.Services;
using Microsoft.EntityFrameworkCore;

namespace CocktailOS.Kiosk.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);

        if (!await db.MachineConfigurations.AnyAsync(cancellationToken))
        {
            var networkAccessEnabled = string.Equals(Environment.GetEnvironmentVariable("COCKTAILOS_NETWORK_ACCESS_DEFAULT"), "true", StringComparison.OrdinalIgnoreCase);
            var networkAccessPinHash = Environment.GetEnvironmentVariable("COCKTAILOS_NETWORK_ACCESS_PIN_HASH");
            db.MachineConfigurations.Add(new MachineConfiguration
            {
                PumpDriver = PumpDriverNames.Dummy,
                PinNumberingScheme = PinNumberingSchemes.Logical,
                Theme = ThemeNames.Dark,
                NetworkAccessEnabled = networkAccessEnabled,
                NetworkAccessPinHash = networkAccessEnabled && NetworkAccessPinService.IsHash(networkAccessPinHash) ? networkAccessPinHash : null
            });
        }

        if (await db.Sizes.AnyAsync(cancellationToken))
        {
            await db.SaveChangesAsync(cancellationToken);
            await EnsureAdditionalDemoCocktailsAsync(db, cancellationToken);
            return;
        }

        var small = new CocktailSize { Name = "Klein", VolumeMl = 100, SortOrder = 10 };
        var normal = new CocktailSize { Name = "Normal", VolumeMl = 250, SortOrder = 20 };
        var large = new CocktailSize { Name = "Groß", VolumeMl = 330, SortOrder = 30 };
        db.Sizes.AddRange(small, normal, large);

        var rum = new Ingredient { Name = "Weißer Rum", AlcoholPercentage = 37.5m };
        var cola = new Ingredient { Name = "Cola", AlcoholPercentage = 0m };
        var lime = new Ingredient { Name = "Limettensaft", AlcoholPercentage = 0m };
        var vodka = new Ingredient { Name = "Wodka", AlcoholPercentage = 40m };
        var orange = new Ingredient { Name = "Orangensaft", AlcoholPercentage = 0m };
        var cranberry = new Ingredient { Name = "Cranberrysaft", AlcoholPercentage = 0m };
        var grenadine = new Ingredient { Name = "Grenadine", AlcoholPercentage = 0m };
        var pineapple = new Ingredient { Name = "Ananassaft", AlcoholPercentage = 0m };
        db.Ingredients.AddRange(rum, cola, lime, vodka, orange, cranberry, grenadine, pineapple);

        db.Pumps.AddRange(
            new Pump { Name = "Pumpe 1", GpioPin = 17, FlowRateMlPerSecond = 20m, Ingredient = rum },
            new Pump { Name = "Pumpe 2", GpioPin = 18, FlowRateMlPerSecond = 24m, Ingredient = cola },
            new Pump { Name = "Pumpe 3", GpioPin = 22, FlowRateMlPerSecond = 18m, Ingredient = lime },
            new Pump { Name = "Pumpe 4", GpioPin = 23, FlowRateMlPerSecond = 20m, Ingredient = vodka },
            new Pump { Name = "Pumpe 5", GpioPin = 24, FlowRateMlPerSecond = 24m, Ingredient = orange },
            new Pump { Name = "Pumpe 6", GpioPin = 25, FlowRateMlPerSecond = 24m, Ingredient = cranberry },
            new Pump { Name = "Pumpe 7", GpioPin = 5, FlowRateMlPerSecond = 16m, Ingredient = grenadine },
            new Pump { Name = "Pumpe 8", GpioPin = 6, FlowRateMlPerSecond = 24m, Ingredient = pineapple });

        db.Cocktails.AddRange(
            CreateCocktail("Cuba Libre", "Rum, Cola und ein Spritzer Limette.", "/assets/cuba-libre.svg", normal,
                (rum, 50m), (cola, 190m), (lime, 10m)),
            CreateCocktail("Sunset Breeze", "Fruchtig, frisch und alkoholfrei.", "/assets/sunset-breeze.svg", normal,
                (orange, 140m), (pineapple, 90m), (grenadine, 20m)),
            CreateCocktail("Cosmopolitan", "Wodka, Cranberry und Limette.", "/assets/cosmopolitan.svg", normal,
                (vodka, 50m), (cranberry, 170m), (lime, 30m)),
            CreateCocktail("Bay Breeze", "Wodka, Cranberry und Ananas.", "/assets/bay-breeze.svg", normal,
                (vodka, 50m), (cranberry, 100m), (pineapple, 100m)),
            CreateCocktail("Vodka Sunrise", "Wodka, Orange und Grenadine.", "/assets/vodka-sunrise.svg", normal,
                (vodka, 50m), (orange, 180m), (grenadine, 20m)));

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureAdditionalDemoCocktailsAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var names = new[] { "Bay Breeze", "Vodka Sunrise" };
        var existingNames = await db.Cocktails
            .Where(x => names.Contains(x.Name))
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);

        if (existingNames.Count == names.Length)
        {
            return;
        }

        var normal = await db.Sizes.SingleOrDefaultAsync(x => x.Name == "Normal", cancellationToken);
        var ingredients = await db.Ingredients
            .Where(x => x.Name == "Wodka" || x.Name == "Cranberrysaft" || x.Name == "Ananassaft" ||
                        x.Name == "Orangensaft" || x.Name == "Grenadine")
            .ToDictionaryAsync(x => x.Name, cancellationToken);

        if (normal is null || ingredients.Count != 5)
        {
            return;
        }

        if (!existingNames.Contains("Bay Breeze"))
        {
            db.Cocktails.Add(CreateCocktail(
                "Bay Breeze",
                "Wodka, Cranberry und Ananas.",
                "/assets/bay-breeze.svg",
                normal,
                (ingredients["Wodka"], 50m),
                (ingredients["Cranberrysaft"], 100m),
                (ingredients["Ananassaft"], 100m)));
        }

        if (!existingNames.Contains("Vodka Sunrise"))
        {
            db.Cocktails.Add(CreateCocktail(
                "Vodka Sunrise",
                "Wodka, Orange und Grenadine.",
                "/assets/vodka-sunrise.svg",
                normal,
                (ingredients["Wodka"], 50m),
                (ingredients["Orangensaft"], 180m),
                (ingredients["Grenadine"], 20m)));
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static Cocktail CreateCocktail(
        string name,
        string description,
        string imagePath,
        CocktailSize standardSize,
        params (Ingredient Ingredient, decimal AmountMl)[] ingredients)
    {
        var cocktail = new Cocktail
        {
            Name = name,
            Description = description,
            ImagePath = imagePath,
            StandardSize = standardSize
        };

        foreach (var (ingredient, amountMl) in ingredients)
        {
            cocktail.Ingredients.Add(new CocktailIngredient { Ingredient = ingredient, AmountMl = amountMl });
        }

        return cocktail;
    }
}
