using System.Collections.Frozen;

namespace Concertable.Auth.Contracts;

/// <summary>Maps <see cref="AuthScope"/> to and from its wire string — the one place the scope literals live.</summary>
public static class AuthScopes
{
    private static readonly FrozenDictionary<AuthScope, string> ByScope = new Dictionary<AuthScope, string>
    {
        [AuthScope.B2BApi] = "concertable.b2b.api",
        [AuthScope.CustomerApi] = "concertable.customer.api",
        [AuthScope.SearchApi] = "concertable.search.api",
        [AuthScope.PaymentWrite] = "payment:write",
        [AuthScope.UserClaims] = "user:claims",
    }.ToFrozenDictionary();

    extension(AuthScope scope)
    {
        /// <summary>The wire scope string, e.g. <c>concertable.b2b.api</c>.</summary>
        public string Id => ByScope[scope];
    }

    /// <summary>Every issued scope.</summary>
    public static IReadOnlyCollection<AuthScope> All => ByScope.Keys;
}
