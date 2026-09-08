using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Concertable.Auth.Contracts;
using Concertable.Auth.Hosting;
using Concertable.Testing.Architecture;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Concertable.Auth.StartupTests;

public sealed class ResourceGraphTests
{
    [Fact]
    public void ProductionGraphAndStrictValidation_AreValid()
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
}
