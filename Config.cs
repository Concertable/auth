using Concertable.Auth.Settings;
using Concertable.User.Contracts;
using Duende.IdentityServer.Models;

namespace Concertable.Auth;

public static class Config
{
    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new ApiScope("concertable.api", "Concertable API")
    ];

    public static IEnumerable<ApiResource> ApiResources =>
    [
        new ApiResource("concertable.api", "Concertable API")
        {
            Scopes = { "concertable.api" },
            UserClaims = { "role" }
        }
    ];

    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
        new IdentityResource("roles", "User roles", ["role"])
    ];

    public static IReadOnlyDictionary<string, Role> ClientRoleMap { get; } = new Dictionary<string, Role>
    {
        ["customer-web"] = Role.Customer,
        ["customer-mobile"] = Role.Customer,
        ["venue-web"] = Role.VenueManager,
        ["venue-mobile"] = Role.VenueManager,
        ["artist-web"] = Role.ArtistManager,
        ["artist-mobile"] = Role.ArtistManager,
    };

    public static Client CustomerMobileClient(string? expoGoRedirectUri = null) =>
        MobileClient("customer-mobile", "concertable-customer://", expoGoRedirectUri);

    public static Client VenueMobileClient(string? expoGoRedirectUri = null) =>
        MobileClient("venue-mobile", "concertable-venue://", expoGoRedirectUri);

    public static Client ArtistMobileClient(string? expoGoRedirectUri = null) =>
        MobileClient("artist-mobile", "concertable-artist://", expoGoRedirectUri);

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

            AllowedScopes = { "openid", "profile", "roles", "concertable.api" },

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
        WebClient("customer-web", spa.Customer),
        WebClient("venue-web", spa.Venue),
        WebClient("artist-web", spa.Artist),
    ];

    private static Client WebClient(string clientId, WebClientSettings settings) => new()
    {
        ClientId = clientId,

        AllowedGrantTypes = GrantTypes.Code,
        RequirePkce = true,
        RequireClientSecret = false,

        RedirectUris = settings.RedirectUris,
        PostLogoutRedirectUris = settings.PostLogoutRedirectUris,
        AllowedCorsOrigins = settings.AllowedCorsOrigins,

        AllowedScopes = { "openid", "profile", "roles", "concertable.api" },

        AllowOfflineAccess = true,
        AccessTokenLifetime = 900,

        RefreshTokenUsage = TokenUsage.OneTimeOnly,
        RefreshTokenExpiration = TokenExpiration.Sliding,
        SlidingRefreshTokenLifetime = 60 * 60 * 24 * 30
    };
}
