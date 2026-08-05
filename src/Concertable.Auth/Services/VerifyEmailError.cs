using Concertable.Kernel.Errors;

namespace Concertable.Auth.Services;

public sealed record VerifyEmailError(ErrorDefinition Definition) : IError
{
    public static readonly VerifyEmailError InvalidOrExpiredToken = new(
        ErrorDefinition.Invalid(
            "auth.verification_link_invalid_or_expired",
            "This verification link is invalid or has expired."));
}
