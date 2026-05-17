namespace Concertable.Auth.Services;

public interface IClientRoleResolver
{
    Task<RoleResolution> ResolveRoleAsync(string? returnUrl, string? selectedRole);
}
