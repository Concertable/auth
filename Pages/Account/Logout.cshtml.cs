using Duende.IdentityServer;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Concertable.Auth.Pages.Account;

public sealed class LogoutModel : PageModel
{
    private readonly IIdentityServerInteractionService interaction;

    public LogoutModel(IIdentityServerInteractionService interaction)
    {
        this.interaction = interaction;
    }

    [BindProperty(SupportsGet = true)] public string? LogoutId { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(IdentityServerConstants.DefaultCookieAuthenticationScheme);

        var context = await interaction.GetLogoutContextAsync(LogoutId);
        if (!string.IsNullOrEmpty(context?.PostLogoutRedirectUri))
            return Redirect(context.PostLogoutRedirectUri);

        return Redirect("/");
    }
}
