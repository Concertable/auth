using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;

namespace Concertable.Auth.Services;

internal sealed class ResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
{
    private readonly IAuthService authService;

    public ResourceOwnerPasswordValidator(IAuthService authService)
    {
        this.authService = authService;
    }

    public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
    {
        var role = ClientRoleResolver.ResolveFromClientId(context.Request.Client.ClientId);
        var principal = await authService.LoginAsync(context.UserName, context.Password, role);
        if (principal is null)
        {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Invalid credentials");
            return;
        }

        var sub = principal.FindFirst("sub")!.Value;
        context.Result = new GrantValidationResult(sub, "password");
    }
}
