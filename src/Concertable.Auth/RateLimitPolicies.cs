namespace Concertable.Auth;

public static class RateLimitPolicies
{
    public const string Credential = "credential";
    public const string ChangePassword = "change-password";

    public static readonly IReadOnlyList<string> All = [Credential, ChangePassword];
}
