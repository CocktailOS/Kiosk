using System.Collections.Concurrent;
using CocktailOS.Kiosk.Services;

namespace CocktailOS.Kiosk.Tests;

internal sealed class FakePumpOutput : IPumpOutput
{
    private readonly ConcurrentDictionary<int, bool> _states = new();
    private readonly TaskCompletionSource _pumpStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public IReadOnlyDictionary<int, bool> States => _states;
    public bool FailOnStart { get; set; }

    public ValueTask SetStateAsync(HardwareSettings settings, PumpChannel channel, bool isOn, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _states[channel.PumpId] = isOn;
        if (isOn)
        {
            _pumpStarted.TrySetResult();
            if (FailOnStart) throw new InvalidOperationException("Simulierter Pumpenfehler");
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask StopAllAsync(HardwareSettings settings, CancellationToken cancellationToken)
    {
        foreach (var pumpId in _states.Keys) _states[pumpId] = false;
        return ValueTask.CompletedTask;
    }

    public Task WaitUntilStartedAsync(TimeSpan timeout) => _pumpStarted.Task.WaitAsync(timeout);
}
