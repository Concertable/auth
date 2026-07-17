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
            opts.UseSqlServer(DesignTimeConnectionString.B2B()));
        return services.BuildServiceProvider().GetRequiredService<AuthDbContext>();
    }
}
