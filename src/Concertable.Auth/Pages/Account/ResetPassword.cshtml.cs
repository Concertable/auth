using Concertable.Auth.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace Concertable.Auth.Pages.Account;

[EnableRateLimiting(RateLimitPolicies.Credential)]
public sealed class ResetPasswordModel : PageModel
{
    private readonly IAuthService authService;

    public ResetPasswordModel(IAuthService authService)
    {
        this.authService = authService;
    }

    [BindProperty(SupportsGet = true)] public string Token { get; set; } = null!;
    [BindProperty] public string NewPassword { get; set; } = null!;

    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            ErrorMessage = "Invalid or expired reset link.";
            return Page();
        }

        var result = await authService.ResetPasswordAsync(Token, NewPassword, ct);
        Success = result.IsSuccess;
        if (result.TryGetError(out var error))
            ErrorMessage = error.Definition.Message;

        return Page();
    }
}
