using System.Security.Claims;
using Concertable.Auth.Data;
using Concertable.Auth.Data.Entities;
using Concertable.Shared.Email.Application;
using Duende.IdentityServer;
using Duende.IdentityServer.Services;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Auth.Services;

internal sealed class AuthService : IAuthService
{
    private readonly AuthDbContext authContext;
    private readonly IPasswordHasher passwordHasher;
    private readonly IIdentityServerInteractionService interaction;
    private readonly IEmailSender emailSender;

    public AuthService(
        AuthDbContext authContext,
        IPasswordHasher passwordHasher,
        IIdentityServerInteractionService interaction,
        IEmailSender emailSender)
    {
        this.authContext = authContext;
        this.passwordHasher = passwordHasher;
        this.interaction = interaction;
        this.emailSender = emailSender;
    }

    public async Task<ClaimsPrincipal?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var credential = await authContext.Credentials.FirstOrDefaultAsync(c => c.Email == email, ct);
        if (credential is null || !passwordHasher.Verify(password, credential.PasswordHash))
            return null;

        if (!credential.IsEmailVerified)
            return null;

        var claims = new List<Claim> { new("sub", credential.Id.ToString()) };
        var identity = new ClaimsIdentity(claims, IdentityServerConstants.DefaultCookieAuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    public async Task<RegisterResult> RegisterAsync(string email, string password, string clientId, string verifyUrl, CancellationToken ct = default)
    {
        if (await authContext.Credentials.AnyAsync(c => c.Email == email, ct))
            return RegisterResult.EmailAlreadyExists;

        var credential = CredentialEntity.Create(email, passwordHasher.Hash(password), clientId);
        authContext.Credentials.Add(credential);
        await authContext.SaveChangesAsync(ct);

        await SendEmailVerificationAsync(credential.Id, verifyUrl, ct);
        return RegisterResult.Success;
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var credential = await authContext.Credentials.FindAsync([userId], ct);
        if (credential is null || !passwordHasher.Verify(currentPassword, credential.PasswordHash))
            return false;

        credential.SetPasswordHash(passwordHasher.Hash(newPassword));
        await authContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<string?> LogoutAsync(string? logoutId, CancellationToken ct = default)
    {
        var context = await interaction.GetLogoutContextAsync(logoutId);
        return context?.PostLogoutRedirectUri;
    }

    public async Task SendEmailVerificationAsync(Guid userId, string verifyUrl, CancellationToken ct = default)
    {
        var credential = await authContext.Credentials.FindAsync([userId], ct);
        if (credential is null) return;

        var token = Guid.NewGuid().ToString("N");
        var expires = DateTime.UtcNow.AddHours(24);
        authContext.EmailVerificationTokens.Add(EmailVerificationTokenEntity.Create(userId, token, expires));
        await authContext.SaveChangesAsync(ct);

        await emailSender.SendVerificationAsync(credential.Email, token, verifyUrl, ct);
    }

    public async Task<bool> VerifyEmailAsync(string token, CancellationToken ct = default)
    {
        var tokenEntity = await authContext.EmailVerificationTokens
            .FirstOrDefaultAsync(t => t.Token == token, ct);

        if (tokenEntity is null || !tokenEntity.IsActive)
            return false;

        var credential = await authContext.Credentials.FindAsync([tokenEntity.CredentialId], ct);
        if (credential is null) return false;

        credential.VerifyEmail();
        await authContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task SendPasswordResetAsync(string email, string resetUrl, CancellationToken ct = default)
    {
        var credential = await authContext.Credentials.FirstOrDefaultAsync(c => c.Email == email, ct);
        if (credential is null) return;

        var token = Guid.NewGuid().ToString("N");
        var expires = DateTime.UtcNow.AddHours(1);
        authContext.PasswordResetTokens.Add(PasswordResetTokenEntity.Create(credential.Id, token, expires));
        await authContext.SaveChangesAsync(ct);

        var link = $"{resetUrl}?token={Uri.EscapeDataString(token)}";
        await emailSender.SendEmailAsync(email, "Reset your password",
            $"Click here to reset your password: {link}. This link expires in 1 hour.");
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default)
    {
        var tokenEntity = await authContext.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.Token == token, ct);

        if (tokenEntity is null || !tokenEntity.IsActive)
            return false;

        var credential = await authContext.Credentials.FindAsync([tokenEntity.CredentialId], ct);
        if (credential is null) return false;

        credential.SetPasswordHash(passwordHasher.Hash(newPassword));
        await authContext.SaveChangesAsync(ct);
        return true;
    }
}
