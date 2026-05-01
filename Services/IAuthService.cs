using System.Security.Claims;

namespace Concertable.Auth.Services;

public interface IAuthService
{
    Task<ClaimsPrincipal?> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<string?> LogoutAsync(string? logoutId, CancellationToken ct = default);

    Task SendEmailVerificationAsync(Guid userId, string verifyUrl, CancellationToken ct = default);
    Task<bool> VerifyEmailAsync(string token, CancellationToken ct = default);

    Task SendPasswordResetAsync(string email, string resetUrl, CancellationToken ct = default);
    Task<bool> ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default);
}
