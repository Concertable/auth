using Concertable.Kernel.Errors;

namespace Concertable.Auth.Services;

public sealed record ChangePasswordError(ErrorDefinition Definition) : IError
{
    public static readonly ChangePasswordError CurrentPasswordIncorrect = new(
        ErrorDefinition.Unauthenticated(
            "auth.current_password_incorrect",
            "Current password is incorrect."));
}
