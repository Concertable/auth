using Concertable.Auth.Contracts;
using Concertable.Auth.Hosting;
using Concertable.Testing.Architecture;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Concertable.Auth.StartupTests;

public sealed class WebHostTests
{
    [Fact]
    public void E2EGraph_UsesHttpCompatibleCookies()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = CompositionTestArguments.Create(),
            EnvironmentName = "E2E"
        });
        builder.AddAuthHost();
        using var app = builder.Build();
        var options = app.Services.GetRequiredService<IOptions<IdentityServerOptions>>().Value;

        Assert.Equal(SameSiteMode.Lax, options.Authentication.CookieSameSiteMode);
        Assert.Equal(SameSiteMode.Lax, options.Authentication.CheckSessionCookieSameSiteMode);
    }

    [Fact]
    public void ProductionGraphAndStrictValidation_AreValid()
    {
        var builder = WebApplication.CreateBuilder(CompositionTestArguments.Create());
        builder.AddAuthHost();
        using var app = builder.Build();
        builder.Services.ValidateComposition(app.Services, new CompositionValidationOptions
        {
            RootAssemblies = [typeof(AuthHostExtensions).Assembly]
        });
        var invalidBuilder = WebApplication.CreateBuilder(CompositionTestArguments.Create());
        invalidBuilder.AddAuthHost();
        invalidBuilder.Services.AddInvalidLifetimeGraph();
        Assert.ThrowsAny<Exception>(() => invalidBuilder.Build());
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("Customer", "customer-web")]
    [InlineData("Venue,Artist,Business,Admin", "venue-web,artist-web,business-web,admin")]
    [InlineData("Customer,Venue,Artist,Business,Admin", "customer-web,venue-web,artist-web,business-web,admin")]
    public async Task EnabledSpaClients_FilterBundledDefaults(string? enabledNames, string? expectedClientIds)
    {
        var builder = WebApplication.CreateBuilder(CompositionTestArguments.Create());
        var enabled = enabledNames?.Split(',') ?? [];
        var configuration = enabled
            .Select((name, index) => new KeyValuePair<string, string?>(
                $"Auth:SpaClients:EnabledClients:{index}", name))
            .Append(new("Auth:SpaClients:RestrictToEnabledClients", "true"));
        builder.Configuration.AddInMemoryCollection(configuration);
        builder.AddAuthHost();
        using var app = builder.Build();
        var clientStore = app.Services.GetRequiredService<IClientStore>();
        var expected = expectedClientIds?.Split(',').ToHashSet(StringComparer.Ordinal)
            ?? [];

        InteractiveClient[] browserClients =
        [
            InteractiveClient.CustomerBrowser,
            InteractiveClient.VenueBrowser,
            InteractiveClient.ArtistBrowser,
            InteractiveClient.BusinessBrowser,
            InteractiveClient.Admin,
        ];
        foreach (var browserClient in browserClients)
        {
            var clientId = InteractiveClientInfo.Get(browserClient).Id;

            Assert.Equal(expected.Contains(clientId), await clientStore.FindClientByIdAsync(clientId) is not null);
        }
    }

    [Fact]
    public void UnknownEnabledSpaClient_Throws()
    {
        var builder = WebApplication.CreateBuilder(CompositionTestArguments.Create());
        builder.Configuration.AddInMemoryCollection([
            new("Auth:SpaClients:RestrictToEnabledClients", "true"),
            new("Auth:SpaClients:EnabledClients:0", "Customer"),
            new("Auth:SpaClients:EnabledClients:1", "Unknown")
        ]);

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddAuthHost());

        Assert.Contains("Unknown", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AbsentSpaClientRestriction_PreservesBundledDefaults()
    {
        var builder = WebApplication.CreateBuilder(CompositionTestArguments.Create());
        builder.AddAuthHost();
        using var app = builder.Build();
        var clientStore = app.Services.GetRequiredService<IClientStore>();

        InteractiveClient[] browserClients =
        [
            InteractiveClient.CustomerBrowser,
            InteractiveClient.VenueBrowser,
            InteractiveClient.ArtistBrowser,
            InteractiveClient.BusinessBrowser,
            InteractiveClient.Admin,
        ];
        foreach (var browserClient in browserClients)
            Assert.NotNull(await clientStore.FindClientByIdAsync(InteractiveClientInfo.Get(browserClient).Id));
    }

    [Fact]
    public async Task BusinessClients_AreRegisteredForB2BInteractiveFlows()
    {
        var builder = WebApplication.CreateBuilder(CompositionTestArguments.Create());
        builder.AddAuthHost();
        using var app = builder.Build();
        var clientStore = app.Services.GetRequiredService<IClientStore>();

        var browser = await clientStore.FindClientByIdAsync(
            InteractiveClientInfo.Get(InteractiveClient.BusinessBrowser).Id);
        var mobile = await clientStore.FindClientByIdAsync(
            InteractiveClientInfo.Get(InteractiveClient.BusinessMobile).Id);

        Assert.NotNull(browser);
        Assert.Equal(["https://business.concertable.co.uk/auth/callback"], browser.RedirectUris);
        Assert.Equal(["https://business.concertable.co.uk"], browser.PostLogoutRedirectUris);
        Assert.Equal(["https://business.concertable.co.uk"], browser.AllowedCorsOrigins);
        Assert.Equal(
            new HashSet<string> { "openid", "profile", AuthScope.B2BApi.Id },
            browser.AllowedScopes.ToHashSet(StringComparer.Ordinal));
        Assert.NotNull(mobile);
        Assert.Equal(["concertable-business://"], mobile.RedirectUris);
        Assert.Equal(["concertable-business://"], mobile.PostLogoutRedirectUris);
        Assert.Equal(
            new HashSet<string> { "openid", "profile", AuthScope.B2BApi.Id },
            mobile.AllowedScopes.ToHashSet(StringComparer.Ordinal));
    }
}
