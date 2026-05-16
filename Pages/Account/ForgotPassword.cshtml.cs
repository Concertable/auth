using Concertable.Auth.Services;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Concertable.Auth.Pages.Account;

public sealed class ForgotPasswordModel : PageModel
{
    private readonly IAuthService authService;
    private readonly IIdentityServerInteractionService interaction;

    public ForgotPasswordModel(IAuthService authService, IIdentityServerInteractionService interaction)
    {
        this.authService = authService;
        this.interaction = interaction;
    }

    [BindProperty] public string Email { get; set; } = null!;
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public bool Submitted { get; private set; }
    public string? ErrorMessage { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var role = await ClientRoleResolver.ResolveFromReturnUrlAsync(interaction, ReturnUrl);
        if (role is null)
        {
            ErrorMessage = "Password reset must be initiated from a Concertable surface.";
            return Page();
        }

        var resetUrl = $"{Request.Scheme}://{Request.Host}/Account/ResetPassword";
        await authService.SendPasswordResetAsync(Email, role.Value, resetUrl, ct);
        Submitted = true;
        return Page();
    }
}
