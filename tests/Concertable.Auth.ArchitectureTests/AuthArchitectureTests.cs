using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Concertable.Auth;
using Concertable.Auth.Contracts;
using Concertable.Auth.Hosting;
using Concertable.Testing.Architecture;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Concertable.Auth.ArchitectureTests;

public sealed class AuthArchitectureTests
{
    [Fact]
    public void Web_ProductionGraphAndStrictValidation_AreValid()
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

    [Fact]
    public void AppHost_ProductionGraphAndStrictValidation_AreValid()
    {
        using var app = AppHost.CreateBuilder([]).Build();
        var builder = AppHost.CreateBuilder([]);
        builder.Services.AddInvalidLifetimeGraph();
        Assert.ThrowsAny<Exception>(() => builder.Build());
    }

    [Fact]
    public async Task WithSpaClients_ReplacesExistingRegistrations()
    {
        var builder = StrictDistributedApplication.CreateBuilder([]);
        var auth = builder.AddContainer("auth-spa-clients", "example.invalid/auth")
                          .WithEnvironment("Auth__SpaClients__Venue__RedirectUri", "https://stale.example/auth/callback")
                          .WithEnvironment("Auth__SpaClients__Artist__AllowedCorsOrigins__0", "https://stale.example")
                          .WithSpaClients([(new SpaSurface("customer", 5174), "Customer")]);
        var configuration = await ExecutionConfigurationBuilder.Create(auth.Resource)
            .WithEnvironmentVariablesConfig()
            .BuildAsync(new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish),
                NullLogger.Instance, CancellationToken.None);
        var environment = configuration.EnvironmentVariables.ToDictionary();
        var spaClients = environment
            .Where(pair => pair.Key.StartsWith("Auth__SpaClients__Customer__", StringComparison.Ordinal))
            .ToDictionary();

        Assert.Equal(3, spaClients.Count);
        Assert.Equal("true", environment["Auth__SpaClients__RestrictToEnabledClients"]);
        Assert.Equal("Customer", environment["Auth__SpaClients__EnabledClients__0"]);
        Assert.DoesNotContain("Auth__SpaClients__EnabledClients__1", environment.Keys);
        Assert.DoesNotContain("Auth__SpaClients__Venue__RedirectUri", environment.Keys);
        Assert.DoesNotContain("Auth__SpaClients__Artist__AllowedCorsOrigins__0", environment.Keys);
        Assert.Equal("https://localhost:5174/auth/callback", spaClients["Auth__SpaClients__Customer__RedirectUri"]);
        Assert.Equal("https://localhost:5174", spaClients["Auth__SpaClients__Customer__PostLogoutRedirectUri"]);
        Assert.Equal("https://localhost:5174", spaClients["Auth__SpaClients__Customer__AllowedCorsOrigins__0"]);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("Customer", "customer-web")]
    [InlineData("Venue,Artist,Admin", "venue-web,artist-web,admin")]
    [InlineData("Customer,Venue,Artist,Admin", "customer-web,venue-web,artist-web,admin")]
    public async Task Web_EnabledSpaClients_FilterBundledDefaults(string? enabledNames, string? expectedClientIds)
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

        foreach (var clientId in new[] { ClientIds.CustomerWeb, ClientIds.VenueWeb, ClientIds.ArtistWeb, ClientIds.Admin })
        {
            var client = await clientStore.FindClientByIdAsync(clientId);
            Assert.Equal(expected.Contains(clientId), client is not null);
        }
    }

    [Fact]
    public void Web_UnknownEnabledSpaClient_Throws()
    {
        var builder = WebApplication.CreateBuilder(CompositionTestArguments.Create());
        builder.Configuration.AddInMemoryCollection([
            new("Auth:SpaClients:RestrictToEnabledClients", "true"),
            new("Auth:SpaClients:EnabledClients:0", "Customer"),
            new("Auth:SpaClients:EnabledClients:1", "Business")
        ]);

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddAuthHost());

        Assert.Contains("Business", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Web_AbsentSpaClientRestriction_PreservesBundledDefaults()
    {
        var builder = WebApplication.CreateBuilder(CompositionTestArguments.Create());
        builder.AddAuthHost();
        using var app = builder.Build();
        var clientStore = app.Services.GetRequiredService<IClientStore>();

        foreach (var clientId in new[] { ClientIds.CustomerWeb, ClientIds.VenueWeb, ClientIds.ArtistWeb, ClientIds.Admin })
            Assert.NotNull(await clientStore.FindClientByIdAsync(clientId));
    }
}
