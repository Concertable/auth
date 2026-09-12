using System.Collections.Frozen;
using System.Collections.Immutable;

namespace Concertable.Auth.Contracts;

/// <summary>
/// The resource-server roster — the JWT audience, accepted scopes and included user claims per API. Auth
/// registers these as identity-server API resources; each resource server validates its own
/// <see cref="Audience"/>. The one home for the audience strings, including <c>concertable.payment.api</c>,
/// whose audience is not one of its scopes.
/// </summary>
public static class AuthResources
{
    private readonly record struct Row(
        string Audience,
        ImmutableArray<AuthScope> Scopes,
        ImmutableArray<string> IncludedClaims);

    private static readonly FrozenDictionary<AuthResource, Row> ByResource = new Dictionary<AuthResource, Row>
    {
        [AuthResource.B2B] = new("concertable.b2b.api", [AuthScope.B2BApi], ["email"]),
        [AuthResource.Customer] = new("concertable.customer.api", [AuthScope.CustomerApi, AuthScope.UserClaims], ["role", "owner"]),
        [AuthResource.Search] = new("concertable.search.api", [AuthScope.SearchApi], []),
        [AuthResource.Payment] = new("concertable.payment.api", [AuthScope.PaymentWrite], []),
    }.ToFrozenDictionary();

    /// <summary>Every resource server.</summary>
    public static IReadOnlyCollection<AuthResource> All => ByResource.Keys;

    extension(AuthResource resource)
    {
        /// <summary>The JWT audience a resource server validates, e.g. <c>concertable.b2b.api</c>.</summary>
        public string Audience => ByResource[resource].Audience;

        /// <summary>The scopes the resource accepts.</summary>
        public ImmutableArray<AuthScope> AcceptedScopes => ByResource[resource].Scopes;

        /// <summary>The user-claim types Auth includes in tokens issued for the resource.</summary>
        public ImmutableArray<string> IncludedClaims => ByResource[resource].IncludedClaims;
    }
}
