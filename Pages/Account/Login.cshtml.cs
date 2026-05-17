using Concertable.Auth.Services;
using Duende.IdentityServer;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Concertable.Auth.Pages.Account;

public sealed class LoginModel : PageModel
{
    private readonly IAuthService authService;
    private readonly IIdentityServerInteractionService interaction;

    public LoginModel(IAuthService authService, IIdentityServerInteractionService interaction)
    {
        this.authService = authService;
        this.interaction = interaction;
    }

    [BindProperty] public string Email { get; set; } = null!;
    [BindProperty] public string Password { get; set; } = null!;
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var principal = await authService.LoginAsync(Email, Password, ct);
        if (principal is null)
        {
            ErrorMessage = "Invalid email or password.";
            return Page();
        }

        await HttpContext.SignInAsync(IdentityServerConstants.DefaultCookieAuthenticationScheme, principal);

        if (interaction.IsValidReturnUrl(ReturnUrl) || Url.IsLocalUrl(ReturnUrl))
            return Redirect(ReturnUrl!);

        return Redirect("/");
    }
}
