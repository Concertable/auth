using Concertable.Auth.Settings;
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

    public static Client TestClient => new Client
    {
        ClientId = "concertable-test",
        AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
        RequireClientSecret = false,
        AllowedScopes = { "openid", "concertable.api" },
    };

    public static IEnumerable<Client> Clients(SpaClientSettings spa) =>
    [
        new Client
        {
            ClientId = "concertable-spa",

            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,
            RequireClientSecret = false,

            RedirectUris = spa.RedirectUris,
            PostLogoutRedirectUris = spa.PostLogoutRedirectUris,
            AllowedCorsOrigins = spa.AllowedCorsOrigins,

            AllowedScopes = { "openid", "profile", "roles", "concertable.api" },

            AllowOfflineAccess = true,
            AccessTokenLifetime = 900,

            RefreshTokenUsage = TokenUsage.OneTimeOnly,
            RefreshTokenExpiration = TokenExpiration.Sliding,
            SlidingRefreshTokenLifetime = 60 * 60 * 24 * 30
        }
    ];
}
