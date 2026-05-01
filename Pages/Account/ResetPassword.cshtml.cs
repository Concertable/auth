using Concertable.Auth.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Concertable.Auth.Pages.Account;

public sealed class ResetPasswordModel : PageModel
{
    private readonly IAuthService authService;

    public ResetPasswordModel(IAuthService authService)
    {
        this.authService = authService;
    }

    [BindProperty(SupportsGet = true)] public string Token { get; set; } = string.Empty;
    [BindProperty] public string NewPassword { get; set; } = string.Empty;

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

        Success = await authService.ResetPasswordAsync(Token, NewPassword, ct);
        if (!Success)
            ErrorMessage = "Invalid or expired reset link.";

        return Page();
    }
}
