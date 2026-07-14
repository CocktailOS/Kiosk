namespace CocktailOS.Kiosk.Contracts;

public sealed record CocktailWriteRequest(
    string Name,
    string? Description,
    string? ImagePath,
    int StandardSizeId,
    IReadOnlyList<CocktailIngredientWriteRequest> Ingredients);

public sealed record CocktailIngredientWriteRequest(int IngredientId, decimal AmountMl);

public sealed record CocktailResponse(
    int Id,
    string Name,
    string? Description,
    string? ImagePath,
    SizeResponse StandardSize,
    IReadOnlyList<CocktailIngredientResponse> Ingredients,
    decimal AlcoholPercentage);

public sealed record CocktailIngredientResponse(int IngredientId, string Name, decimal AmountMl, decimal? AlcoholPercentage);

public sealed record IngredientWriteRequest(string Name, decimal? AlcoholPercentage);
public sealed record IngredientResponse(int Id, string Name, decimal? AlcoholPercentage, bool HasPump);

public sealed record PumpWriteRequest(
    string Name,
    int GpioPin,
    decimal FlowRateMlPerSecond,
    bool ActiveHigh,
    bool IsEnabled,
    int? IngredientId);

public sealed record PumpResponse(
    int Id,
    string Name,
    int GpioPin,
    decimal FlowRateMlPerSecond,
    bool ActiveHigh,
    bool IsEnabled,
    int? IngredientId,
    string? IngredientName);

public sealed record SizeWriteRequest(string Name, int VolumeMl, int SortOrder);
public sealed record SizeResponse(int Id, string Name, int VolumeMl, int SortOrder);

public sealed record MachineConfigurationRequest(string PumpDriver, string PinNumberingScheme, string Theme);
public sealed record ThemeRequest(string Theme);
public sealed record MachineConfigurationResponse(string PumpDriver, string PinNumberingScheme, string Theme, int MaximumParallelPumps);
public sealed record ApplicationInfoResponse(string Version);

public sealed record StartDispenseRequest(int CocktailId, int SizeId);

public sealed record DispenseStatusResponse(
    Guid? Id,
    string Status,
    string? CocktailName,
    string? SizeName,
    DateTimeOffset? StartedAt,
    double EstimatedDurationSeconds,
    double Progress,
    string? Error,
    IReadOnlyList<DispenseStepResponse> Steps);

public sealed record DispenseStepResponse(
    string IngredientName,
    decimal AmountMl,
    int PumpId,
    double DurationSeconds);

public sealed record ImageUploadResponse(string Path);
