namespace CocktailOS.Kiosk.Contracts;

public sealed record AdminAccessStatusResponse(bool PinConfigured, bool IsAuthenticated);
