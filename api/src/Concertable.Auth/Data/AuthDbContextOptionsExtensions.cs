using Microsoft.EntityFrameworkCore;

namespace Concertable.Auth.Data;

internal static class AuthDbContextOptionsExtensions
{
    private const string History = "__EFMigrationsHistory";
    private const string OutboxHistory = "__EFMigrationsHistory_Outbox";

    private static string MigrationsAssembly => typeof(AuthDbContext).Assembly.GetName().Name!;

    extension(DbContextOptionsBuilder options)
    {
        public DbContextOptionsBuilder UseNpgsqlForAuth(string? connectionString) =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(History, Schema.Name));

        public DbContextOptionsBuilder UseNpgsqlForGrants(string? connectionString) =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql
                    .MigrationsAssembly(MigrationsAssembly)
                    .MigrationsHistoryTable(History, Schema.Grants));

        public DbContextOptionsBuilder UseNpgsqlForOutbox(string? connectionString) =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(OutboxHistory, Schema.Messaging));
    }
}
