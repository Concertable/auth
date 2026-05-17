namespace Concertable.Auth.Settings;

public sealed class SpaClientSettings
{
    public const string SectionName = "Auth:SpaClients";

    public WebClientSettings Customer { get; init; } = new();
    public WebClientSettings Venue { get; init; } = new();
    public WebClientSettings Artist { get; init; } = new();
    public WebClientSettings Business { get; init; } = new();
}

public sealed class WebClientSettings
{
    public string RedirectUri { get; init; } = string.Empty;
    public string PostLogoutRedirectUri { get; init; } = string.Empty;
    public string[] AllowedCorsOrigins { get; init; } = [];
}
