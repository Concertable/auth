namespace Concertable.Auth.Settings;

public sealed class SpaClientSettings
{
    public const string SectionName = "Auth:SpaClients";

    public bool RestrictToEnabledClients { get; init; }
    public string[]? EnabledClients { get; init; }
    public WebClientSettings Customer { get; init; } = null!;
    public WebClientSettings Venue { get; init; } = null!;
    public WebClientSettings Artist { get; init; } = null!;
    public WebClientSettings Admin { get; init; } = null!;
}

public sealed class WebClientSettings
{
    public string RedirectUri { get; init; } = null!;
    public string PostLogoutRedirectUri { get; init; } = null!;
    public string[] AllowedCorsOrigins { get; init; } = [];
}
