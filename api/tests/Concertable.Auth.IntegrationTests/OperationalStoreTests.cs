using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Xunit.Abstractions;

namespace Concertable.Auth.IntegrationTests;

[Collection("Integration")]
public sealed class OperationalStoreTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public OperationalStoreTests(ApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync()
    {
        fixture.DetachOutput();
        return Task.CompletedTask;
    }

    #region Persisted grants

    [Fact]
    public async Task StoreAsync_RoundTripsTheGrantAndItsUtcTimestamps()
    {
        var creation = new DateTime(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc);
        var grant = CreateGrant("round-trip-key", creation, creation.AddHours(1));

        await fixture.InvokeAsync<IPersistedGrantStore, bool>(async store =>
        {
            await store.StoreAsync(grant);
            return true;
        });
        var stored = await fixture.InvokeAsync<IPersistedGrantStore, PersistedGrant?>(
            store => store.GetAsync(grant.Key));

        Assert.NotNull(stored);
        Assert.Equal(grant.Type, stored.Type);
        Assert.Equal(grant.SubjectId, stored.SubjectId);
        Assert.Equal(grant.Data, stored.Data);
        Assert.Equal(creation, stored.CreationTime);
        Assert.Equal(creation.AddHours(1), stored.Expiration);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyTheSubjectsGrants()
    {
        var creation = DateTime.UtcNow;
        await fixture.InvokeAsync<IPersistedGrantStore, bool>(async store =>
        {
            await store.StoreAsync(CreateGrant("subject-a-key", creation, creation.AddHours(1)));
            await store.StoreAsync(CreateGrant("subject-b-key", creation, creation.AddHours(1), subjectId: "subject-b"));
            return true;
        });

        var grants = await fixture.InvokeAsync<IPersistedGrantStore, IEnumerable<PersistedGrant>>(
            store => store.GetAllAsync(new PersistedGrantFilter { SubjectId = "subject-a" }));

        Assert.Equal(["subject-a-key"], grants.Select(grant => grant.Key));
    }

    [Fact]
    public async Task RemoveAllAsync_ClearsTheExpiredGrantsOfOneClient()
    {
        var expired = DateTime.UtcNow.AddHours(-2);
        await fixture.InvokeAsync<IPersistedGrantStore, bool>(async store =>
        {
            await store.StoreAsync(CreateGrant("expired-key", expired, expired.AddHours(1)));
            return true;
        });

        await fixture.InvokeAsync<IPersistedGrantStore, bool>(async store =>
        {
            await store.RemoveAllAsync(new PersistedGrantFilter
            {
                SubjectId = "subject-a",
                ClientId = "concertable-customer-web"
            });
            return true;
        });

        var remaining = await fixture.InvokeAsync<IPersistedGrantStore, PersistedGrant?>(
            store => store.GetAsync("expired-key"));

        Assert.Null(remaining);
    }

    #endregion

    #region Signing keys

    [Fact]
    public async Task StoreKeyAsync_RoundTripsTheSigningKey()
    {
        var created = new DateTime(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc);
        var key = new SerializedKey
        {
            Id = "signing-key-1",
            Version = 1,
            Created = created,
            Algorithm = "RS256",
            IsX509Certificate = false,
            DataProtected = false,
            Data = "serialized-key-material"
        };

        await fixture.InvokeAsync<ISigningKeyStore, bool>(async store =>
        {
            await store.StoreKeyAsync(key);
            return true;
        });
        var keys = await fixture.InvokeAsync<ISigningKeyStore, IEnumerable<SerializedKey>>(
            store => store.LoadKeysAsync());

        var stored = Assert.Single(keys, candidate => candidate.Id == key.Id);
        Assert.Equal(key.Algorithm, stored.Algorithm);
        Assert.Equal(key.Data, stored.Data);
        Assert.Equal(created, stored.Created);
    }

    #endregion

    private static PersistedGrant CreateGrant(
        string key,
        DateTime creation,
        DateTime expiration,
        string subjectId = "subject-a") =>
        new()
        {
            Key = key,
            Type = "refresh_token",
            SubjectId = subjectId,
            SessionId = "session-a",
            ClientId = "concertable-customer-web",
            Description = "integration grant",
            CreationTime = creation,
            Expiration = expiration,
            Data = "protected-grant-data"
        };
}
