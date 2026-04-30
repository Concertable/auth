using System.Security.Claims;
using Concertable.User.Contracts;
using Duende.IdentityServer;
using Duende.IdentityServer.Services;

namespace Concertable.Auth.Services;

internal sealed class AuthService : IAuthService
{
    private readonly IUserModule userModule;
    private readonly IPasswordHasher passwordHasher;
    private readonly IIdentityServerInteractionService interaction;

    public AuthService(
        IUserModule userModule,
        IPasswordHasher passwordHasher,
        IIdentityServerInteractionService interaction)
    {
        this.userModule = userModule;
        this.passwordHasher = passwordHasher;
        this.interaction = interaction;
    }

    public async Task<ClaimsPrincipal?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var creds = await userModule.GetCredentialsByEmailAsync(email, ct);
        if (creds is null || !passwordHasher.Verify(password, creds.PasswordHash))
            return null;

        var claims = new List<Claim> { new("sub", creds.Id.ToString()) };
        var identity = new ClaimsIdentity(claims, IdentityServerConstants.DefaultCookieAuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    public async Task<string?> LogoutAsync(string? logoutId, CancellationToken ct = default)
    {
        var context = await interaction.GetLogoutContextAsync(logoutId);
        return context?.PostLogoutRedirectUri;
    }
}
