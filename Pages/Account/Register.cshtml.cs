using Concertable.Auth.Services;
using Concertable.User.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Concertable.Auth.Pages.Account;

public sealed class RegisterModel : PageModel
{
    private readonly IAuthService authService;

    public RegisterModel(IAuthService authService)
    {
        this.authService = authService;
    }

    [BindProperty] public string Email { get; set; } = null!;
    [BindProperty] public string Password { get; set; } = null!;
    [BindProperty] public Role Role { get; set; }

    public bool Submitted { get; private set; }
    public string? ErrorMessage { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var verifyUrl = $"{Request.Scheme}://{Request.Host}/Account/VerifyEmail";
        var result = await authService.RegisterAsync(Email, Password, Role, verifyUrl, ct);

        switch (result)
        {
            case RegisterResult.Success:
                Submitted = true;
                break;
            case RegisterResult.EmailAlreadyExists:
                ErrorMessage = "An account with that email already exists.";
                break;
            case RegisterResult.RoleNotAllowed:
                ErrorMessage = "Cannot self-assign this role.";
                break;
            case RegisterResult.InvalidRole:
                ErrorMessage = "Invalid role.";
                break;
        }

        return Page();
    }
}
