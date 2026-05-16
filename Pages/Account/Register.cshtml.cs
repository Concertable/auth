using Concertable.Auth.Services;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Concertable.Auth.Pages.Account;

public sealed class RegisterModel : PageModel
{
    private readonly IAuthService authService;
    private readonly IIdentityServerInteractionService interaction;

    public RegisterModel(IAuthService authService, IIdentityServerInteractionService interaction)
    {
        this.authService = authService;
        this.interaction = interaction;
    }

    [BindProperty] public string Email { get; set; } = null!;
    [BindProperty] public string Password { get; set; } = null!;
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public bool Submitted { get; private set; }
    public string? ErrorMessage { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var role = await ClientRoleResolver.ResolveFromReturnUrlAsync(interaction, ReturnUrl);
        if (role is null)
        {
            ErrorMessage = "Sign up must be initiated from a Concertable surface.";
            return Page();
        }

        var verifyUrl = $"{Request.Scheme}://{Request.Host}/Account/VerifyEmail";
        var result = await authService.RegisterAsync(Email, Password, role.Value, verifyUrl, ct);

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
