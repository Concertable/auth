using Dunet;
using Reunion.Errors;

namespace Concertable.Auth.Domain;

[Union(EnableImplicitConversions = false)]
public abstract partial record ResetPasswordError : IError
{
    public ErrorDefinition Definition => this switch
    {
        InvalidOrExpiredToken => ErrorDefinition.Invalid<InvalidOrExpiredToken>(
            "Invalid or expired reset link.")
    };

    public partial record InvalidOrExpiredToken;
}
