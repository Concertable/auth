using System.Security.Cryptography;
using System.Text;
using Concertable.Auth.Contracts;
using Concertable.Auth.Settings;
using Duende.IdentityServer.Models;

namespace Concertable.Auth;

public static class Config
{
    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new ApiScope("concertable.b2b.api",      "Concertable B2B API"),
        new ApiScope("concertable.customer.api",  "Concertable Customer API"),
        new ApiScope("concertable.search.api",    "Concertable Search API"),
        new ApiScope("payment:write",             "Payment write access"),
        new ApiScope("user:claims",               "User claims access"),
    ];

    public static IEnumerable<ApiResource> ApiResources =>
    [
        // The `owner` claim (Payment's opaque payout-account key) rides each data service's OWN api
        // resource — B2B issues its tenant id, Customer the user's own id — so interactive clients get it
        // by requesting the scope they already use. It must NOT hang off the payment resource: payment:write
        // is the service-only gate on the gRPC money surface, and a user token must never be able to carry it.
        new ApiResource("concertable.b2b.api", "Concertable B2B API")
        {
            Scopes = { "concertable.b2b.api", "user:claims" },
            UserClaims = { "role", "owner" }
        },
        new ApiResource("concertable.customer.api", "Concertable Customer API")
        {
            Scopes = { "concertable.customer.api", "user:claims" },
            UserClaims = { "role", "owner" }
        },
        new ApiResource("concertable.search.api", "Concertable Search API")
        {
            Scopes = { "concertable.search.api" }
        },
        new ApiResource("concertable.payment.api", "Concertable Payment API")
        {
            Scopes = { "payment:write" }
        },
    ];

    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
        new IdentityResource("roles", new[] { "role" }),
    ];

    public static Client CustomerMobileClient(string? expoGoRedirectUri = null) =>
        MobileClient(ClientIds.CustomerMobile, "concertable-customer://", expoGoRedirectUri);

    public static Client VenueMobileClient(string? expoGoRedirectUri = null) =>
        MobileClient(ClientIds.VenueMobile, "concertable-business://", expoGoRedirectUri);

    public static Client ArtistMobileClient(string? expoGoRedirectUri = null) =>
        MobileClient(ClientIds.ArtistMobile, "concertable-business://", expoGoRedirectUri);

    private static Client MobileClient(string clientId, string scheme, string? expoGoRedirectUri)
    {
        var redirectUris = new HashSet<string> { scheme };
        if (!string.IsNullOrEmpty(expoGoRedirectUri))
            redirectUris.Add(expoGoRedirectUri);

        return new Client
        {
            ClientId = clientId,

            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,
            RequireClientSecret = false,

            RedirectUris = redirectUris,
            PostLogoutRedirectUris = { scheme },

            AllowedScopes = clientId == ClientIds.CustomerMobile
                ? new HashSet<string> { "openid", "profile", "concertable.customer.api" }
                : new HashSet<string> { "openid", "profile", "concertable.b2b.api" },

            AllowOfflineAccess = true,
            AccessTokenLifetime = 900,

            RefreshTokenUsage = TokenUsage.OneTimeOnly,
            RefreshTokenExpiration = TokenExpiration.Sliding,
            SlidingRefreshTokenLifetime = 60 * 60 * 24 * 30
        };
    }

    public static Client TestClient => new Client
    {
        ClientId = "concertable-test",
        AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
        RequireClientSecret = false,
        AllowedScopes = { "openid", "concertable.b2b.api", "concertable.customer.api", "concertable.search.api" },
    };

    public static IEnumerable<Client> WebClients(SpaClientSettings spa) =>
    [
        WebClient(ClientIds.CustomerWeb, spa.Customer),
        WebClient(ClientIds.VenueWeb, spa.Venue),
        WebClient(ClientIds.ArtistWeb, spa.Artist),
    ];

    private static Client WebClient(string clientId, WebClientSettings settings) => new()
    {
        ClientId = clientId,

        AllowedGrantTypes = GrantTypes.Code,
        RequirePkce = true,
        RequireClientSecret = false,

        RedirectUris = [settings.RedirectUri],
        PostLogoutRedirectUris = [settings.PostLogoutRedirectUri],
        AllowedCorsOrigins = settings.AllowedCorsOrigins,

        AllowedScopes = clientId == ClientIds.CustomerWeb
            ? new HashSet<string> { "openid", "profile", "roles", "concertable.customer.api", "concertable.search.api" }
            : new HashSet<string> { "openid", "profile", "roles", "concertable.b2b.api" },

        AllowOfflineAccess = true,
        AccessTokenLifetime = 900,

        RefreshTokenUsage = TokenUsage.OneTimeOnly,
        RefreshTokenExpiration = TokenExpiration.Sliding,
        SlidingRefreshTokenLifetime = 60 * 60 * 24 * 30
    };

    public static Client ServiceClient(string clientId, string clientSecret, params string[] allowedScopes) => new()
    {
        ClientId = clientId,
        ClientSecrets = { new Secret(Sha256(clientSecret)) },
        AllowedGrantTypes = GrantTypes.ClientCredentials,
        AllowedScopes = allowedScopes.ToList()
    };

    private static string Sha256(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToBase64String(hash);
    }
}
