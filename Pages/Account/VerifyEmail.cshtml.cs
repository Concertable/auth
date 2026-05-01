using Concertable.Auth.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Concertable.Auth.Pages.Account;

public sealed class VerifyEmailModel : PageModel
{
    private readonly IAuthService authService;

    public VerifyEmailModel(IAuthService authService)
    {
        this.authService = authService;
    }

    public bool Success { get; private set; }

    public async Task OnGetAsync(string? token, CancellationToken ct)
    {
        Success = !string.IsNullOrWhiteSpace(token)
            && await authService.VerifyEmailAsync(token, ct);
    }
}
