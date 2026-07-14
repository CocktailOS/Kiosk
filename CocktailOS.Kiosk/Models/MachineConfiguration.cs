namespace CocktailOS.Kiosk.Models;

public sealed class MachineConfiguration
{
    public const int SingletonId = 1;
    public int Id { get; set; } = SingletonId;
    public required string PumpDriver { get; set; } = PumpDriverNames.Dummy;
    public required string PinNumberingScheme { get; set; } = PinNumberingSchemes.Logical;
    public required string Theme { get; set; } = ThemeNames.Dark;
}
