namespace Concertable.Auth.Settings;

public sealed class SpaClientSettings
{
    public const string SectionName = "Auth:SpaClient";

    public string[] RedirectUris { get; init; } = [];
    public string[] PostLogoutRedirectUris { get; init; } = [];
    public string[] AllowedCorsOrigins { get; init; } = [];
}
