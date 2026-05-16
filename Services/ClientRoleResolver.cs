using Concertable.User.Contracts;
using Duende.IdentityServer.Services;

namespace Concertable.Auth.Services;

internal static class ClientRoleResolver
{
    public static async Task<Role?> ResolveFromReturnUrlAsync(IIdentityServerInteractionService interaction, string? returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl)) return null;
        var context = await interaction.GetAuthorizationContextAsync(returnUrl);
        var clientId = context?.Client?.ClientId;
        return ResolveFromClientId(clientId);
    }

    public static Role? ResolveFromClientId(string? clientId) =>
        clientId is not null && Config.ClientRoleMap.TryGetValue(clientId, out var role)
            ? role
            : null;
}
