using System.Security.Claims;
using Concertable.Auth.Contracts;
using Concertable.User.Contracts;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;

namespace Concertable.Auth.Services;

internal sealed class ProfileService : IProfileService
{
    private readonly IEnumerable<IProfileClaimsProvider> claimsProviders;
    private readonly IUserModule userModule;

    public ProfileService(IEnumerable<IProfileClaimsProvider> claimsProviders, IUserModule userModule)
    {
        this.claimsProviders = claimsProviders;
        this.userModule = userModule;
    }

    public async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        var userId = Guid.Parse(context.Subject.GetSubjectId());
        var claims = new List<Claim>();
        foreach (var provider in claimsProviders)
            claims.AddRange(await provider.GetClaimsAsync(userId));
        context.AddRequestedClaims(claims);
    }

    public async Task IsActiveAsync(IsActiveContext context)
    {
        var userId = Guid.Parse(context.Subject.GetSubjectId());
        var creds = await userModule.GetCredentialsByIdAsync(userId);
        context.IsActive = creds is not null;
    }
}
