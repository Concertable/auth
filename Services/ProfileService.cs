using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using System.Security.Claims;

namespace Concertable.Auth.Services;

public class ProfileService : IProfileService
{
    private readonly UserStore _userStore;

    public ProfileService(UserStore userStore)
    {
        _userStore = userStore;
    }

    public async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        var userId = Guid.Parse(context.Subject.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userStore.FindByIdAsync(userId);

        if (user is null) return;

        context.IssuedClaims.AddRange([
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        ]);
    }

    public async Task IsActiveAsync(IsActiveContext context)
    {
        var userId = Guid.Parse(context.Subject.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userStore.FindByIdAsync(userId);
        context.IsActive = user is not null;
    }
}
