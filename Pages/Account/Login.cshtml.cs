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
    private readonly IClientRoleResolver clientRoleResolver;

    public LoginModel(IAuthService authService, IIdentityServerInteractionService interaction, IClientRoleResolver clientRoleResolver)
    {
        this.authService = authService;
        this.interaction = interaction;
        this.clientRoleResolver = clientRoleResolver;
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

        var allowedRoles = await clientRoleResolver.GetAllowedRolesAsync(ReturnUrl);
        if (allowedRoles.Count > 0)
        {
            var userRole = principal.FindFirst("role")?.Value;
            if (userRole is null || !allowedRoles.Any(r => r.ToString() == userRole))
            {
                ErrorMessage = "This account doesn't have access to this application.";
                return Page();
            }
        }

        await HttpContext.SignInAsync(IdentityServerConstants.DefaultCookieAuthenticationScheme, principal);

        if (interaction.IsValidReturnUrl(ReturnUrl) || Url.IsLocalUrl(ReturnUrl))
            return Redirect(ReturnUrl!);

        return Redirect("/");
    }
}
