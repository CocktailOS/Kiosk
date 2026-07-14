namespace CocktailOS.Kiosk.Contracts;
public sealed record MachineConfigurationResponse(string PumpDriver, string PinNumberingScheme, string Theme, int MaximumParallelPumps);
