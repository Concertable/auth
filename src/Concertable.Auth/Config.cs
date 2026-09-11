using System.Security.Cryptography;
using System.Text;
using Concertable.Auth.Contracts;
using Concertable.Auth.Settings;
using Duende.IdentityServer.Models;

namespace Concertable.Auth;

public static class Config
{
    private static readonly IReadOnlyDictionary<AuthScope, string> ScopeDisplayNames = new Dictionary<AuthScope, string>
    {
        [AuthScope.B2BApi] = "Concertable B2B API",
        [AuthScope.CustomerApi] = "Concertable Customer API",
        [AuthScope.SearchApi] = "Concertable Search API",
        [AuthScope.PaymentWrite] = "Payment write access",
        [AuthScope.UserClaims] = "User claims access",
    };

    private static readonly IReadOnlyDictionary<AuthResource, string> ResourceDisplayNames = new Dictionary<AuthResource, string>
    {
        [AuthResource.B2B] = "Concertable B2B API",
        [AuthResource.Customer] = "Concertable Customer API",
        [AuthResource.Search] = "Concertable Search API",
        [AuthResource.Payment] = "Concertable Payment API",
    };

    public static IReadOnlyList<ApiScope> ApiScopes =>
        AuthScopes.All.Select(scope => new ApiScope(scope.Id(), ScopeDisplayNames[scope])).ToArray();

    /* B2B is identity-only: `email` comes from the local Auth credential, and authority is the
       request-scoped active tenant (X-Tenant-Id → membership), never a token claim. No `role`, no
       `owner` — one claim can't model a multi-tenant user. `owner` stays Customer-only. */
    public static IReadOnlyList<ApiResource> ApiResources =>
        AuthResources.All.Select(resource => new ApiResource(resource.Audience(), ResourceDisplayNames[resource])
        {
            Scopes = resource.AcceptedScopes().Select(scope => scope.Id()).ToList(),
            UserClaims = resource.IncludedClaims().ToList(),
        }).ToArray();

    public static IReadOnlyList<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
        new IdentityResource("roles", new[] { "role" }),
    ];

    public static Client CustomerMobileClient(string? expoGoRedirectUri = null) =>
        MobileClient(InteractiveClient.CustomerMobile, expoGoRedirectUri);

    public static Client VenueMobileClient(string? expoGoRedirectUri = null) =>
        MobileClient(InteractiveClient.VenueMobile, expoGoRedirectUri);

    public static Client ArtistMobileClient(string? expoGoRedirectUri = null) =>
        MobileClient(InteractiveClient.ArtistMobile, expoGoRedirectUri);

    private static Client MobileClient(InteractiveClient client, string? expoGoRedirectUri)
    {
        var info = client.Info();
        var scheme = info.MobileScheme
            ?? throw new InvalidOperationException($"{client} has no mobile redirect scheme.");
        var redirectUris = new HashSet<string> { scheme };
        if (!string.IsNullOrEmpty(expoGoRedirectUri))
            redirectUris.Add(expoGoRedirectUri);

        return new Client
        {
            ClientId = info.Id,

            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,
            RequireClientSecret = false,

            RedirectUris = redirectUris,
            PostLogoutRedirectUris = { scheme },

            AllowedScopes = client == InteractiveClient.CustomerMobile
                ? new HashSet<string> { "openid", "profile", AuthScope.CustomerApi.Id() }
                : new HashSet<string> { "openid", "profile", AuthScope.B2BApi.Id() },

            AllowOfflineAccess = true,
            AccessTokenLifetime = 900,

            RefreshTokenUsage = TokenUsage.OneTimeOnly,
            RefreshTokenExpiration = TokenExpiration.Sliding,
            SlidingRefreshTokenLifetime = 60 * 60 * 24 * 30
        };
    }

    public static Client TestClient => new Client
    {
        ClientId = InteractiveClient.E2ETest.Info().Id,
        AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
        RequireClientSecret = false,
        AllowedScopes = { "openid", AuthScope.B2BApi.Id(), AuthScope.CustomerApi.Id(), AuthScope.SearchApi.Id() },
    };

    public static IReadOnlyList<Client> WebClients(SpaClientSettings spa)
    {
        (string Name, InteractiveClient Client, WebClientSettings Settings)[] definitions =
        [
            (nameof(SpaClientSettings.Customer), InteractiveClient.CustomerBrowser, spa.Customer),
            (nameof(SpaClientSettings.Venue), InteractiveClient.VenueBrowser, spa.Venue),
            (nameof(SpaClientSettings.Artist), InteractiveClient.ArtistBrowser, spa.Artist),
            (nameof(SpaClientSettings.Admin), InteractiveClient.Admin, spa.Admin),
        ];

        if (!spa.RestrictToEnabledClients)
            return definitions.Select(definition => WebClient(definition.Client, definition.Settings)).ToArray();

        var enabled = spa.EnabledClients?.ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? [];
        var unknown = enabled
            .Except(definitions.Select(definition => definition.Name), StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (unknown.Length > 0)
            throw new InvalidOperationException($"Unknown Auth SPA clients: {string.Join(", ", unknown)}.");

        return definitions
            .Where(definition => enabled.Contains(definition.Name))
            .Select(definition => WebClient(definition.Client, definition.Settings))
            .ToArray();
    }

    private static Client WebClient(InteractiveClient client, WebClientSettings settings) => new()
    {
        ClientId = client.Info().Id,

        AllowedGrantTypes = GrantTypes.Code,
        RequirePkce = true,
        RequireClientSecret = false,

        RedirectUris = [settings.RedirectUri],
        PostLogoutRedirectUris = [settings.PostLogoutRedirectUri],
        AllowedCorsOrigins = settings.AllowedCorsOrigins,

        AllowedScopes = client == InteractiveClient.CustomerBrowser
            ? new HashSet<string> { "openid", "profile", "roles", AuthScope.CustomerApi.Id(), AuthScope.SearchApi.Id() }
            : new HashSet<string> { "openid", "profile", AuthScope.B2BApi.Id() },

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
