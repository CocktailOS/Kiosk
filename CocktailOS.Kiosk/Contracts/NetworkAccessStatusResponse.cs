namespace CocktailOS.Kiosk.Contracts;

public sealed record NetworkAccessStatusResponse(bool NetworkAccessEnabled, bool PinConfigured, bool RequiresPin, bool IsAuthenticated, string Theme);
