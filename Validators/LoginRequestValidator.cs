using Concertable.Auth.Controllers;
using FluentValidation;

namespace Concertable.Auth.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.ReturnUrl).NotEmpty();
    }
}
