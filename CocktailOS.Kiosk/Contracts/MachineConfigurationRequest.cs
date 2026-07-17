namespace CocktailOS.Kiosk.Contracts;
public sealed record MachineConfigurationRequest(string PumpDriver, string PinNumberingScheme, string Theme, bool NetworkAccessEnabled, string? NetworkAccessPin);
