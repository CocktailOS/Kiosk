using CocktailOS.Kiosk.Models;
using Microsoft.EntityFrameworkCore;

namespace CocktailOS.Kiosk.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Cocktail> Cocktails => Set<Cocktail>();
    public DbSet<CocktailIngredient> CocktailIngredients => Set<CocktailIngredient>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<Pump> Pumps => Set<Pump>();
    public DbSet<CocktailSize> Sizes => Set<CocktailSize>();
    public DbSet<MachineConfiguration> MachineConfigurations => Set<MachineConfiguration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cocktail>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.ImagePath).HasMaxLength(300);
            entity.HasOne(x => x.StandardSize)
                .WithMany(x => x.StandardForCocktails)
                .HasForeignKey(x => x.StandardSizeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CocktailIngredient>(entity =>
        {
            entity.HasKey(x => new { x.CocktailId, x.IngredientId });
            entity.Property(x => x.AmountMl).HasPrecision(8, 2);
            entity.HasOne(x => x.Cocktail)
                .WithMany(x => x.Ingredients)
                .HasForeignKey(x => x.CocktailId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Ingredient)
                .WithMany(x => x.Cocktails)
                .HasForeignKey(x => x.IngredientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.AlcoholPercentage).HasPrecision(5, 2);
            entity.Property(x => x.BottleSizeMl).HasPrecision(8, 2).HasDefaultValue(InventoryDefaults.DefaultBottleSizeMl);
            entity.Property(x => x.RemainingVolumeMl).HasPrecision(8, 2).HasDefaultValue(InventoryDefaults.DefaultBottleSizeMl);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Pump>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.FlowRateMlPerSecond).HasPrecision(8, 3);
            entity.Property(x => x.FlowRateSource).HasMaxLength(20).HasDefaultValue(FlowRateSources.Manual).IsRequired();
            entity.HasIndex(x => x.GpioPin).IsUnique();
            entity.HasIndex(x => x.IngredientId).IsUnique();
            entity.HasOne(x => x.Ingredient)
                .WithOne(x => x.Pump)
                .HasForeignKey<Pump>(x => x.IngredientId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.CalibratedIngredient)
                .WithMany()
                .HasForeignKey(x => x.CalibratedIngredientId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CocktailSize>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.VolumeMl).IsUnique();
        });

        modelBuilder.Entity<MachineConfiguration>(entity =>
        {
            entity.Property(x => x.PumpDriver).HasMaxLength(20).IsRequired();
            entity.Property(x => x.PinNumberingScheme).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Theme).HasMaxLength(10).IsRequired();
            entity.Property(x => x.NetworkAccessEnabled).HasDefaultValue(false);
            entity.Property(x => x.NetworkAccessPinHash).HasMaxLength(200);
            entity.Property(x => x.IntroTourCompleted).HasDefaultValue(false);
        });
    }
}
