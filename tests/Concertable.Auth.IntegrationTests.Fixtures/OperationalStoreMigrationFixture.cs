using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Options;
using Concertable.Testing.Integration;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Concertable.Auth.IntegrationTests.Fixtures;

public sealed class OperationalStoreMigrationFixture : IAsyncLifetime
{
    private readonly SqlFixture sqlFixture = new();

    public string SourceConnectionString { get; private set; } = null!;
    public string TargetConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await sqlFixture.InitializeAsync();
        SourceConnectionString = await CreateDatabaseAsync("ConcertableAuthOperationalSource");
        TargetConnectionString = await CreateDatabaseAsync("ConcertableAuthOperationalTarget");
        await MigrateAsync(SourceConnectionString);
        await MigrateAsync(TargetConnectionString);
    }

    public async Task DisposeAsync() => await sqlFixture.DisposeAsync();

    public async Task ResetAsync()
    {
        await ClearAsync(SourceConnectionString);
        await ClearAsync(TargetConnectionString);
    }

    public async Task SeedEveryTableAsync(string connectionString)
    {
        const string sql = """
            SET QUOTED_IDENTIFIER ON;

            INSERT INTO [idsrv].[DeviceCodes]
                ([UserCode], [DeviceCode], [SubjectId], [SessionId], [ClientId], [Description], [CreationTime], [Expiration], [Data])
            VALUES
                (N'user-code', N'device-code', N'subject', N'session', N'client', N'device',
                 '2026-08-30T12:00:00', '2026-08-30T12:10:00', N'protected-device-data');

            INSERT INTO [idsrv].[Keys]
                ([Id], [Version], [Created], [Use], [Algorithm], [IsX509Certificate], [DataProtected], [Data])
            VALUES
                (N'key-1', 1, '2026-08-30T12:00:00', N'signing', N'RS256', 0, 1, N'protected-key-data');

            SET IDENTITY_INSERT [idsrv].[PersistedGrants] ON;
            INSERT INTO [idsrv].[PersistedGrants]
                ([Id], [Key], [Type], [SubjectId], [SessionId], [ClientId], [Description],
                 [CreationTime], [Expiration], [ConsumedTime], [Data])
            VALUES
                (41, N'grant-key', N'refresh_token', N'subject', N'session', N'client', N'refresh token',
                 '2026-08-30T12:00:00', '2026-09-29T12:00:00', NULL, N'protected-grant-data');
            SET IDENTITY_INSERT [idsrv].[PersistedGrants] OFF;

            SET IDENTITY_INSERT [idsrv].[PushedAuthorizationRequests] ON;
            INSERT INTO [idsrv].[PushedAuthorizationRequests]
                ([Id], [ReferenceValueHash], [ExpiresAtUtc], [Parameters])
            VALUES
                (42, REPLICATE(N'A', 64), '2026-08-30T12:10:00', N'protected-par-data');
            SET IDENTITY_INSERT [idsrv].[PushedAuthorizationRequests] OFF;

            SET IDENTITY_INSERT [idsrv].[ServerSideSessions] ON;
            INSERT INTO [idsrv].[ServerSideSessions]
                ([Id], [Key], [Scheme], [SubjectId], [SessionId], [DisplayName], [Created], [Renewed], [Expires], [Data])
            VALUES
                (43, N'session-key', N'idsrv', N'subject', N'session', N'Test Session',
                 '2026-08-30T12:00:00', '2026-08-30T12:01:00', '2026-09-29T12:00:00', N'protected-session-data');
            SET IDENTITY_INSERT [idsrv].[ServerSideSessions] OFF;
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<long> ReadIdentityAsync(string connectionString, string tableName)
    {
        if (tableName is not ("PersistedGrants" or "PushedAuthorizationRequests" or "ServerSideSessions"))
            throw new ArgumentOutOfRangeException(nameof(tableName));

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand($"SELECT [Id] FROM [idsrv].[{tableName}];", connection);
        return (long)(await command.ExecuteScalarAsync() ?? throw new InvalidOperationException("Identity row was not found."));
    }

    public async Task<decimal> ReadIdentityCurrentAsync(string connectionString, string tableName)
    {
        if (tableName is not ("PersistedGrants" or "PushedAuthorizationRequests" or "ServerSideSessions"))
            throw new ArgumentOutOfRangeException(nameof(tableName));

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand($"SELECT IDENT_CURRENT(N'idsrv.{tableName}');", connection);
        return (decimal)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Identity state was not found."));
    }

    public async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<string> CreateDatabaseAsync(string databaseName)
    {
        var connectionString = new SqlConnectionStringBuilder(sqlFixture.ConnectionString)
        {
            InitialCatalog = databaseName
        }.ConnectionString;

        await using var connection = new SqlConnection(sqlFixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand($"CREATE DATABASE [{databaseName}];", connection);
        await command.ExecuteNonQueryAsync();
        return connectionString;
    }

    private static async Task MigrateAsync(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new OperationalStoreOptions { DefaultSchema = "idsrv" });
        services.AddDbContext<PersistedGrantDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly(typeof(AuthHostExtensions).Assembly.GetName().Name)));
        await using var provider = services.BuildServiceProvider();
        await provider.GetRequiredService<PersistedGrantDbContext>().Database.MigrateAsync();
    }

    private static async Task ClearAsync(string connectionString)
    {
        const string sql = """
            DELETE FROM [idsrv].[DeviceCodes];
            DELETE FROM [idsrv].[Keys];
            DELETE FROM [idsrv].[PersistedGrants];
            DELETE FROM [idsrv].[PushedAuthorizationRequests];
            DELETE FROM [idsrv].[ServerSideSessions];
            DBCC CHECKIDENT ('[idsrv].[PersistedGrants]', RESEED, 0);
            DBCC CHECKIDENT ('[idsrv].[PushedAuthorizationRequests]', RESEED, 0);
            DBCC CHECKIDENT ('[idsrv].[ServerSideSessions]', RESEED, 0);
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
