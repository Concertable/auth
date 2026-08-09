using Reunion.Errors;
using Dunet;

namespace Concertable.Auth.Services;

[Union(EnableImplicitConversions = false)]
public abstract partial record VerifyEmailError : IError
{
    public ErrorDefinition Definition => this switch
    {
        InvalidOrExpiredToken => ErrorDefinition.For<VerifyEmailError>().Invalid<InvalidOrExpiredToken>(
            "This verification link is invalid or has expired.")
    };

    public partial record InvalidOrExpiredToken;
}
