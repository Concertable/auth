using System.Security.Claims;
using Concertable.Auth.Data;
using Concertable.Auth.Data.Entities;
using Concertable.Auth.Domain;
using Reunion;
using Concertable.Shared.Email.Application;
using Duende.IdentityServer;
using Duende.IdentityServer.Services;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Auth.Services;

internal sealed class AuthService : IAuthService
{
    private readonly AuthDbContext context;
    private readonly IOutboxUnitOfWorkBehavior outboxBehavior;
    private readonly IPasswordHasher passwordHasher;
    private readonly IIdentityServerInteractionService interaction;
    private readonly IEmailSender emailSender;
    private readonly ITokenGenerator tokenGenerator;
    private readonly TimeProvider timeProvider;

    public AuthService(
        AuthDbContext context,
        IOutboxUnitOfWorkBehavior outboxBehavior,
        IPasswordHasher passwordHasher,
        IIdentityServerInteractionService interaction,
        IEmailSender emailSender,
        ITokenGenerator tokenGenerator,
        TimeProvider timeProvider)
    {
        this.context = context;
        this.outboxBehavior = outboxBehavior;
        this.passwordHasher = passwordHasher;
        this.interaction = interaction;
        this.emailSender = emailSender;
        this.tokenGenerator = tokenGenerator;
        this.timeProvider = timeProvider;
    }

    public async Task<Option<ClaimsPrincipal>> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var credential = await context.Credentials.FirstOrDefaultAsync(c => c.Email == email, ct);
        if (credential is null || !credential.CanAuthenticate(password, passwordHasher))
            return null;

        var claims = new List<Claim> { new("sub", credential.Id.ToString()) };
        var identity = new ClaimsIdentity(claims, IdentityServerConstants.DefaultCookieAuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    public async Task<UnitResult<RegisterError>> RegisterAsync(string email, string password, string clientId, string verifyUrl, CancellationToken ct = default)
    {
        if (await context.Credentials.AnyAsync(c => c.Email == email, ct))
            return new RegisterError.EmailAlreadyExists();

        var credential = CredentialEntity.Create(email, passwordHasher.Hash(password), clientId);
        context.Credentials.Add(credential);
        await context.SaveChangesAsync(ct);

        await SendEmailVerificationAsync(credential.Id, verifyUrl, ct);
        return new Success();
    }

    public async Task<UnitResult<ChangePasswordError>> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var credential = await context.Credentials.FindAsync([userId], ct);
        if (credential is null)
            return new ChangePasswordError.CurrentPasswordIncorrect();

        var result = credential.ChangePassword(currentPassword, newPassword, passwordHasher);
        if (result.IsFailure)
            return result;

        await context.SaveChangesAsync(ct);
        return result;
    }

    public async Task<Option<string>> LogoutAsync(string? logoutId, CancellationToken ct = default)
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

        await outboxBehavior.ExecuteAsync(() =>
            emailSender.SendVerificationAsync(credential.Email, token, verifyUrl, ct), ct);
    }

    public async Task<UnitResult<VerifyEmailError>> VerifyEmailAsync(string token, CancellationToken ct = default)
    {
        var tokenEntity = await context.EmailVerificationTokens
            .FirstOrDefaultAsync(t => t.Token == token, ct);

        if (tokenEntity is null)
            return new VerifyEmailError.InvalidOrExpiredToken();

        var credential = await context.Credentials.FindAsync([tokenEntity.CredentialId], ct);
        if (credential is null)
            return new VerifyEmailError.InvalidOrExpiredToken();

        var result = tokenEntity.Verify(credential, timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
            return result;

        context.EmailVerificationTokens.Remove(tokenEntity);
        await context.SaveChangesAsync(ct);
        return result;
    }

    public async Task SendPasswordResetAsync(string email, string resetUrl, CancellationToken ct = default)
    {
        var credential = await context.Credentials.FirstOrDefaultAsync(c => c.Email == email, ct);
        if (credential is null) return;

        var token = tokenGenerator.Generate();
        var expires = timeProvider.GetUtcNow().UtcDateTime.AddHours(1);
        context.PasswordResetTokens.Add(PasswordResetTokenEntity.Create(credential.Id, token, expires));

        var link = $"{resetUrl}?token={Uri.EscapeDataString(token)}";
        await outboxBehavior.ExecuteAsync(() =>
            emailSender.SendEmailAsync(email, "Reset your password",
                $"Click here to reset your password: {link}. This link expires in 1 hour."), ct);
    }

    public async Task<UnitResult<ResetPasswordError>> ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default)
    {
        var tokenEntity = await context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.Token == token, ct);

        if (tokenEntity is null)
            return new ResetPasswordError.InvalidOrExpiredToken();

        var credential = await context.Credentials.FindAsync([tokenEntity.CredentialId], ct);
        if (credential is null)
            return new ResetPasswordError.InvalidOrExpiredToken();

        var result = tokenEntity.ResetPassword(
            credential,
            newPassword,
            passwordHasher,
            timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
            return result;

        context.PasswordResetTokens.Remove(tokenEntity);
        await context.SaveChangesAsync(ct);
        return result;
    }
}
