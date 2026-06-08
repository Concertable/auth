namespace Concertable.Auth.Data;

internal static class Schema
{
    public const string Name = "auth";

    public static class Tables
    {
        public const string Credentials = "Credentials";
        public const string EmailVerificationTokens = "EmailVerificationTokens";
        public const string PasswordResetTokens = "PasswordResetTokens";
    }
}
