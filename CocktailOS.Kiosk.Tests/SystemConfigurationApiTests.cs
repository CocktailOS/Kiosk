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
    public async Task NetworkRequests_AreBlockedUntilNetworkAccessIsEnabled()
    {
        using var factory = new KioskApplicationFactory();
        using var client = factory.CreateClient();

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
            new MachineConfigurationRequest(configuration.PumpDriver, configuration.PinNumberingScheme, configuration.Theme, true, "1234"));
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

        var initial = await client.GetFromJsonAsync<MachineConfigurationResponse>("/api/system");
        Assert.NotNull(initial);
        Assert.False(initial.NetworkAccessEnabled);

        var enabledResponse = await client.PutAsJsonAsync(
            "/api/system",
            new MachineConfigurationRequest(initial.PumpDriver, initial.PinNumberingScheme, initial.Theme, true, "1234"));
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
    public async Task NetworkAccess_CannotBeEnabledWithoutPin()
    {
        using var factory = new KioskApplicationFactory();
        using var client = factory.CreateClient();
        var configuration = (await client.GetFromJsonAsync<MachineConfigurationResponse>("/api/system"))!;

        var response = await client.PutAsJsonAsync(
            "/api/system",
            new MachineConfigurationRequest(configuration.PumpDriver, configuration.PinNumberingScheme, configuration.Theme, true, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Netzwerk-PIN", await response.Content.ReadAsStringAsync());
    }
}
