using Concertable.Auth.OperationalStoreMigration;

namespace Concertable.Auth.IntegrationTests;

[Collection(OperationalStoreMigrationCollection.Name)]
public sealed class OperationalStoreMigrationTests : IAsyncLifetime
{
    private readonly OperationalStoreMigrationFixture fixture;
    private readonly OperationalStoreMigrator migrator = new();

    public OperationalStoreMigrationTests(OperationalStoreMigrationFixture fixture)
    {
        this.fixture = fixture;
    }

    public async Task InitializeAsync() => await fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Inspect_DoesNotChangeTheTarget()
    {
        await fixture.SeedEveryTableAsync(fixture.SourceConnectionString);

        var report = await migrator.InspectAsync(
            fixture.SourceConnectionString,
            fixture.TargetConnectionString);

        Assert.False(report.Executed);
        Assert.True(report.TargetIsEmpty);
        Assert.All(report.Tables, table =>
        {
            Assert.Equal(1, table.SourceRows);
            Assert.Equal(0, table.TargetRows);
        });
    }

    [Fact]
    public async Task Copy_CopiesEveryTableAndPreservesIdentityValues()
    {
        await fixture.SeedEveryTableAsync(fixture.SourceConnectionString);

        var report = await migrator.CopyAsync(
            fixture.SourceConnectionString,
            fixture.TargetConnectionString);

        Assert.True(report.Executed);
        Assert.All(report.Tables, table =>
        {
            Assert.Equal(1, table.SourceRows);
            Assert.Equal(table.SourceRows, table.TargetRows);
            Assert.Equal(table.SourceSha256, table.TargetSha256);
        });
        Assert.Equal(41, await fixture.ReadIdentityAsync(fixture.TargetConnectionString, "PersistedGrants"));
        Assert.Equal(42, await fixture.ReadIdentityAsync(fixture.TargetConnectionString, "PushedAuthorizationRequests"));
        Assert.Equal(43, await fixture.ReadIdentityAsync(fixture.TargetConnectionString, "ServerSideSessions"));
        Assert.Equal(
            await fixture.ReadIdentityCurrentAsync(fixture.SourceConnectionString, "PersistedGrants"),
            await fixture.ReadIdentityCurrentAsync(fixture.TargetConnectionString, "PersistedGrants"));
        Assert.Equal(
            await fixture.ReadIdentityCurrentAsync(fixture.SourceConnectionString, "PushedAuthorizationRequests"),
            await fixture.ReadIdentityCurrentAsync(fixture.TargetConnectionString, "PushedAuthorizationRequests"));
        Assert.Equal(
            await fixture.ReadIdentityCurrentAsync(fixture.SourceConnectionString, "ServerSideSessions"),
            await fixture.ReadIdentityCurrentAsync(fixture.TargetConnectionString, "ServerSideSessions"));
    }

    [Fact]
    public async Task Copy_WhenTheTargetContainsData_RefusesToOverwriteIt()
    {
        await fixture.SeedEveryTableAsync(fixture.SourceConnectionString);
        await fixture.SeedEveryTableAsync(fixture.TargetConnectionString);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => migrator.CopyAsync(
            fixture.SourceConnectionString,
            fixture.TargetConnectionString));

        Assert.Contains("refusing to overwrite", exception.Message);
        var report = await migrator.InspectAsync(
            fixture.SourceConnectionString,
            fixture.TargetConnectionString);
        Assert.All(report.Tables, table =>
        {
            Assert.Equal(1, table.SourceRows);
            Assert.Equal(1, table.TargetRows);
        });
    }

    [Fact]
    public async Task Inspect_WhenTargetIndexDiffers_RejectsTheSchema()
    {
        await fixture.ExecuteAsync(
            fixture.TargetConnectionString,
            "DROP INDEX [IX_PersistedGrants_Key] ON [idsrv].[PersistedGrants];");
        try
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => migrator.InspectAsync(
                fixture.SourceConnectionString,
                fixture.TargetConnectionString));

            Assert.Contains("schemas differ", exception.Message);
        }
        finally
        {
            await fixture.ExecuteAsync(
                fixture.TargetConnectionString,
                "CREATE UNIQUE INDEX [IX_PersistedGrants_Key] ON [idsrv].[PersistedGrants] ([Key]) WHERE [Key] IS NOT NULL;");
        }
    }
}
