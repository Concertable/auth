using System.Security.Claims;
using Concertable.Auth.Services;
using Concertable.User.Contracts;
using Duende.IdentityServer;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Concertable.Auth.Pages.Account;

public sealed class LoginModel : PageModel
{
    private readonly IUserModule userModule;
    private readonly IIdentityServerInteractionService interaction;
    private readonly IPasswordHasher passwordHasher;

    public LoginModel(
        IUserModule userModule,
        IIdentityServerInteractionService interaction,
        IPasswordHasher passwordHasher)
    {
        this.userModule = userModule;
        this.interaction = interaction;
        this.passwordHasher = passwordHasher;
    }

    [BindProperty] public string Email { get; set; } = string.Empty;
    [BindProperty] public string Password { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var creds = await userModule.GetCredentialsByEmailAsync(Email);
        if (creds is null || !passwordHasher.Verify(Password, creds.PasswordHash))
        {
            ErrorMessage = "Invalid email or password.";
            return Page();
        }

        var claims = new List<Claim> { new("sub", creds.Id.ToString()) };
        var identity = new ClaimsIdentity(claims, IdentityServerConstants.DefaultCookieAuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(IdentityServerConstants.DefaultCookieAuthenticationScheme, principal);

        if (interaction.IsValidReturnUrl(ReturnUrl) || Url.IsLocalUrl(ReturnUrl))
            return Redirect(ReturnUrl!);

        return Redirect("/");
    }
}
