namespace CocktailOS.Kiosk.Services;

public sealed class NetworkAccessPolicy
{
    private int _enabled;

    public bool IsEnabled => Volatile.Read(ref _enabled) == 1;

    public void SetEnabled(bool enabled) => Volatile.Write(ref _enabled, enabled ? 1 : 0);
}
