using CocktailOS.Kiosk.Data;
using CocktailOS.Kiosk.Models;
using CocktailOS.Kiosk.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CocktailOS.Kiosk.Tests;

internal sealed class KioskApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseDirectory;
    private readonly string _databasePath;

    public FakePumpOutput PumpOutput { get; } = new();

    public KioskApplicationFactory()
    {
        _databaseDirectory = Path.Combine(Path.GetTempPath(), "cocktailos-kiosk-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_databaseDirectory);
        _databasePath = Path.Combine(_databaseDirectory, "cocktailos.db");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={_databasePath}"));
            services.RemoveAll<IPumpOutput>();
            services.AddSingleton<IPumpOutput>(PumpOutput);
        });
    }

    public async Task UseGpioDriverAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuration = await db.MachineConfigurations.SingleAsync();
        configuration.PumpDriver = PumpDriverNames.Gpio;
        await db.SaveChangesAsync();
    }

    public async Task<Pump> GetPumpAsync(int id = 1)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Pumps.AsNoTracking().SingleAsync(x => x.Id == id);
    }

    public async Task UpdatePumpAsync(int id, Action<Pump> update)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pump = await db.Pumps.SingleAsync(x => x.Id == id);
        update(pump);
        await db.SaveChangesAsync();
    }

    public async Task<int> CreateIngredientAsync(string name)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ingredient = new Ingredient { Name = name, AlcoholPercentage = 0 };
        db.Ingredients.Add(ingredient);
        await db.SaveChangesAsync();
        return ingredient.Id;
    }

    public async Task<Ingredient> GetIngredientAsync(string name)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Ingredients.AsNoTracking().SingleAsync(x => x.Name == name);
    }

    public async Task UpdateIngredientAsync(string name, Action<Ingredient> update)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ingredient = await db.Ingredients.SingleAsync(x => x.Name == name);
        update(ingredient);
        await db.SaveChangesAsync();
    }

    public async Task SetAllPumpFlowRatesAsync(decimal flowRateMlPerSecond)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pumps = await db.Pumps.ToListAsync();
        foreach (var pump in pumps) pump.FlowRateMlPerSecond = flowRateMlPerSecond;
        await db.SaveChangesAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        try { Directory.Delete(_databaseDirectory, recursive: true); } catch (IOException) { }
    }
}
