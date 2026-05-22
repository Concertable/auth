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
        new ApiScope("concertable.api", "Concertable API"),
        new ApiScope("payment:write", "Payment write access"),
        new ApiScope("user:claims", "User claims access"),
    ];

    public static IEnumerable<ApiResource> ApiResources =>
    [
        new ApiResource("concertable.api", "Concertable API")
        {
            Scopes = { "concertable.api", "payment:write", "user:claims" }
        }
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

            AllowedScopes = { "openid", "profile", "concertable.api" },

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
        AllowedScopes = { "openid", "concertable.api" },
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

        AllowedScopes = { "openid", "profile", "roles", "concertable.api" },

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
