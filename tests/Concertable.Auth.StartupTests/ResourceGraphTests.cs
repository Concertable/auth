using Concertable.Testing.Architecture;
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
}
