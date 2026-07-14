using CocktailOS.Kiosk.Models;
using System.Device.Gpio;

namespace CocktailOS.Kiosk.Services;

public sealed class GpioPumpOutput(ILogger<GpioPumpOutput> logger) : IPumpOutput, IDisposable
{
    private readonly object _sync = new();
    private readonly HashSet<int> _openedPins = [];
    private readonly Dictionary<int, bool> _pinActiveHigh = [];
    private GpioController? _controller;

    public ValueTask SetStateAsync(HardwareSettings settings, PumpChannel channel, bool isOn, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested(); var pin = ResolvePin(settings.PinNumberingScheme, channel.GpioPin);
        lock (_sync)
        {
            _controller ??= new GpioController();
            if (_openedPins.Add(pin)) _controller.OpenPin(pin, PinMode.Output, channel.ActiveHigh ? PinValue.Low : PinValue.High);
            _pinActiveHigh[pin] = channel.ActiveHigh;
            _controller.Write(pin, isOn == channel.ActiveHigh ? PinValue.High : PinValue.Low);
        }
        logger.LogInformation("GPIO {Pin} für {PumpName} ist {State}", pin, channel.Name, isOn ? "AN" : "AUS"); return ValueTask.CompletedTask;
    }

    public ValueTask StopAllAsync(HardwareSettings settings, CancellationToken cancellationToken)
    {
        lock (_sync) { if (_controller is null) return ValueTask.CompletedTask; foreach (var pin in _openedPins) _controller.Write(pin, _pinActiveHigh.GetValueOrDefault(pin) ? PinValue.Low : PinValue.High); }
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_controller is null) return;
            foreach (var pin in _openedPins) { _controller.Write(pin, _pinActiveHigh.GetValueOrDefault(pin) ? PinValue.Low : PinValue.High); _controller.ClosePin(pin); }
            _controller.Dispose(); _controller = null; _openedPins.Clear(); _pinActiveHigh.Clear();
        }
    }

    private static int ResolvePin(string scheme, int configuredPin) => !scheme.Equals(PinNumberingSchemes.Board, StringComparison.OrdinalIgnoreCase)
        ? configuredPin : BoardToBcmPins.TryGetValue(configuredPin, out var bcmPin) ? bcmPin : throw new InvalidOperationException($"Der physische Pin {configuredPin} ist kein GPIO-Pin des 40-Pin-Headers.");

    private static readonly IReadOnlyDictionary<int, int> BoardToBcmPins = new Dictionary<int, int>
    {
        [3] = 2, [5] = 3, [7] = 4, [8] = 14, [10] = 15, [11] = 17, [12] = 18, [13] = 27, [15] = 22, [16] = 23, [18] = 24, [19] = 10, [21] = 9, [22] = 25, [23] = 11, [24] = 8, [26] = 7, [27] = 0, [28] = 1, [29] = 5, [31] = 6, [32] = 12, [33] = 13, [35] = 19, [36] = 16, [37] = 26, [38] = 20, [40] = 21
    };
}
