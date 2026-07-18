using CocktailOS.Kiosk.Services;

namespace CocktailOS.Kiosk.Tests;

public sealed class NetworkAccessPinServiceTests
{
    [Fact]
    public void Hash_VerifiesOnlyTheOriginalFourDigitPin()
    {
        var service = new NetworkAccessPinService();
        var hash = service.Hash("1234");

        Assert.True(service.Verify("1234", hash));
        Assert.False(service.Verify("4321", hash));
        Assert.False(service.Verify("123", hash));
        Assert.DoesNotContain("1234", hash, StringComparison.Ordinal);
    }

    [Fact]
    public void IsHash_RecognizesOnlyValidStoredPinHashes()
    {
        var service = new NetworkAccessPinService();

        Assert.True(NetworkAccessPinService.IsHash(service.Hash("1234")));
        Assert.False(NetworkAccessPinService.IsHash("v1.120000.invalid.invalid"));
        Assert.False(NetworkAccessPinService.IsHash("not-a-hash"));
    }
}
