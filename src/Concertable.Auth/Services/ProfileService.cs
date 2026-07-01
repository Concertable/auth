using System.Security.Claims;
using Concertable.Auth.Contracts;
using Concertable.Auth.Data;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;

namespace Concertable.Auth.Services;

internal sealed class ProfileService : IProfileService
{
    private readonly IEnumerable<IProfileClaimsProvider> claimsProviders;
    private readonly AuthDbContext authContext;

    public ProfileService(IEnumerable<IProfileClaimsProvider> claimsProviders, AuthDbContext authContext)
    {
        this.claimsProviders = claimsProviders;
        this.authContext = authContext;
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
        var credential = await authContext.Credentials.FindAsync([userId]);
        context.IsActive = credential is not null;
    }
}
