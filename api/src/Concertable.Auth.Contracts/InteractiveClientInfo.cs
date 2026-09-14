using System.Collections.Frozen;

namespace Concertable.Auth.Contracts;

/// <summary>
/// Catalog row for an <see cref="InteractiveClient"/>: its wire client id. Owns its own lookup — obtain a
/// row via <see cref="Get"/> / <see cref="GetOrDefault"/>, never construct one. Identity-only — which
/// business party a client belongs to is each consuming service's own classification, never Auth's; see
/// that service's own <c>AGENTS.md</c>. A <c>sealed record</c> (reference type), not a struct: its all-zero
/// default (<c>Client = CustomerBrowser</c> — enum value 0 — with <c>Id = null</c>) would look like real
/// data for the wrong client, so a struct's silent default is a real footgun here.
/// </summary>
public sealed record InteractiveClientInfo(InteractiveClient Client, string Id)
{
    private static readonly FrozenDictionary<InteractiveClient, InteractiveClientInfo> ByClient = new[]
    {
        new InteractiveClientInfo(InteractiveClient.CustomerBrowser, "customer-web"),
        new InteractiveClientInfo(InteractiveClient.CustomerMobile,  "customer-mobile"),
        new InteractiveClientInfo(InteractiveClient.VenueBrowser,    "venue-web"),
        new InteractiveClientInfo(InteractiveClient.VenueMobile,     "venue-mobile"),
        new InteractiveClientInfo(InteractiveClient.ArtistBrowser,   "artist-web"),
        new InteractiveClientInfo(InteractiveClient.ArtistMobile,    "artist-mobile"),
        new InteractiveClientInfo(InteractiveClient.Admin,           "admin"),
        new InteractiveClientInfo(InteractiveClient.E2ETest,         "concertable-test"),
    }.ToFrozenDictionary(info => info.Client);

    private static readonly FrozenDictionary<string, InteractiveClientInfo> ById =
        ByClient.Values.ToFrozenDictionary(info => info.Id, StringComparer.Ordinal);

    /// <summary>The catalog row for a known client.</summary>
    public static InteractiveClientInfo Get(InteractiveClient client) => ByClient[client];

    /// <summary>
    /// Resolves a wire client id (e.g. <see cref="Events.CredentialRegisteredEvent.ClientId"/>), or
    /// <see langword="null"/> for an absent or unrecognised id — so a consumer skips an event from a
    /// client it predates, or a malformed one, rather than throwing.
    /// </summary>
    public static InteractiveClientInfo? GetOrDefault(string? clientId) =>
        clientId is not null ? ById.GetValueOrDefault(clientId) : null;

    /// <summary>Every registered interactive client. Internal — nothing outside this package's own tests
    /// enumerates the full roster; every real consumer resolves one client at a time via
    /// <see cref="Get"/>/<see cref="GetOrDefault"/>.</summary>
    internal static IReadOnlyCollection<InteractiveClientInfo> All => ByClient.Values;
}
