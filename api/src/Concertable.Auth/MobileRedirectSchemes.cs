using System.Collections.Frozen;
using Concertable.Auth.Contracts;

namespace Concertable.Auth;

/// <summary>
/// The native redirect scheme each mobile <see cref="InteractiveClient"/> registers, e.g.
/// <c>concertable-customer://</c>. Auth's own registration detail, not identity: the scheme is how one
/// deployment of one app receives an authorization response, so it never travels on
/// <see cref="Concertable.Auth.Contracts.Events.CredentialRegisteredEvent"/> and has no place in the
/// published contracts package. Browser clients have none.
/// </summary>
public static class MobileRedirectSchemes
{
    private static readonly FrozenDictionary<InteractiveClient, string> ByClient = new Dictionary<InteractiveClient, string>
    {
        [InteractiveClient.CustomerMobile] = "concertable-customer://",
        [InteractiveClient.VenueMobile] = "concertable-business://",
        [InteractiveClient.ArtistMobile] = "concertable-business://",
        [InteractiveClient.BusinessMobile] = "concertable-business://",
    }.ToFrozenDictionary();

    extension(InteractiveClient client)
    {
        /// <summary>The client's native redirect scheme, or <see langword="null"/> for a browser client.</summary>
        public string? MobileRedirectScheme => ByClient.GetValueOrDefault(client);
    }
}
