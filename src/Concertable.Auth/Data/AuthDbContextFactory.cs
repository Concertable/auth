using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Auth.Data;

internal sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var services = new ServiceCollection();
        services.AddSingleton<AuthConfigurationProvider>();
        services.AddDbContext<AuthDbContext>(opts =>
            opts.UseSqlServer(DesignTimeConfiguration.ConnectionString()));
        return services.BuildServiceProvider().GetRequiredService<AuthDbContext>();
    }
}
