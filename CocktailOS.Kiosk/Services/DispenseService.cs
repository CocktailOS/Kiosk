using CocktailOS.Kiosk.Contracts;
using CocktailOS.Kiosk.Data;
using CocktailOS.Kiosk.Models;
using Microsoft.EntityFrameworkCore;

namespace CocktailOS.Kiosk.Services;

public sealed class DispenseService(
    IServiceScopeFactory scopeFactory,
    IPumpOutput pumpOutput,
    ILogger<DispenseService> logger)
{
    public const int MaximumParallelPumps = 8;

    private readonly object _sync = new();
    private CancellationTokenSource? _cancellation;
    private Task? _runningTask;
    private DispenseState _state = DispenseState.Idle;

    public async Task<DispenseStatusResponse> StartAsync(
        int cocktailId,
        int sizeId,
        CancellationToken cancellationToken)
    {
        DispensePlan plan;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            plan = await CreatePlanAsync(db, cocktailId, sizeId, cancellationToken);
        }

        lock (_sync)
        {
            if (_runningTask is { IsCompleted: false })
            {
                throw new DispenseConflictException("Es läuft bereits ein Ausschank.");
            }

            _cancellation?.Dispose();
            _cancellation = new CancellationTokenSource();
            _state = DispenseState.Running(plan);
            _runningTask = Task.Run(() => ExecuteAsync(plan, _cancellation.Token), CancellationToken.None);
            return CreateResponse(_state);
        }
    }

    public DispenseStatusResponse GetStatus()
    {
        lock (_sync)
        {
            return CreateResponse(_state);
        }
    }

    public async Task<DispenseStatusResponse> StopAsync()
    {
        Task? runningTask;
        lock (_sync)
        {
            if (_runningTask is not { IsCompleted: false })
            {
                return CreateResponse(_state);
            }

            _cancellation?.Cancel();
            runningTask = _runningTask;
        }

        try
        {
            await runningTask;
        }
        catch (OperationCanceledException)
        {
            // Expected when the physical stop button is used.
        }

        return GetStatus();
    }

    public bool IsRunning
    {
        get
        {
            lock (_sync)
            {
                return _runningTask is { IsCompleted: false };
            }
        }
    }

    private async Task ExecuteAsync(DispensePlan plan, CancellationToken cancellationToken)
    {
        try
        {
            using var safetyCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var tasks = plan.Steps
                .Select(step => RunPumpWithSafetyAsync(plan.Hardware, step, safetyCancellation))
                .ToArray();
            await Task.WhenAll(tasks);

            lock (_sync)
            {
                _state = _state with { Status = DispenseStatuses.Completed, CompletedAt = DateTimeOffset.UtcNow };
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            lock (_sync)
            {
                _state = _state with { Status = DispenseStatuses.Stopped, CompletedAt = DateTimeOffset.UtcNow };
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Der Ausschank {DispenseId} ist fehlgeschlagen", plan.Id);
            lock (_sync)
            {
                _state = _state with
                {
                    Status = DispenseStatuses.Failed,
                    Error = "Die Pumpensteuerung ist fehlgeschlagen. Bitte Hardware und Konfiguration prüfen.",
                    CompletedAt = DateTimeOffset.UtcNow
                };
            }
        }
        finally
        {
            try
            {
                await pumpOutput.StopAllAsync(plan.Hardware, CancellationToken.None);
            }
            catch (Exception exception)
            {
                logger.LogCritical(exception, "Nicht alle Pumpen konnten sicher abgeschaltet werden");
                lock (_sync)
                {
                    _state = _state with
                    {
                        Status = DispenseStatuses.Failed,
                        Error = "NOT-AUS fehlgeschlagen. Stromversorgung der Pumpen sofort trennen.",
                        CompletedAt = DateTimeOffset.UtcNow
                    };
                }
            }
        }
    }

    private async Task RunPumpWithSafetyAsync(
        HardwareSettings hardware,
        DispenseStep step,
        CancellationTokenSource safetyCancellation)
    {
        try
        {
            await RunPumpAsync(hardware, step, safetyCancellation.Token);
        }
        catch
        {
            // One failing output must immediately cancel every other active pump.
            await safetyCancellation.CancelAsync();
            throw;
        }
    }

    private async Task RunPumpAsync(HardwareSettings hardware, DispenseStep step, CancellationToken cancellationToken)
    {
        await pumpOutput.SetStateAsync(hardware, step.Channel, true, cancellationToken);
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(step.DurationSeconds), cancellationToken);
        }
        finally
        {
            await pumpOutput.SetStateAsync(hardware, step.Channel, false, CancellationToken.None);
        }
    }

    private static async Task<DispensePlan> CreatePlanAsync(
        AppDbContext db,
        int cocktailId,
        int sizeId,
        CancellationToken cancellationToken)
    {
        var cocktail = await db.Cocktails
            .AsNoTracking()
            .Include(x => x.StandardSize)
            .Include(x => x.Ingredients)
            .ThenInclude(x => x.Ingredient)
            .SingleOrDefaultAsync(x => x.Id == cocktailId, cancellationToken)
            ?? throw new DispenseValidationException("Der ausgewählte Cocktail wurde nicht gefunden.");

        var size = await db.Sizes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == sizeId, cancellationToken)
            ?? throw new DispenseValidationException("Die ausgewählte Größe wurde nicht gefunden.");

        if (cocktail.Ingredients.Count == 0)
        {
            throw new DispenseValidationException("Der Cocktail enthält noch keine Zutaten.");
        }

        if (cocktail.Ingredients.Count > MaximumParallelPumps)
        {
            throw new DispenseValidationException($"Ein Cocktail darf höchstens {MaximumParallelPumps} Zutaten enthalten.");
        }

        var ingredientIds = cocktail.Ingredients.Select(x => x.IngredientId).ToArray();
        var pumps = await db.Pumps
            .AsNoTracking()
            .Where(x => x.IsEnabled && x.IngredientId != null && ingredientIds.Contains(x.IngredientId.Value))
            .ToDictionaryAsync(x => x.IngredientId!.Value, cancellationToken);

        var missing = cocktail.Ingredients
            .Where(x => !pumps.ContainsKey(x.IngredientId))
            .Select(x => x.Ingredient.Name)
            .ToArray();

        if (missing.Length > 0)
        {
            throw new DispenseValidationException($"Keine aktive Pumpe für: {string.Join(", ", missing)}.");
        }

        var configuration = await db.MachineConfigurations.AsNoTracking()
            .SingleAsync(x => x.Id == MachineConfiguration.SingletonId, cancellationToken);

        var scale = (decimal)size.VolumeMl / cocktail.StandardSize.VolumeMl;
        var steps = cocktail.Ingredients.Select(item =>
        {
            var pump = pumps[item.IngredientId];
            var amountMl = decimal.Round(item.AmountMl * scale, 2);
            return new DispenseStep(
                item.Ingredient.Name,
                amountMl,
                new PumpChannel(pump.Id, pump.Name, pump.GpioPin, pump.ActiveHigh),
                (double)(amountMl / pump.FlowRateMlPerSecond));
        }).ToArray();

        return new DispensePlan(
            Guid.NewGuid(),
            cocktail.Name,
            size.Name,
            new HardwareSettings(configuration.PumpDriver, configuration.PinNumberingScheme),
            steps);
    }

    private static DispenseStatusResponse CreateResponse(DispenseState state)
    {
        var progress = state.Status switch
        {
            DispenseStatuses.Completed => 1d,
            DispenseStatuses.Running when state.StartedAt is not null && state.EstimatedDurationSeconds > 0 =>
                Math.Clamp((DateTimeOffset.UtcNow - state.StartedAt.Value).TotalSeconds / state.EstimatedDurationSeconds, 0, 0.99),
            _ => 0d
        };

        return new DispenseStatusResponse(
            state.Id,
            state.Status,
            state.CocktailName,
            state.SizeName,
            state.StartedAt,
            state.EstimatedDurationSeconds,
            progress,
            state.Error,
            state.Steps.Select(x => new DispenseStepResponse(
                x.IngredientName,
                x.AmountMl,
                x.Channel.PumpId,
                x.DurationSeconds)).ToArray());
    }

    private sealed record DispensePlan(
        Guid Id,
        string CocktailName,
        string SizeName,
        HardwareSettings Hardware,
        IReadOnlyList<DispenseStep> Steps);

    private sealed record DispenseStep(
        string IngredientName,
        decimal AmountMl,
        PumpChannel Channel,
        double DurationSeconds);

    private sealed record DispenseState(
        Guid? Id,
        string Status,
        string? CocktailName,
        string? SizeName,
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt,
        double EstimatedDurationSeconds,
        string? Error,
        IReadOnlyList<DispenseStep> Steps)
    {
        public static DispenseState Idle { get; } = new(
            null, DispenseStatuses.Idle, null, null, null, null, 0, null, []);

        public static DispenseState Running(DispensePlan plan) => new(
            plan.Id,
            DispenseStatuses.Running,
            plan.CocktailName,
            plan.SizeName,
            DateTimeOffset.UtcNow,
            null,
            plan.Steps.Max(x => x.DurationSeconds),
            null,
            plan.Steps);
    }
}

public static class DispenseStatuses
{
    public const string Idle = "idle";
    public const string Running = "running";
    public const string Completed = "completed";
    public const string Stopped = "stopped";
    public const string Failed = "failed";
}

public sealed class DispenseValidationException(string message) : Exception(message);
public sealed class DispenseConflictException(string message) : Exception(message);
