using System.Net;
using System.Net.Http.Json;
using CocktailOS.Kiosk.Contracts;
using CocktailOS.Kiosk.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CocktailOS.Kiosk.Tests;

public sealed class SystemConfigurationApiTests
{
    [Fact]
    public async Task IntroTour_IsCompletedOnlyAfterTheExplicitCompletionRequest()
    {
        using var factory = new KioskApplicationFactory();
        using var client = factory.CreateClient();

        var initial = await client.GetFromJsonAsync<IntroTourStatusResponse>("/api/intro-tour/status");
        Assert.False(initial?.Completed);

        var completion = await client.PostAsync("/api/intro-tour/complete", null);
        completion.EnsureSuccessStatusCode();

        var completed = await client.GetFromJsonAsync<IntroTourStatusResponse>("/api/intro-tour/status");
        Assert.True(completed?.Completed);
    }

    [Fact]
    public async Task NetworkRequests_AreBlockedUntilNetworkAccessIsEnabled()
    {
        using var factory = new KioskApplicationFactory();
        using var client = factory.CreateClient();
        await SetupAdminPinAsync(client);

        var denied = await factory.Server.SendAsync(context =>
        {
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = "/api/system";
            context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.20");
        });
        Assert.Equal(StatusCodes.Status403Forbidden, denied.Response.StatusCode);

        var configuration = (await client.GetFromJsonAsync<MachineConfigurationResponse>("/api/system"))!;
        var update = await client.PutAsJsonAsync(
            "/api/system",
            new MachineConfigurationRequest(configuration.PumpDriver, configuration.PinNumberingScheme, configuration.Theme, true, null));
        update.EnsureSuccessStatusCode();

        var tokens = await factory.Server.SendAsync(context =>
        {
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = "/tokens.css";
            context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.20");
        });
        Assert.Equal(StatusCodes.Status200OK, tokens.Response.StatusCode);

        var allowed = await factory.Server.SendAsync(context =>
        {
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = "/api/system";
            context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.20");
        });
        Assert.Equal(StatusCodes.Status401Unauthorized, allowed.Response.StatusCode);
    }

    [Fact]
    public async Task NetworkAccess_CanBeEnabledAndDisabled()
    {
        using var factory = new KioskApplicationFactory();
        using var client = factory.CreateClient();
        await SetupAdminPinAsync(client);

        var initial = await client.GetFromJsonAsync<MachineConfigurationResponse>("/api/system");
        Assert.NotNull(initial);
        Assert.False(initial.NetworkAccessEnabled);

        var enabledResponse = await client.PutAsJsonAsync(
            "/api/system",
            new MachineConfigurationRequest(initial.PumpDriver, initial.PinNumberingScheme, initial.Theme, true, null));
        enabledResponse.EnsureSuccessStatusCode();
        var enabled = await enabledResponse.Content.ReadFromJsonAsync<MachineConfigurationResponse>();

        Assert.True(enabled?.NetworkAccessEnabled);
        Assert.True(enabled?.NetworkAccessPinConfigured);
        Assert.True(factory.Services.GetRequiredService<NetworkAccessPolicy>().IsEnabled);

        var disabledResponse = await client.PutAsJsonAsync(
            "/api/system",
            new MachineConfigurationRequest(initial.PumpDriver, initial.PinNumberingScheme, initial.Theme, false, null));
        disabledResponse.EnsureSuccessStatusCode();

        Assert.False(factory.Services.GetRequiredService<NetworkAccessPolicy>().IsEnabled);
    }

    [Fact]
    public async Task NetworkAccess_CannotBeEnabledBeforeAppPinIsConfigured()
    {
        using var factory = new KioskApplicationFactory();
        using var client = factory.CreateClient();
        var configuration = (await client.GetFromJsonAsync<MachineConfigurationResponse>("/api/system"))!;

        var response = await client.PutAsJsonAsync(
            "/api/system",
            new MachineConfigurationRequest(configuration.PumpDriver, configuration.PinNumberingScheme, configuration.Theme, true, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("App-PIN", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AdminPin_SetupAuthenticatesTheKioskAndProtectsAdministrationEndpoints()
    {
        using var factory = new KioskApplicationFactory();
        using var client = factory.CreateClient();

        var status = await client.GetFromJsonAsync<AdminAccessStatusResponse>("/api/admin-access/status");
        Assert.False(status?.PinConfigured);

        await SetupAdminPinAsync(client);

        var configured = await client.GetFromJsonAsync<AdminAccessStatusResponse>("/api/admin-access/status");
        Assert.True(configured?.PinConfigured);
        Assert.True(configured?.IsAuthenticated);

        var logout = await client.PostAsync("/api/admin-access/logout", null);
        logout.EnsureSuccessStatusCode();
        var locked = await client.GetFromJsonAsync<AdminAccessStatusResponse>("/api/admin-access/status");
        Assert.False(locked?.IsAuthenticated);

        var denied = await factory.Server.SendAsync(context =>
        {
            context.Request.Method = HttpMethods.Post;
            context.Request.Path = "/api/cocktails";
        });
        Assert.Equal(StatusCodes.Status401Unauthorized, denied.Response.StatusCode);
    }

    private static async Task SetupAdminPinAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/admin-access/setup", new NetworkPinRequest("1234"));
        response.EnsureSuccessStatusCode();
    }
}
