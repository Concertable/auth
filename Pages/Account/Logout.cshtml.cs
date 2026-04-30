using Concertable.Auth.Services;
using Duende.IdentityServer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Concertable.Auth.Pages.Account;

public sealed class LogoutModel : PageModel
{
    private readonly IAuthService authService;

    public LogoutModel(IAuthService authService)
    {
        this.authService = authService;
    }

    [BindProperty(SupportsGet = true)] public string? LogoutId { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await HttpContext.SignOutAsync(IdentityServerConstants.DefaultCookieAuthenticationScheme);

        var redirect = await authService.LogoutAsync(LogoutId, ct);
        return Redirect(redirect ?? "/");
    }
}
