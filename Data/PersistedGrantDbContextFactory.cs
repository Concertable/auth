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
                "Server=localhost,1433;Database=concertable-b2b;User Id=sa;Password=Password11!;TrustServerCertificate=True",
                sql => sql.MigrationsAssembly(typeof(Program).Assembly.GetName().Name)));
        return services.BuildServiceProvider().GetRequiredService<PersistedGrantDbContext>();
    }
}
