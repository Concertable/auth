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
        services.AddSingleton(new OperationalStoreOptions { DefaultSchema = "idsrv" });
        services.AddDbContext<PersistedGrantDbContext>(opts =>
            opts.UseSqlServer(
                DesignTimeConfiguration.ConnectionString(),
                sql => sql.MigrationsAssembly(typeof(Program).Assembly.GetName().Name)));
        return services.BuildServiceProvider().GetRequiredService<PersistedGrantDbContext>();
    }
}
