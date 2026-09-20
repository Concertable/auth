extern alias AuthMigrations;

using Concertable.Testing.Integration;
using Npgsql;
using AuthMigrationJob = AuthMigrations::Concertable.Auth.Migrations.AuthMigrationJob;

namespace Concertable.Auth.IntegrationTests;

public sealed class MigrationJobTests
{
    [Fact]
    public async Task RunAsync_CanMigrateCleanDatabaseTwice()
    {
        var postgres = new PostgresFixture();
        await postgres.InitializeAsync();
        try
        {
            await AuthMigrationJob.RunAsync(postgres.ConnectionString);
            await AuthMigrationJob.RunAsync(postgres.ConnectionString);

            await using var connection = new NpgsqlConnection(postgres.ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT table_schema || '.' || table_name
                FROM information_schema.tables
                WHERE table_name LIKE '__EFMigrationsHistory%'
                ORDER BY table_schema, table_name
                """;
            await using var reader = await command.ExecuteReaderAsync();
            var histories = new List<string>();
            while (await reader.ReadAsync())
                histories.Add(reader.GetString(0));

            Assert.Equal(
            [
                "auth.__EFMigrationsHistory",
                "idsrv.__EFMigrationsHistory",
                "messaging.__EFMigrationsHistory_Outbox",
            ],
            histories);
        }
        finally
        {
            await postgres.DisposeAsync();
        }
    }
}
