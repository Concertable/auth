using Duende.IdentityServer;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.ResponseHandling;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Validation;
using Microsoft.Extensions.Logging;

namespace Concertable.Auth.Services;

internal sealed class RoleEnforcingInteractionResponseGenerator : AuthorizeInteractionResponseGenerator
{
    private readonly IClientRoleResolver clientRoleResolver;

    public RoleEnforcingInteractionResponseGenerator(
        IClientRoleResolver clientRoleResolver,
        IdentityServerOptions options,
        IClock clock,
        ILogger<AuthorizeInteractionResponseGenerator> logger,
        IConsentService consent,
        IProfileService profile)
        : base(options, clock, logger, consent, profile)
    {
        this.clientRoleResolver = clientRoleResolver;
    }

    protected override async Task<InteractionResponse> ProcessLoginAsync(ValidatedAuthorizeRequest request)
    {
        var result = await base.ProcessLoginAsync(request);
        if (result.IsLogin) return result;

        if (request.Subject?.Identity?.IsAuthenticated != true) return result;

        var requiredRole = clientRoleResolver.GetRequiredRoleForClient(request.Client.ClientId);
        if (requiredRole is null) return result;

        var userRole = request.Subject.FindFirst("role")?.Value;
        if (userRole is null || userRole != requiredRole.ToString())
            return new InteractionResponse { IsLogin = true };

        return result;
    }
}
