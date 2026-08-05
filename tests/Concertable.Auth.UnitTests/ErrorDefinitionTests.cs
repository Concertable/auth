using Concertable.Auth.Services;
using Concertable.Kernel.Errors;

namespace Concertable.Auth.UnitTests;

public sealed class ErrorDefinitionTests
{
    public static TheoryData<IError, string, string, ErrorKind> Definitions => new()
    {
        {
            RegisterError.EmailAlreadyExists,
            "auth.email_already_exists",
            "An account with that email already exists.",
            ErrorKind.Conflict
        },
        {
            ChangePasswordError.CurrentPasswordIncorrect,
            "auth.current_password_incorrect",
            "Current password is incorrect.",
            ErrorKind.Unauthenticated
        },
        {
            VerifyEmailError.InvalidOrExpiredToken,
            "auth.verification_link_invalid_or_expired",
            "This verification link is invalid or has expired.",
            ErrorKind.Invalid
        },
        {
            ResetPasswordError.InvalidOrExpiredToken,
            "auth.reset_link_invalid_or_expired",
            "Invalid or expired reset link.",
            ErrorKind.Invalid
        }
    };

    [Theory]
    [MemberData(nameof(Definitions))]
    public void Definition_DeclaredError_MatchesPublishedContract(
        IError error,
        string code,
        string message,
        ErrorKind kind)
    {
        Assert.Equal(code, error.Definition.Code);
        Assert.Equal(message, error.Definition.Message);
        Assert.Equal(kind, error.Definition.Kind);
    }
}
