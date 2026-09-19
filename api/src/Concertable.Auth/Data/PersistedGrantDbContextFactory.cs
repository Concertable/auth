using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Auth.Data;

public sealed class PersistedGrantDbContextFactory : IDesignTimeDbContextFactory<PersistedGrantDbContext>
{
    public PersistedGrantDbContext CreateDbContext(string[] args)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new OperationalStoreOptions { DefaultSchema = Schema.Grants });
        services.AddDbContext<PersistedGrantDbContext>(opts =>
            opts.UseNpgsqlForGrants(DesignTimeConfiguration.ConnectionString()));
        return services.BuildServiceProvider().GetRequiredService<PersistedGrantDbContext>();
    }
}
