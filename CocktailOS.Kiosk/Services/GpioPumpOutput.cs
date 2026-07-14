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
        logger.LogInformation("gpio {Pin} für {PumpName} ist {State}", pin, channel.Name, isOn ? "an" : "aus"); return ValueTask.CompletedTask;
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
        ? configuredPin : RaspberryPiHeaderPins.BoardToBcm.TryGetValue(configuredPin, out var bcmPin) ? bcmPin : throw new InvalidOperationException($"Der physische Pin {configuredPin} ist kein gpio-Pin des 40-Pin-Headers.");
}
