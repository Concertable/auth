using Concertable.Kernel.Errors;
using Dunet;

namespace Concertable.Auth.Services;

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
