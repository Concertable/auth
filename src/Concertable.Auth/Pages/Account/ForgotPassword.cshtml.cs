using Concertable.Auth.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace Concertable.Auth.Pages.Account;

[EnableRateLimiting(RateLimitPolicies.Credential)]
public sealed class ForgotPasswordModel : PageModel
{
    private readonly IAuthService authService;

    public ForgotPasswordModel(IAuthService authService)
    {
        this.authService = authService;
    }

    [BindProperty] public string Email { get; set; } = null!;
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public bool Submitted { get; private set; }
    public string? ErrorMessage { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var resetUrl = $"{Request.Scheme}://{Request.Host}/Account/ResetPassword";
        await authService.SendPasswordResetAsync(Email, resetUrl, ct);
        Submitted = true;
        return Page();
    }
}
