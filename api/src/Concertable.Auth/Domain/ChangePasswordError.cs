using Dunet;
using Reunion.Errors;

namespace Concertable.Auth.Domain;

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
