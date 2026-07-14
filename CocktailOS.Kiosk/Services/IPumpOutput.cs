namespace CocktailOS.Kiosk.Services;
public interface IPumpOutput
{
    ValueTask SetStateAsync(HardwareSettings settings, PumpChannel channel, bool isOn, CancellationToken cancellationToken);
    ValueTask StopAllAsync(HardwareSettings settings, CancellationToken cancellationToken);
}
