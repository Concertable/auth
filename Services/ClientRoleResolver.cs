using Concertable.User.Contracts;
using Duende.IdentityServer.Services;

namespace Concertable.Auth.Services;

internal sealed class ClientRoleResolver : IClientRoleResolver
{
    private static readonly IReadOnlyDictionary<string, Role[]> clientRoleMap = new Dictionary<string, Role[]>
    {
        ["customer-web"]    = [Role.Customer],
        ["customer-mobile"] = [Role.Customer],
        ["business-web"]    = [Role.VenueManager, Role.ArtistManager],
        ["business-mobile"] = [Role.VenueManager, Role.ArtistManager],
    };

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
}
