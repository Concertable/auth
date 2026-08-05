using Concertable.Kernel.Errors;

namespace Concertable.Auth.Services;

public sealed record ResetPasswordError(ErrorDefinition Definition) : IError
{
    public static readonly ResetPasswordError InvalidOrExpiredToken = new(
        ErrorDefinition.Invalid(
            "auth.reset_link_invalid_or_expired",
            "Invalid or expired reset link."));
}
