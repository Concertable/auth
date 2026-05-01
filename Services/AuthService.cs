using System.Security.Claims;
using Concertable.Application.Interfaces;
using Concertable.User.Contracts;
using Duende.IdentityServer;
using Duende.IdentityServer.Services;

namespace Concertable.Auth.Services;

internal sealed class AuthService : IAuthService
{
    private readonly IUserModule userModule;
    private readonly IPasswordHasher passwordHasher;
    private readonly IIdentityServerInteractionService interaction;
    private readonly IEmailService emailService;

    public AuthService(
        IUserModule userModule,
        IPasswordHasher passwordHasher,
        IIdentityServerInteractionService interaction,
        IEmailService emailService)
    {
        this.userModule = userModule;
        this.passwordHasher = passwordHasher;
        this.interaction = interaction;
        this.emailService = emailService;
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

    public async Task SendEmailVerificationAsync(Guid userId, string verifyUrl, CancellationToken ct = default)
    {
        var token = await userModule.CreateEmailVerificationTokenAsync(userId, ct);
        if (token is null) return;

        var creds = await userModule.GetCredentialsByIdAsync(userId, ct);
        if (creds is null) return;

        var link = $"{verifyUrl}?token={Uri.EscapeDataString(token)}";
        await emailService.SendEmailAsync(creds.Email, "Verify your email",
            $"Click here to verify your email: {link}");
    }

    public Task<bool> VerifyEmailAsync(string token, CancellationToken ct = default) =>
        userModule.VerifyEmailWithTokenAsync(token, ct);

    public async Task SendPasswordResetAsync(string email, string resetUrl, CancellationToken ct = default)
    {
        var token = await userModule.CreatePasswordResetTokenAsync(email, ct);
        if (token is null) return;

        var link = $"{resetUrl}?token={Uri.EscapeDataString(token)}";
        await emailService.SendEmailAsync(email, "Reset your password",
            $"Click here to reset your password: {link}. This link expires in 1 hour.");
    }

    public Task<bool> ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default) =>
        userModule.ResetPasswordWithTokenAsync(token, passwordHasher.Hash(newPassword), ct);
}
