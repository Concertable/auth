using System.Security.Claims;
using Concertable.Auth.Contracts;
using Concertable.Auth.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Auth.Services;

internal sealed class LocalProfileClaimsProvider : IProfileClaimsProvider
{
    private readonly AuthDbContext context;

    public LocalProfileClaimsProvider(AuthDbContext context)
    {
        this.context = context;
    }

    public async Task<IEnumerable<Claim>> GetClaimsAsync(Guid subjectId)
    {
        var credential = await context.Credentials
            .Where(c => c.Id == subjectId)
            .Select(c => new { c.Email, c.IsEmailVerified })
            .FirstOrDefaultAsync();

        if (credential is null)
            return [];

        return
        [
            new Claim("email", credential.Email),
            new Claim("email_verified", credential.IsEmailVerified.ToString().ToLower())
        ];
    }
}
