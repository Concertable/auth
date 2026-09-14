namespace Concertable.Auth.Contracts;

/// <summary>
/// An OAuth client a human signs in through — the browser SPAs and native apps (authorization code + PKCE)
/// and the E2E harness (resource-owner password). Resolve its wire id through
/// <see cref="InteractiveClientInfo"/>; the raw string travels on
/// <see cref="Events.CredentialRegisteredEvent"/>.
/// </summary>
public enum InteractiveClient
{
    CustomerBrowser,
    CustomerMobile,
    VenueBrowser,
    VenueMobile,
    ArtistBrowser,
    ArtistMobile,
    Admin,
    E2ETest,
}
