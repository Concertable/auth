using Concertable.Auth.Data;
using Concertable.Messaging.Infrastructure.Outbox;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Auth.Migrations;

internal static class AuthMigrationJob
{
    public static async Task RunAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var services = new ServiceCollection();
        services.AddOptions<OutboxOptions>();
        services.AddSingleton(new OperationalStoreOptions { DefaultSchema = Schema.Grants });
        services.AddSingleton<AuthConfigurationProvider>();
        services.AddDbContext<PersistedGrantDbContext>(opts => opts.UseNpgsqlForGrants(connectionString));
        services.AddDbContext<AuthDbContext>(opts => opts.UseNpgsqlForAuth(connectionString));
        services.AddDbContext<OutboxDbContext>(opts => opts.UseNpgsqlForOutbox(connectionString));

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        DbContext[] contexts =
        [
            scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>(),
            scope.ServiceProvider.GetRequiredService<AuthDbContext>(),
            scope.ServiceProvider.GetRequiredService<OutboxDbContext>(),
        ];

        foreach (var context in contexts)
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
                throw new InvalidOperationException($"Migrations remain pending for {context.GetType().Name}.");
        }
    }
}
