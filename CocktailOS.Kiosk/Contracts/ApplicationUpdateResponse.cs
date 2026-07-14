namespace CocktailOS.Kiosk.Contracts;

public sealed record ApplicationUpdateResponse(
    bool IsAvailable,
    string CurrentVersion,
    string? LatestVersion,
    string? ReleaseUrl);
