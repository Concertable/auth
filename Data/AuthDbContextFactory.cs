using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Auth.Data;

internal class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var services = new ServiceCollection();
        services.AddSingleton<AuthConfigurationProvider>();
        services.AddDbContext<AuthDbContext>(opts =>
            opts.UseSqlServer("Server=localhost,1433;Database=concertable-b2b;User Id=sa;Password=Password11!;TrustServerCertificate=True"));
        return services.BuildServiceProvider().GetRequiredService<AuthDbContext>();
    }
}
