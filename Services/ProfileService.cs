using System.Security.Claims;
using Concertable.User.Contracts;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;

namespace Concertable.Auth.Services;

internal sealed class ProfileService : IProfileService
{
    private readonly IUserModule userModule;

    public ProfileService(IUserModule userModule)
    {
        this.userModule = userModule;
    }

    public async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        var userId = Guid.Parse(context.Subject.GetSubjectId());

        var creds = await userModule.GetCredentialsByIdAsync(userId);
        if (creds is null) return;

        var claims = new List<Claim>
        {
            new("email", creds.Email),
            new("email_verified", creds.IsEmailVerified ? "true" : "false", ClaimValueTypes.Boolean),
            new("role", creds.Role.ToString())
        };

        context.AddRequestedClaims(claims);
    }

    public async Task IsActiveAsync(IsActiveContext context)
    {
        var userId = Guid.Parse(context.Subject.GetSubjectId());
        var creds = await userModule.GetCredentialsByIdAsync(userId);

        context.IsActive = creds is not null;
    }
}
