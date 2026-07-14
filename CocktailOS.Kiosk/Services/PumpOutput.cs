using CocktailOS.Kiosk.Models;
using System.Device.Gpio;

namespace CocktailOS.Kiosk.Services;

public sealed record PumpChannel(int PumpId, string Name, int GpioPin, bool ActiveHigh);
public sealed record HardwareSettings(string PumpDriver, string PinNumberingScheme);

public interface IPumpOutput
{
    ValueTask SetStateAsync(HardwareSettings settings, PumpChannel channel, bool isOn, CancellationToken cancellationToken);
    ValueTask StopAllAsync(HardwareSettings settings, CancellationToken cancellationToken);
}

public sealed class PumpOutputRouter(DummyPumpOutput dummy, GpioPumpOutput gpio) : IPumpOutput
{
    public ValueTask SetStateAsync(
        HardwareSettings settings,
        PumpChannel channel,
        bool isOn,
        CancellationToken cancellationToken) =>
        Select(settings).SetStateAsync(settings, channel, isOn, cancellationToken);

    public async ValueTask StopAllAsync(HardwareSettings settings, CancellationToken cancellationToken)
    {
        // Always stop both implementations. This keeps a runtime driver change safe.
        await dummy.StopAllAsync(settings, cancellationToken);
        await gpio.StopAllAsync(settings, cancellationToken);
    }

    private IPumpOutput Select(HardwareSettings settings) =>
        settings.PumpDriver.Equals(PumpDriverNames.Gpio, StringComparison.OrdinalIgnoreCase) ? gpio : dummy;
}

public sealed class DummyPumpOutput(ILogger<DummyPumpOutput> logger) : IPumpOutput
{
    private readonly HashSet<int> _activePumps = [];
    private readonly object _sync = new();

    public ValueTask SetStateAsync(
        HardwareSettings settings,
        PumpChannel channel,
        bool isOn,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (isOn)
            {
                _activePumps.Add(channel.PumpId);
            }
            else
            {
                _activePumps.Remove(channel.PumpId);
            }
        }

        logger.LogInformation(
            "Dummy-Pumpe {PumpName} (GPIO {GpioPin}, {Polarity}) ist {State}",
            channel.Name,
            channel.GpioPin,
            channel.ActiveHigh ? "active HIGH" : "active LOW",
            isOn ? "AN" : "AUS");

        return ValueTask.CompletedTask;
    }

    public ValueTask StopAllAsync(HardwareSettings settings, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (_activePumps.Count > 0)
            {
                logger.LogWarning("Dummy-Not-Aus: {Count} aktive Pumpen werden gestoppt", _activePumps.Count);
                _activePumps.Clear();
            }
        }

        return ValueTask.CompletedTask;
    }
}

public sealed class GpioPumpOutput(ILogger<GpioPumpOutput> logger) : IPumpOutput, IDisposable
{
    private readonly object _sync = new();
    private readonly HashSet<int> _openedPins = [];
    private readonly Dictionary<int, bool> _pinActiveHigh = [];
    private GpioController? _controller;

    public ValueTask SetStateAsync(
        HardwareSettings settings,
        PumpChannel channel,
        bool isOn,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var pin = ResolvePin(settings.PinNumberingScheme, channel.GpioPin);

        lock (_sync)
        {
            _controller ??= new GpioController();
            if (_openedPins.Add(pin))
            {
                var inactive = channel.ActiveHigh ? PinValue.Low : PinValue.High;
                _controller.OpenPin(pin, PinMode.Output, inactive);
            }

            _pinActiveHigh[pin] = channel.ActiveHigh;
            var value = isOn == channel.ActiveHigh ? PinValue.High : PinValue.Low;
            _controller.Write(pin, value);
        }

        logger.LogInformation("GPIO {Pin} für {PumpName} ist {State}", pin, channel.Name, isOn ? "AN" : "AUS");
        return ValueTask.CompletedTask;
    }

    public ValueTask StopAllAsync(HardwareSettings settings, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (_controller is null)
            {
                return ValueTask.CompletedTask;
            }

            foreach (var pin in _openedPins)
            {
                var activeHigh = _pinActiveHigh.GetValueOrDefault(pin);
                _controller.Write(pin, activeHigh ? PinValue.Low : PinValue.High);
            }
        }

        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_controller is null)
            {
                return;
            }

            foreach (var pin in _openedPins)
            {
                var activeHigh = _pinActiveHigh.GetValueOrDefault(pin);
                _controller.Write(pin, activeHigh ? PinValue.Low : PinValue.High);
                _controller.ClosePin(pin);
            }

            _controller.Dispose();
            _controller = null;
            _openedPins.Clear();
            _pinActiveHigh.Clear();
        }
    }

    private static int ResolvePin(string scheme, int configuredPin)
    {
        if (!scheme.Equals(PinNumberingSchemes.Board, StringComparison.OrdinalIgnoreCase))
        {
            return configuredPin;
        }

        return BoardToBcmPins.TryGetValue(configuredPin, out var bcmPin)
            ? bcmPin
            : throw new InvalidOperationException($"Der physische Pin {configuredPin} ist kein GPIO-Pin des 40-Pin-Headers.");
    }

    private static readonly IReadOnlyDictionary<int, int> BoardToBcmPins = new Dictionary<int, int>
    {
        [3] = 2, [5] = 3, [7] = 4, [8] = 14, [10] = 15, [11] = 17, [12] = 18,
        [13] = 27, [15] = 22, [16] = 23, [18] = 24, [19] = 10, [21] = 9,
        [22] = 25, [23] = 11, [24] = 8, [26] = 7, [27] = 0, [28] = 1,
        [29] = 5, [31] = 6, [32] = 12, [33] = 13, [35] = 19, [36] = 16,
        [37] = 26, [38] = 20, [40] = 21
    };
}
