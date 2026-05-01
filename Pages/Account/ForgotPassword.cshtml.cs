using Concertable.Auth.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Concertable.Auth.Pages.Account;

public sealed class ForgotPasswordModel : PageModel
{
    private readonly IAuthService authService;

    public ForgotPasswordModel(IAuthService authService)
    {
        this.authService = authService;
    }

    [BindProperty] public string Email { get; set; } = string.Empty;
    public bool Submitted { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var resetUrl = $"{Request.Scheme}://{Request.Host}/Account/ResetPassword";
        await authService.SendPasswordResetAsync(Email, resetUrl, ct);
        Submitted = true;
        return Page();
    }
}
