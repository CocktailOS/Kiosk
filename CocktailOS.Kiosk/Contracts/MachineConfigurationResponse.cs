namespace CocktailOS.Kiosk.Contracts;
public sealed record MachineConfigurationResponse(string PumpDriver, string PinNumberingScheme, string Theme, bool NetworkAccessEnabled, bool NetworkAccessPinConfigured, string? NetworkAddress, int MaximumParallelPumps);
