namespace CocktailOS.Kiosk.Services;

public sealed class DummyPumpOutput(ILogger<DummyPumpOutput> logger) : IPumpOutput
{
    private readonly HashSet<int> _activePumps = [];
    private readonly object _sync = new();

    public ValueTask SetStateAsync(HardwareSettings settings, PumpChannel channel, bool isOn, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync) { if (isOn) _activePumps.Add(channel.PumpId); else _activePumps.Remove(channel.PumpId); }
        logger.LogInformation("Dummy-Pumpe {PumpName} (gpio {GpioPin}, {Polarity}) ist {State}", channel.Name, channel.GpioPin, channel.ActiveHigh ? "active high" : "active low", isOn ? "an" : "aus");
        return ValueTask.CompletedTask;
    }

    public ValueTask StopAllAsync(HardwareSettings settings, CancellationToken cancellationToken)
    {
        lock (_sync) { if (_activePumps.Count > 0) { logger.LogWarning("Dummy-Not-Aus: {Count} aktive Pumpen werden gestoppt", _activePumps.Count); _activePumps.Clear(); } }
        return ValueTask.CompletedTask;
    }
}
