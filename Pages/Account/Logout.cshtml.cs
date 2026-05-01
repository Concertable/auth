using Concertable.Auth.Services;
using Duende.IdentityServer;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Concertable.Auth.Pages.Account;

public sealed class LogoutModel : PageModel
{
    private readonly IAuthService authService;
    private readonly IIdentityServerInteractionService interaction;

    public LogoutModel(IAuthService authService, IIdentityServerInteractionService interaction)
    {
        this.authService = authService;
        this.interaction = interaction;
    }

    [BindProperty(SupportsGet = true)] public string? LogoutId { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var context = await interaction.GetLogoutContextAsync(LogoutId);
        if (context?.ShowSignoutPrompt == false)
            return await SignOutAndRedirectAsync(ct);
        return Page();
    }

    public Task<IActionResult> OnPostAsync(CancellationToken ct) => SignOutAndRedirectAsync(ct);

    private async Task<IActionResult> SignOutAndRedirectAsync(CancellationToken ct)
    {
        await HttpContext.SignOutAsync(IdentityServerConstants.DefaultCookieAuthenticationScheme);
        var redirect = await authService.LogoutAsync(LogoutId, ct);
        return Redirect(redirect ?? "/");
    }
}
