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
    private readonly AuthDbContext context;
    private readonly IPasswordHasher passwordHasher;
    private readonly IIdentityServerInteractionService interaction;
    private readonly IEmailSender emailSender;
    private readonly ITokenGenerator tokenGenerator;
    private readonly TimeProvider timeProvider;

    public AuthService(
        AuthDbContext context,
        IPasswordHasher passwordHasher,
        IIdentityServerInteractionService interaction,
        IEmailSender emailSender,
        ITokenGenerator tokenGenerator,
        TimeProvider timeProvider)
    {
        this.context = context;
        this.passwordHasher = passwordHasher;
        this.interaction = interaction;
        this.emailSender = emailSender;
        this.tokenGenerator = tokenGenerator;
        this.timeProvider = timeProvider;
    }

    public async Task<ClaimsPrincipal?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var credential = await context.Credentials.FirstOrDefaultAsync(c => c.Email == email, ct);
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
        if (await context.Credentials.AnyAsync(c => c.Email == email, ct))
            return RegisterResult.EmailAlreadyExists;

        var credential = CredentialEntity.Create(email, passwordHasher.Hash(password), clientId);
        context.Credentials.Add(credential);
        await context.SaveChangesAsync(ct);

        await SendEmailVerificationAsync(credential.Id, verifyUrl, ct);
        return RegisterResult.Success;
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var credential = await context.Credentials.FindAsync([userId], ct);
        if (credential is null || !passwordHasher.Verify(currentPassword, credential.PasswordHash))
            return false;

        credential.SetPasswordHash(passwordHasher.Hash(newPassword));
        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<string?> LogoutAsync(string? logoutId, CancellationToken ct = default)
    {
        var logoutContext = await interaction.GetLogoutContextAsync(logoutId);
        return logoutContext?.PostLogoutRedirectUri;
    }

    public async Task SendEmailVerificationAsync(Guid userId, string verifyUrl, CancellationToken ct = default)
    {
        var credential = await context.Credentials.FindAsync([userId], ct);
        if (credential is null) return;

        var token = tokenGenerator.Generate();
        var expires = timeProvider.GetUtcNow().UtcDateTime.AddHours(24);
        context.EmailVerificationTokens.Add(EmailVerificationTokenEntity.Create(userId, token, expires));
        await context.SaveChangesAsync(ct);

        await emailSender.SendVerificationAsync(credential.Email, token, verifyUrl, ct);
    }

    public async Task<bool> VerifyEmailAsync(string token, CancellationToken ct = default)
    {
        var tokenEntity = await context.EmailVerificationTokens
            .FirstOrDefaultAsync(t => t.Token == token, ct);

        if (tokenEntity is null || !tokenEntity.IsActive(timeProvider.GetUtcNow().UtcDateTime))
            return false;

        var credential = await context.Credentials.FindAsync([tokenEntity.CredentialId], ct);
        if (credential is null) return false;

        credential.VerifyEmail();
        context.EmailVerificationTokens.Remove(tokenEntity);
        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task SendPasswordResetAsync(string email, string resetUrl, CancellationToken ct = default)
    {
        var credential = await context.Credentials.FirstOrDefaultAsync(c => c.Email == email, ct);
        if (credential is null) return;

        var token = tokenGenerator.Generate();
        var expires = timeProvider.GetUtcNow().UtcDateTime.AddHours(1);
        context.PasswordResetTokens.Add(PasswordResetTokenEntity.Create(credential.Id, token, expires));
        await context.SaveChangesAsync(ct);

        var link = $"{resetUrl}?token={Uri.EscapeDataString(token)}";
        await emailSender.SendEmailAsync(email, "Reset your password",
            $"Click here to reset your password: {link}. This link expires in 1 hour.");
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default)
    {
        var tokenEntity = await context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.Token == token, ct);

        if (tokenEntity is null || !tokenEntity.IsActive(timeProvider.GetUtcNow().UtcDateTime))
            return false;

        var credential = await context.Credentials.FindAsync([tokenEntity.CredentialId], ct);
        if (credential is null) return false;

        credential.SetPasswordHash(passwordHasher.Hash(newPassword));
        context.PasswordResetTokens.Remove(tokenEntity);
        await context.SaveChangesAsync(ct);
        return true;
    }
}
