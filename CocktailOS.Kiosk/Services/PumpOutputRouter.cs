using CocktailOS.Kiosk.Models;

namespace CocktailOS.Kiosk.Services;

public sealed class PumpOutputRouter(DummyPumpOutput dummy, GpioPumpOutput gpio) : IPumpOutput
{
    public ValueTask SetStateAsync(HardwareSettings settings, PumpChannel channel, bool isOn, CancellationToken cancellationToken) =>
        Select(settings).SetStateAsync(settings, channel, isOn, cancellationToken);

    public async ValueTask StopAllAsync(HardwareSettings settings, CancellationToken cancellationToken)
    {
        await dummy.StopAllAsync(settings, cancellationToken);
        await gpio.StopAllAsync(settings, cancellationToken);
    }

    private IPumpOutput Select(HardwareSettings settings) => settings.PumpDriver.Equals(PumpDriverNames.Gpio, StringComparison.OrdinalIgnoreCase) ? gpio : dummy;
}
