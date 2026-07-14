using CocktailOS.Kiosk.Contracts;
using CocktailOS.Kiosk.Data;
using CocktailOS.Kiosk.Models;
using CocktailOS.Kiosk.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CocktailOS.Kiosk.Endpoints;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapCocktailOsApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api");

        api.MapGet("/cocktails", GetCocktailsAsync);
        api.MapGet("/cocktails/{id:int}", GetCocktailAsync);
        api.MapPost("/cocktails", CreateCocktailAsync);
        api.MapPut("/cocktails/{id:int}", UpdateCocktailAsync);
        api.MapDelete("/cocktails/{id:int}", DeleteCocktailAsync);

        api.MapGet("/ingredients", GetIngredientsAsync);
        api.MapPost("/ingredients", CreateIngredientAsync);
        api.MapPut("/ingredients/{id:int}", UpdateIngredientAsync);
        api.MapDelete("/ingredients/{id:int}", DeleteIngredientAsync);

        api.MapGet("/pumps", GetPumpsAsync);
        api.MapPost("/pumps", CreatePumpAsync);
        api.MapPut("/pumps/{id:int}", UpdatePumpAsync);
        api.MapDelete("/pumps/{id:int}", DeletePumpAsync);

        api.MapGet("/sizes", GetSizesAsync);
        api.MapPost("/sizes", CreateSizeAsync);
        api.MapPut("/sizes/{id:int}", UpdateSizeAsync);
        api.MapDelete("/sizes/{id:int}", DeleteSizeAsync);

        api.MapGet("/system", GetMachineConfigurationAsync);
        api.MapPut("/system", UpdateMachineConfigurationAsync);
        api.MapPut("/system/theme", UpdateThemeAsync);
        api.MapGet("/app-info", GetApplicationInfo);

        api.MapPost("/images", UploadImageAsync)
            .DisableAntiforgery();

        api.MapGet("/dispenses/current", (DispenseService service) => Results.Ok(service.GetStatus()));
        api.MapPost("/dispenses", StartDispenseAsync);
        api.MapPost("/dispenses/current/stop", StopDispenseAsync);

        return endpoints;
    }

    private static IResult GetApplicationInfo()
    {
        var informationalVersion = System.Reflection.Assembly.GetEntryAssembly()?
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), inherit: false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .SingleOrDefault()?.InformationalVersion;
        var version = string.IsNullOrWhiteSpace(informationalVersion)
            ? "0.0.0"
            : informationalVersion.Split('+')[0];

        return Results.Ok(new ApplicationInfoResponse(version));
    }

    private static async Task<IResult> GetCocktailsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var cocktails = await CocktailQuery(db)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
        return Results.Ok(cocktails.Select(x => x.ToResponse()));
    }

    private static async Task<IResult> GetCocktailAsync(int id, AppDbContext db, CancellationToken cancellationToken)
    {
        var cocktail = await CocktailQuery(db).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return cocktail is null ? Results.NotFound() : Results.Ok(cocktail.ToResponse());
    }

    private static async Task<IResult> CreateCocktailAsync(
        CocktailWriteRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateCocktailAsync(request, db, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        var cocktail = new Cocktail { Name = request.Name.Trim(), StandardSizeId = request.StandardSizeId };
        ApplyCocktail(cocktail, request);
        db.Cocktails.Add(cocktail);
        await db.SaveChangesAsync(cancellationToken);
        var created = await CocktailQuery(db).SingleAsync(x => x.Id == cocktail.Id, cancellationToken);
        return Results.Created($"/api/cocktails/{cocktail.Id}", created.ToResponse());
    }

    private static async Task<IResult> UpdateCocktailAsync(
        int id,
        CocktailWriteRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var cocktail = await db.Cocktails.Include(x => x.Ingredients)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (cocktail is null)
        {
            return Results.NotFound();
        }

        var validation = await ValidateCocktailAsync(request, db, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        db.CocktailIngredients.RemoveRange(cocktail.Ingredients);
        cocktail.Ingredients.Clear();
        ApplyCocktail(cocktail, request);
        await db.SaveChangesAsync(cancellationToken);
        var updated = await CocktailQuery(db).SingleAsync(x => x.Id == id, cancellationToken);
        return Results.Ok(updated.ToResponse());
    }

    private static async Task<IResult> DeleteCocktailAsync(int id, AppDbContext db, CancellationToken cancellationToken)
    {
        var cocktail = await db.Cocktails.FindAsync([id], cancellationToken);
        if (cocktail is null)
        {
            return Results.NotFound();
        }

        db.Cocktails.Remove(cocktail);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static IQueryable<Cocktail> CocktailQuery(AppDbContext db) => db.Cocktails
        .AsNoTracking()
        .AsSplitQuery()
        .Include(x => x.StandardSize)
        .Include(x => x.Ingredients)
        .ThenInclude(x => x.Ingredient);

    private static void ApplyCocktail(Cocktail cocktail, CocktailWriteRequest request)
    {
        cocktail.Name = request.Name.Trim();
        cocktail.Description = NullIfWhiteSpace(request.Description);
        cocktail.ImagePath = NullIfWhiteSpace(request.ImagePath);
        cocktail.StandardSizeId = request.StandardSizeId;
        foreach (var item in request.Ingredients)
        {
            cocktail.Ingredients.Add(new CocktailIngredient
            {
                IngredientId = item.IngredientId,
                AmountMl = item.AmountMl
            });
        }
    }

    private static async Task<IResult?> ValidateCocktailAsync(
        CocktailWriteRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100)
        {
            return Validation("name", "Der Name muss zwischen 1 und 100 Zeichen lang sein.");
        }

        if (request.Description?.Length > 500)
        {
            return Validation("description", "Die Beschreibung darf höchstens 500 Zeichen lang sein.");
        }

        var size = await db.Sizes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.StandardSizeId, cancellationToken);
        if (size is null)
        {
            return Validation("standardSizeId", "Die Standardgröße existiert nicht.");
        }

        if (request.Ingredients.Count is < 1 or > DispenseService.MaximumParallelPumps)
        {
            return Validation("ingredients", $"Ein Cocktail benötigt 1 bis {DispenseService.MaximumParallelPumps} Zutaten.");
        }

        if (request.Ingredients.Any(x => x.AmountMl <= 0))
        {
            return Validation("ingredients", "Alle Zutatenmengen müssen größer als 0 ml sein.");
        }

        if (request.Ingredients.Select(x => x.IngredientId).Distinct().Count() != request.Ingredients.Count)
        {
            return Validation("ingredients", "Eine Zutat darf pro Cocktail nur einmal vorkommen.");
        }

        var ingredientIds = request.Ingredients.Select(x => x.IngredientId).ToArray();
        var existingCount = await db.Ingredients.CountAsync(x => ingredientIds.Contains(x.Id), cancellationToken);
        if (existingCount != ingredientIds.Length)
        {
            return Validation("ingredients", "Mindestens eine ausgewählte Zutat existiert nicht.");
        }

        var total = request.Ingredients.Sum(x => x.AmountMl);
        if (Math.Abs(total - size.VolumeMl) > 0.5m)
        {
            return Validation("ingredients", $"Die Zutaten müssen zusammen {size.VolumeMl} ml ergeben (aktuell {total:0.##} ml).");
        }

        return null;
    }

    private static async Task<IResult> GetIngredientsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var ingredients = await db.Ingredients.AsNoTracking().Include(x => x.Pump)
            .OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return Results.Ok(ingredients.Select(x => x.ToResponse()));
    }

    private static async Task<IResult> CreateIngredientAsync(
        IngredientWriteRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var validation = ValidateIngredient(request);
        if (validation is not null)
        {
            return validation;
        }

        var ingredient = new Ingredient
        {
            Name = request.Name.Trim(),
            AlcoholPercentage = request.AlcoholPercentage
        };
        db.Ingredients.Add(ingredient);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/ingredients/{ingredient.Id}", ingredient.ToResponse());
    }

    private static async Task<IResult> UpdateIngredientAsync(
        int id,
        IngredientWriteRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var validation = ValidateIngredient(request);
        if (validation is not null)
        {
            return validation;
        }

        var ingredient = await db.Ingredients.Include(x => x.Pump)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (ingredient is null)
        {
            return Results.NotFound();
        }

        ingredient.Name = request.Name.Trim();
        ingredient.AlcoholPercentage = request.AlcoholPercentage;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(ingredient.ToResponse());
    }

    private static IResult? ValidateIngredient(IngredientWriteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100)
        {
            return Validation("name", "Der Name muss zwischen 1 und 100 Zeichen lang sein.");
        }

        if (request.AlcoholPercentage is < 0 or > 100)
        {
            return Validation("alcoholPercentage", "Der Alkoholwert muss zwischen 0 und 100 % liegen.");
        }

        return null;
    }

    private static async Task<IResult> DeleteIngredientAsync(int id, AppDbContext db, CancellationToken cancellationToken)
    {
        var ingredient = await db.Ingredients.Include(x => x.Cocktails)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (ingredient is null)
        {
            return Results.NotFound();
        }

        if (ingredient.Cocktails.Count > 0)
        {
            return Results.Conflict(new ProblemDetails
            {
                Title = "Zutat wird verwendet",
                Detail = "Entferne die Zutat zuerst aus allen Cocktails."
            });
        }

        db.Ingredients.Remove(ingredient);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetPumpsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var pumps = await db.Pumps.AsNoTracking().Include(x => x.Ingredient)
            .OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return Results.Ok(pumps.Select(x => x.ToResponse()));
    }

    private static async Task<IResult> CreatePumpAsync(
        PumpWriteRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (await db.Pumps.CountAsync(cancellationToken) >= DispenseService.MaximumParallelPumps)
        {
            return Results.Conflict(new ProblemDetails { Title = "Pumpenlimit erreicht", Detail = "Es können maximal 8 Pumpen angelegt werden." });
        }

        var validation = await ValidatePumpAsync(request, null, db, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        var pump = new Pump { Name = request.Name.Trim() };
        ApplyPump(pump, request);
        db.Pumps.Add(pump);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(pump).Reference(x => x.Ingredient).LoadAsync(cancellationToken);
        return Results.Created($"/api/pumps/{pump.Id}", pump.ToResponse());
    }

    private static async Task<IResult> UpdatePumpAsync(
        int id,
        PumpWriteRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var pump = await db.Pumps.Include(x => x.Ingredient).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (pump is null)
        {
            return Results.NotFound();
        }

        var validation = await ValidatePumpAsync(request, id, db, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        ApplyPump(pump, request);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(pump).Reference(x => x.Ingredient).LoadAsync(cancellationToken);
        return Results.Ok(pump.ToResponse());
    }

    private static async Task<IResult?> ValidatePumpAsync(
        PumpWriteRequest request,
        int? pumpId,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100)
        {
            return Validation("name", "Der Name muss zwischen 1 und 100 Zeichen lang sein.");
        }

        if (request.GpioPin is < 0 or > 40)
        {
            return Validation("gpioPin", "Der GPIO-Pin muss zwischen 0 und 40 liegen.");
        }

        if (request.FlowRateMlPerSecond is <= 0 or > 1000)
        {
            return Validation("flowRateMlPerSecond", "Die Förderrate muss größer als 0 und höchstens 1000 ml/s sein.");
        }

        if (request.IngredientId is not null &&
            !await db.Ingredients.AnyAsync(x => x.Id == request.IngredientId, cancellationToken))
        {
            return Validation("ingredientId", "Die ausgewählte Zutat existiert nicht.");
        }

        if (await db.Pumps.AnyAsync(x => x.Id != pumpId && x.GpioPin == request.GpioPin, cancellationToken))
        {
            return Validation("gpioPin", "Dieser GPIO-Pin ist bereits einer anderen Pumpe zugeordnet.");
        }

        if (request.IngredientId is not null &&
            await db.Pumps.AnyAsync(x => x.Id != pumpId && x.IngredientId == request.IngredientId, cancellationToken))
        {
            return Validation("ingredientId", "Diese Zutat ist bereits einer anderen Pumpe zugeordnet.");
        }

        return null;
    }

    private static void ApplyPump(Pump pump, PumpWriteRequest request)
    {
        pump.Name = request.Name.Trim();
        pump.GpioPin = request.GpioPin;
        pump.FlowRateMlPerSecond = request.FlowRateMlPerSecond;
        pump.ActiveHigh = request.ActiveHigh;
        pump.IsEnabled = request.IsEnabled;
        pump.IngredientId = request.IngredientId;
    }

    private static async Task<IResult> DeletePumpAsync(int id, AppDbContext db, CancellationToken cancellationToken)
    {
        var pump = await db.Pumps.FindAsync([id], cancellationToken);
        if (pump is null)
        {
            return Results.NotFound();
        }

        db.Pumps.Remove(pump);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetSizesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var sizes = await db.Sizes.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.VolumeMl)
            .ToListAsync(cancellationToken);
        return Results.Ok(sizes.Select(x => x.ToResponse()));
    }

    private static async Task<IResult> CreateSizeAsync(
        SizeWriteRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var validation = ValidateSize(request);
        if (validation is not null)
        {
            return validation;
        }

        var size = new CocktailSize { Name = request.Name.Trim() };
        ApplySize(size, request);
        db.Sizes.Add(size);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/sizes/{size.Id}", size.ToResponse());
    }

    private static async Task<IResult> UpdateSizeAsync(
        int id,
        SizeWriteRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var validation = ValidateSize(request);
        if (validation is not null)
        {
            return validation;
        }

        var size = await db.Sizes.Include(x => x.StandardForCocktails)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (size is null)
        {
            return Results.NotFound();
        }

        if (size.VolumeMl != request.VolumeMl && size.StandardForCocktails.Count > 0)
        {
            return Results.Conflict(new ProblemDetails
            {
                Title = "Größe wird als Standard verwendet",
                Detail = "Ändere zuerst die Standardgröße der betroffenen Cocktails oder lege eine neue Größe an."
            });
        }

        ApplySize(size, request);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(size.ToResponse());
    }

    private static IResult? ValidateSize(SizeWriteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 50)
        {
            return Validation("name", "Der Name muss zwischen 1 und 50 Zeichen lang sein.");
        }

        return request.VolumeMl is < 10 or > 2000
            ? Validation("volumeMl", "Das Volumen muss zwischen 10 und 2000 ml liegen.")
            : null;
    }

    private static void ApplySize(CocktailSize size, SizeWriteRequest request)
    {
        size.Name = request.Name.Trim();
        size.VolumeMl = request.VolumeMl;
        size.SortOrder = request.SortOrder;
    }

    private static async Task<IResult> DeleteSizeAsync(int id, AppDbContext db, CancellationToken cancellationToken)
    {
        var size = await db.Sizes.Include(x => x.StandardForCocktails)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (size is null)
        {
            return Results.NotFound();
        }

        if (size.StandardForCocktails.Count > 0)
        {
            return Results.Conflict(new ProblemDetails
            {
                Title = "Größe wird verwendet",
                Detail = "Die Größe ist noch Standardgröße mindestens eines Cocktails."
            });
        }

        db.Sizes.Remove(size);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetMachineConfigurationAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var configuration = await db.MachineConfigurations.AsNoTracking()
            .SingleAsync(x => x.Id == MachineConfiguration.SingletonId, cancellationToken);
        return Results.Ok(new MachineConfigurationResponse(
            configuration.PumpDriver,
            configuration.PinNumberingScheme,
            configuration.Theme,
            DispenseService.MaximumParallelPumps));
    }

    private static async Task<IResult> UpdateMachineConfigurationAsync(
        MachineConfigurationRequest request,
        AppDbContext db,
        DispenseService dispenseService,
        CancellationToken cancellationToken)
    {
        if (dispenseService.IsRunning)
        {
            return Results.Conflict(new ProblemDetails { Title = "Ausschank aktiv", Detail = "Die Hardwarekonfiguration kann während eines Ausschanks nicht geändert werden." });
        }

        var driver = PumpDriverNames.All.SingleOrDefault(x => x.Equals(request.PumpDriver, StringComparison.OrdinalIgnoreCase));
        var scheme = PinNumberingSchemes.All.SingleOrDefault(x => x.Equals(request.PinNumberingScheme, StringComparison.OrdinalIgnoreCase));
        var theme = ThemeNames.All.SingleOrDefault(x => x.Equals(request.Theme, StringComparison.OrdinalIgnoreCase));
        if (driver is null || scheme is null || theme is null)
        {
            return Validation("configuration", "Treiber muss Dummy oder Gpio, die Nummerierung Logical oder Board und das Design Light oder Dark sein.");
        }

        var configuration = await db.MachineConfigurations.SingleAsync(x => x.Id == MachineConfiguration.SingletonId, cancellationToken);
        configuration.PumpDriver = driver;
        configuration.PinNumberingScheme = scheme;
        configuration.Theme = theme;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new MachineConfigurationResponse(driver, scheme, theme, DispenseService.MaximumParallelPumps));
    }

    private static async Task<IResult> UpdateThemeAsync(
        ThemeRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var theme = ThemeNames.All.SingleOrDefault(x => x.Equals(request.Theme, StringComparison.OrdinalIgnoreCase));
        if (theme is null)
        {
            return Validation("theme", "Das Design muss Light oder Dark sein.");
        }

        var configuration = await db.MachineConfigurations.SingleAsync(x => x.Id == MachineConfiguration.SingletonId, cancellationToken);
        configuration.Theme = theme;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new MachineConfigurationResponse(
            configuration.PumpDriver,
            configuration.PinNumberingScheme,
            configuration.Theme,
            DispenseService.MaximumParallelPumps));
    }

    private static async Task<IResult> UploadImageAsync(
        IFormFile file,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        const long maximumBytes = 5 * 1024 * 1024;
        var allowedTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
            ["image/gif"] = ".gif"
        };

        if (file.Length is <= 0 or > maximumBytes || !allowedTypes.TryGetValue(file.ContentType, out var extension))
        {
            return Validation("file", "Erlaubt sind JPG, PNG, WebP oder GIF bis maximal 5 MB.");
        }

        var directory = Path.Combine(environment.WebRootPath, "uploads");
        Directory.CreateDirectory(directory);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var targetPath = Path.Combine(directory, fileName);
        await using var stream = File.Create(targetPath);
        await file.CopyToAsync(stream, cancellationToken);
        return Results.Ok(new ImageUploadResponse($"/uploads/{fileName}"));
    }

    private static async Task<IResult> StartDispenseAsync(
        StartDispenseRequest request,
        DispenseService service,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Accepted("/api/dispenses/current", await service.StartAsync(request.CocktailId, request.SizeId, cancellationToken));
        }
        catch (DispenseValidationException exception)
        {
            return Results.BadRequest(new ProblemDetails { Title = "Ausschank nicht möglich", Detail = exception.Message });
        }
        catch (DispenseConflictException exception)
        {
            return Results.Conflict(new ProblemDetails { Title = "Ausschank läuft bereits", Detail = exception.Message });
        }
    }

    private static async Task<IResult> StopDispenseAsync(DispenseService service) =>
        Results.Ok(await service.StopAsync());

    private static IResult Validation(string key, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [key] = [message] });

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
