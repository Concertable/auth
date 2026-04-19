using Concertable.Auth.Data;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace Concertable.Auth.Services;

public class AuthService
{
    private readonly UserStore _userStore;
    private readonly IIdentityServerInteractionService _interaction;

    public AuthService(UserStore userStore, IIdentityServerInteractionService interaction)
    {
        _userStore = userStore;
        _interaction = interaction;
    }

    public async Task<string?> LoginAsync(string email, string password, string returnUrl, HttpContext httpContext)
    {
        var user = await _userStore.FindByEmailAsync(email);
        if (user is null || !_userStore.ValidatePassword(user, password))
            return null;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "local"));
        await httpContext.SignInAsync(principal);

        return returnUrl;
    }

    public async Task<bool> RegisterAsync(string email, string password, Role role)
    {
        var existing = await _userStore.FindByEmailAsync(email);
        if (existing is not null) return false;

        await _userStore.CreateAsync(email, password, role);
        return true;
    }

    public async Task<string?> LogoutAsync(string logoutId, HttpContext httpContext)
    {
        await httpContext.SignOutAsync();

        var logout = await _interaction.GetLogoutContextAsync(logoutId);
        return logout?.PostLogoutRedirectUri;
    }
}
