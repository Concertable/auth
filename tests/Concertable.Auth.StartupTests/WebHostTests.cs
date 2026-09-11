using Concertable.Auth.Contracts;
using Concertable.Auth.Hosting;
using Concertable.Testing.Architecture;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Concertable.Auth.StartupTests;

public sealed class WebHostTests
{
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
    [InlineData("Venue,Artist,Admin", "venue-web,artist-web,admin")]
    [InlineData("Customer,Venue,Artist,Admin", "customer-web,venue-web,artist-web,admin")]
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

        foreach (var clientId in new[] { ClientIds.CustomerWeb, ClientIds.VenueWeb, ClientIds.ArtistWeb, ClientIds.Admin })
        {
            var client = await clientStore.FindClientByIdAsync(clientId);
            Assert.Equal(expected.Contains(clientId), client is not null);
        }
    }

    [Fact]
    public void UnknownEnabledSpaClient_Throws()
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
    public async Task AbsentSpaClientRestriction_PreservesBundledDefaults()
    {
        var builder = WebApplication.CreateBuilder(CompositionTestArguments.Create());
        builder.AddAuthHost();
        using var app = builder.Build();
        var clientStore = app.Services.GetRequiredService<IClientStore>();

        foreach (var clientId in new[] { ClientIds.CustomerWeb, ClientIds.VenueWeb, ClientIds.ArtistWeb, ClientIds.Admin })
            Assert.NotNull(await clientStore.FindClientByIdAsync(clientId));
    }
}
