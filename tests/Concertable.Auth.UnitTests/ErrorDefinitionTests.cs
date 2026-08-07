using Concertable.Auth.Services;
using Concertable.Kernel.Errors;

namespace Concertable.Auth.UnitTests;

public sealed class ErrorDefinitionTests
{
    public static TheoryData<IError, string, string, ErrorKind> Definitions => new()
    {
        {
            new RegisterError.EmailAlreadyExists(),
            "register.email_already_exists",
            "An account with that email already exists.",
            ErrorKind.Conflict
        },
        {
            new ChangePasswordError.CurrentPasswordIncorrect(),
            "change.password_current_password_incorrect",
            "Current password is incorrect.",
            ErrorKind.Unauthenticated
        },
        {
            new VerifyEmailError.InvalidOrExpiredToken(),
            "verify.email_invalid_or_expired_token",
            "This verification link is invalid or has expired.",
            ErrorKind.Invalid
        },
        {
            new ResetPasswordError.InvalidOrExpiredToken(),
            "reset.password_invalid_or_expired_token",
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
