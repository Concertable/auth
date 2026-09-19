namespace Concertable.Auth.Data;

internal static class Schema
{
    public const string Name = "auth";
    public const string Grants = "idsrv";
    public const string Messaging = "messaging";

    public static IReadOnlyList<string> Owned { get; } = [Name, Grants, Messaging];

    public static class Tables
    {
        public const string Credentials = "Credentials";
        public const string EmailVerificationTokens = "EmailVerificationTokens";
        public const string PasswordResetTokens = "PasswordResetTokens";
    }
}
