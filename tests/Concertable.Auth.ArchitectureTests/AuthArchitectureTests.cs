using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Concertable.Auth;
using Concertable.Auth.Hosting;
using Concertable.Testing.Architecture;
using Microsoft.AspNetCore.Builder;
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
                          .WithSpaClients([new SpaSurface("customer", 5174, "Customer")]);
        var configuration = await ExecutionConfigurationBuilder.Create(auth.Resource)
            .WithEnvironmentVariablesConfig()
            .BuildAsync(new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish),
                NullLogger.Instance, CancellationToken.None);
        var environment = configuration.EnvironmentVariables.ToDictionary();
        var spaClients = environment
            .Where(pair => pair.Key.StartsWith("Auth__SpaClients__", StringComparison.Ordinal))
            .ToDictionary();

        Assert.Equal(3, spaClients.Count);
        Assert.Equal("https://localhost:5174/auth/callback", spaClients["Auth__SpaClients__Customer__RedirectUri"]);
        Assert.Equal("https://localhost:5174", spaClients["Auth__SpaClients__Customer__PostLogoutRedirectUri"]);
        Assert.Equal("https://localhost:5174", spaClients["Auth__SpaClients__Customer__AllowedCorsOrigins__0"]);
    }
}
