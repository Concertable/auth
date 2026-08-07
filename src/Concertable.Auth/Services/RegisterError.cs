using Concertable.Kernel.Errors;
using Dunet;

namespace Concertable.Auth.Services;

[Union(EnableImplicitConversions = false)]
public abstract partial record RegisterError : IError
{
    public ErrorDefinition Definition => this switch
    {
        EmailAlreadyExists => ErrorDefinition.Conflict<EmailAlreadyExists>(
            "An account with that email already exists.")
    };

    public partial record EmailAlreadyExists;
}
