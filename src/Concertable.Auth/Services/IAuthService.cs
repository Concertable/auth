using System.Security.Claims;
using Concertable.Kernel.Functional;

namespace Concertable.Auth.Services;

public interface IAuthService
{
    Task<Option<ClaimsPrincipal>> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<Option<string>> LogoutAsync(string? logoutId, CancellationToken ct = default);

    Task<UnitResult<RegisterError>> RegisterAsync(string email, string password, string clientId, string verifyUrl, CancellationToken ct = default);
    Task<UnitResult<ChangePasswordError>> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default);

    Task SendEmailVerificationAsync(Guid userId, string verifyUrl, CancellationToken ct = default);
    Task<UnitResult<VerifyEmailError>> VerifyEmailAsync(string token, CancellationToken ct = default);

    Task SendPasswordResetAsync(string email, string resetUrl, CancellationToken ct = default);
    Task<UnitResult<ResetPasswordError>> ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default);
}
