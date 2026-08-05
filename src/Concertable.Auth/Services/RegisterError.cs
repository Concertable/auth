using Concertable.Kernel.Errors;

namespace Concertable.Auth.Services;

public sealed record RegisterError(ErrorDefinition Definition) : IError
{
    public static readonly RegisterError EmailAlreadyExists = new(
        ErrorDefinition.Conflict(
            "auth.email_already_exists",
            "An account with that email already exists."));
}
