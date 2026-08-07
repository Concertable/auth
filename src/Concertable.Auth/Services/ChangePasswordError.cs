using Concertable.Kernel.Errors;
using Dunet;

namespace Concertable.Auth.Services;

[Union(EnableImplicitConversions = false)]
public abstract partial record ChangePasswordError : IError
{
    public ErrorDefinition Definition => this switch
    {
        CurrentPasswordIncorrect => ErrorDefinition.Unauthenticated<CurrentPasswordIncorrect>(
            "Current password is incorrect.")
    };

    public partial record CurrentPasswordIncorrect;
}
