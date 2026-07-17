namespace CocktailOS.Kiosk.Tests;

public sealed class StaticAssetCachingTests
{
    [Fact]
    public async Task ApplicationShell_UsesMatchingVersionedAssets_AndDisablesCaching()
    {
        using var factory = new KioskApplicationFactory();
        using var client = factory.CreateClient();

        using var indexResponse = await client.GetAsync("/");
        indexResponse.EnsureSuccessStatusCode();
        var html = await indexResponse.Content.ReadAsStringAsync();

        Assert.Contains("/app.css?v=20260717-ui18", html);
        Assert.Contains("/app.js?v=20260717-ui18", html);
        AssertNoStore(indexResponse);

        using var cssResponse = await client.GetAsync("/app.css?v=20260717-ui18");
        cssResponse.EnsureSuccessStatusCode();
        AssertNoStore(cssResponse);

        using var scriptResponse = await client.GetAsync("/app.js?v=20260717-ui18");
        scriptResponse.EnsureSuccessStatusCode();
        AssertNoStore(scriptResponse);
    }

    private static void AssertNoStore(HttpResponseMessage response)
    {
        Assert.True(response.Headers.CacheControl?.NoCache);
        Assert.True(response.Headers.CacheControl?.NoStore);
    }
}
