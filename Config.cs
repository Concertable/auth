using Duende.IdentityServer.Models;

internal static class Config
{
    public static IEnumerable<Client> Clients =>
    [
        new Client
        {
            ClientId = "concertable-react",
            RequireClientSecret = false,
            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,
            AllowOfflineAccess = true,
            RedirectUris = ["https://localhost:5173/callback"],
            PostLogoutRedirectUris = ["https://localhost:5173"],
            AllowedCorsOrigins = ["https://localhost:5173"],
            AllowedScopes = ["openid", "profile", "offline_access"]
        }
    ];

    public static IEnumerable<ApiScope> ApiScopes => [];

    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile()
    ];
}
