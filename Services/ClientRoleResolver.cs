using System.Collections.Frozen;
using Concertable.User.Contracts;
using Duende.IdentityServer.Services;

namespace Concertable.Auth.Services;

internal sealed class ClientRoleResolver : IClientRoleResolver
{
    private static readonly FrozenDictionary<string, Role[]> clientRoleMap = new Dictionary<string, Role[]>
    {
        ["customer-web"]    = [Role.Customer],
        ["customer-mobile"] = [Role.Customer],
        ["venue-web"]       = [Role.VenueManager],
        ["artist-web"]      = [Role.ArtistManager],
        ["business-mobile"] = [Role.VenueManager, Role.ArtistManager],
    }.ToFrozenDictionary();

    private readonly IIdentityServerInteractionService interaction;

    public ClientRoleResolver(IIdentityServerInteractionService interaction)
        => this.interaction = interaction;

    public async Task<RoleResolution> ResolveRoleAsync(string? returnUrl, string? selectedRole)
    {
        if (string.IsNullOrEmpty(returnUrl)) return new RoleResolution.UnknownClient();
        var context = await interaction.GetAuthorizationContextAsync(returnUrl);
        var clientId = context?.Client?.ClientId;
        if (clientId is null) return new RoleResolution.UnknownClient();

        if (!clientRoleMap.TryGetValue(clientId, out var roles))
            return new RoleResolution.UnknownClient();

        if (roles.Length == 1)
            return new RoleResolution.Resolved(roles[0]);

        return Enum.TryParse<Role>(selectedRole, out var selected) && roles.Contains(selected)
            ? new RoleResolution.Resolved(selected)
            : new RoleResolution.InvalidSelection();
    }

    public async Task<IReadOnlyList<Role>> GetAllowedRolesAsync(string? returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl)) return [];
        var context = await interaction.GetAuthorizationContextAsync(returnUrl);
        var clientId = context?.Client?.ClientId;
        if (clientId is null) return [];
        return clientRoleMap.TryGetValue(clientId, out var roles) ? roles : [];
    }
}
