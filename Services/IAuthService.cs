using System.Security.Claims;

namespace Concertable.Auth.Services;

public interface IAuthService
{
    Task<ClaimsPrincipal?> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<string?> LogoutAsync(string? logoutId, CancellationToken ct = default);
}
