using System.Collections.Frozen;

namespace Concertable.Auth.Contracts;

/// <summary>
/// The interactive OAuth client roster — the single source for wire client ids. Auth registers these with
/// the identity server; downstream services classify <see cref="Events.CredentialRegisteredEvent.ClientId"/>
/// through <see cref="Find"/>. Identity-only — never grows a business-party or role field; a consuming
/// service that needs to know what a client id means to it owns that classification locally.
/// </summary>
public static class InteractiveClients
{
    private static readonly FrozenDictionary<InteractiveClient, InteractiveClientInfo> ByClient = new[]
    {
        new InteractiveClientInfo(InteractiveClient.CustomerBrowser, "customer-web",     null),
        new InteractiveClientInfo(InteractiveClient.CustomerMobile,  "customer-mobile",  "concertable-customer://"),
        new InteractiveClientInfo(InteractiveClient.VenueBrowser,    "venue-web",        null),
        new InteractiveClientInfo(InteractiveClient.VenueMobile,     "venue-mobile",     "concertable-business://"),
        new InteractiveClientInfo(InteractiveClient.ArtistBrowser,   "artist-web",       null),
        new InteractiveClientInfo(InteractiveClient.ArtistMobile,    "artist-mobile",    "concertable-business://"),
        new InteractiveClientInfo(InteractiveClient.Admin,           "admin",            null),
        new InteractiveClientInfo(InteractiveClient.E2ETest,         "concertable-test", null),
    }.ToFrozenDictionary(info => info.Client);

    private static readonly FrozenDictionary<string, InteractiveClientInfo> ById =
        ByClient.Values.ToFrozenDictionary(info => info.Id, StringComparer.Ordinal);

    /// <summary>Every registered interactive client.</summary>
    public static IReadOnlyCollection<InteractiveClientInfo> All => ByClient.Values;

    /// <summary>The catalog row for a known client.</summary>
    public static InteractiveClientInfo Info(this InteractiveClient client) => ByClient[client];

    /// <summary>
    /// Resolves a wire client id (e.g. <see cref="Events.CredentialRegisteredEvent.ClientId"/>), or
    /// <see langword="null"/> for an absent or unrecognised id — so a consumer skips an event from a
    /// client it predates, or a malformed one, rather than throwing.
    /// </summary>
    public static InteractiveClientInfo? Find(string? clientId) =>
        clientId is not null && ById.TryGetValue(clientId, out var info) ? info : null;
}
