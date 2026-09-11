namespace Concertable.Auth.Contracts;

/// <summary>
/// Catalog row for an <see cref="InteractiveClient"/>: its wire client id and, for a native app, its
/// redirect scheme. Obtain from <see cref="InteractiveClients"/>; never construct one. Identity-only —
/// which business party a client belongs to is each consuming service's own classification, never Auth's;
/// see that service's own <c>AGENTS.md</c>.
/// </summary>
public readonly record struct InteractiveClientInfo(
    InteractiveClient Client,
    string Id,
    string? MobileScheme)
{
    /// <summary>A native mobile app rather than a browser SPA.</summary>
    public bool IsMobile => MobileScheme is not null;
}
