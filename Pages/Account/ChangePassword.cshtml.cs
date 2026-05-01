using Concertable.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Concertable.Auth.Pages.Account;

[Authorize]
public sealed class ChangePasswordModel : PageModel
{
    private readonly IAuthService authService;

    public ChangePasswordModel(IAuthService authService)
    {
        this.authService = authService;
    }

    [BindProperty] public string CurrentPassword { get; set; } = null!;
    [BindProperty] public string NewPassword { get; set; } = null!;

    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var sub = User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(sub, out var userId))
        {
            ErrorMessage = "Could not identify your account.";
            return Page();
        }

        Success = await authService.ChangePasswordAsync(userId, CurrentPassword, NewPassword, ct);
        if (!Success)
            ErrorMessage = "Current password is incorrect.";

        return Page();
    }
}
