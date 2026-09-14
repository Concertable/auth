using Concertable.Auth.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Concertable.Auth.StartupTests;

public sealed class AppModelStartupContractTests
{
    [Fact]
    public async Task WebHost_StartsOnTheConfigurationTheAppModelSupplies()
    {
        var appModel = AppHost.CreateBuilder([]);
        var arguments = await AppModelConfiguration.ArgumentsForAsync(
            appModel, AuthConstants.Resource, AppModelConfiguration.Secrets);

        var builder = WebApplication.CreateBuilder(arguments);
        builder.AddAuthHost();
        using var app = builder.Build();

        app.Services.GetService<IStartupValidator>()?.Validate();
    }
}
