using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Auth.Data;

internal sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var services = new ServiceCollection();
        services.AddOptions<OutboxOptions>();
        services.AddSingleton<AuthConfigurationProvider>();
        services.AddDbContext<AuthDbContext>(opts =>
            opts.UseNpgsql(
                DesignTimeConfiguration.ConnectionString(),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", Schema.Name)));
        return services.BuildServiceProvider().GetRequiredService<AuthDbContext>();
    }
}
