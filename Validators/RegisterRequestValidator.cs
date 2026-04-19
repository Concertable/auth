using Concertable.Auth.Controllers;
using Concertable.Auth.Data;
using FluentValidation;

namespace Concertable.Auth.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Role).IsInEnum().NotEqual(Role.Admin)
            .WithMessage("Cannot self-register as Admin");
    }
}
