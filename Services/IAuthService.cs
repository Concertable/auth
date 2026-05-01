using System.Security.Claims;

namespace Concertable.Auth.Services;

public interface IAuthService
{
    Task<ClaimsPrincipal?> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<string?> LogoutAsync(string? logoutId, CancellationToken ct = default);

    Task<RegisterResult> RegisterAsync(string email, string password, Concertable.User.Contracts.Role role, string verifyUrl, CancellationToken ct = default);
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default);

    Task SendEmailVerificationAsync(Guid userId, string verifyUrl, CancellationToken ct = default);
    Task<bool> VerifyEmailAsync(string token, CancellationToken ct = default);

    Task SendPasswordResetAsync(string email, string resetUrl, CancellationToken ct = default);
    Task<bool> ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default);
}

public enum RegisterResult { Success, EmailAlreadyExists, RoleNotAllowed, InvalidRole }
