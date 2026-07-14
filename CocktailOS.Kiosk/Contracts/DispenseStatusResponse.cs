namespace CocktailOS.Kiosk.Contracts;
public sealed record DispenseStatusResponse(Guid? Id, string Status, string? CocktailName, string? SizeName, DateTimeOffset? StartedAt, double EstimatedDurationSeconds, double Progress, string? Error, IReadOnlyList<DispenseStepResponse> Steps, string Mode);
