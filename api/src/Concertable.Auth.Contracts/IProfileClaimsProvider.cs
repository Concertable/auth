using System.Security.Claims;

namespace Concertable.Auth.Contracts;

public interface IProfileClaimsProvider
{
    Task<IEnumerable<Claim>> GetClaimsAsync(Guid subjectId);
}
