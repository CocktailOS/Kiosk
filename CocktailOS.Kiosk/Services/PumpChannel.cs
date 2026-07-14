namespace CocktailOS.Kiosk.Services;
public sealed record PumpChannel(int PumpId, string Name, int GpioPin, bool ActiveHigh);
