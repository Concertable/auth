using Concertable.User.Contracts;

namespace Concertable.Auth.Services;

public interface IClientRoleResolver
{
    Task<RoleResolution> ResolveRoleAsync(string? returnUrl, string? selectedRole);
    Task<IReadOnlyList<Role>> GetAllowedRolesAsync(string? returnUrl);
}
