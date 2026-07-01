using System.Security.Claims;
using Concertable.Auth.Contracts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Refit;

namespace Concertable.Auth.Services;

/// <summary>
/// Fetches a subject's claims from another service's <c>/internal/users/{sub}/claims</c> endpoint.
/// Fail-open: any failure yields no claims from that source rather than blocking token issuance.
/// </summary>
internal sealed class RemoteProfileClaimsProvider : IProfileClaimsProvider
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly string source;
    private readonly IUserClaimsApi api;
    private readonly IMemoryCache cache;
    private readonly ILogger<RemoteProfileClaimsProvider> logger;

    public RemoteProfileClaimsProvider(
        string source,
        IUserClaimsApi api,
        IMemoryCache cache,
        ILogger<RemoteProfileClaimsProvider> logger)
    {
        this.source = source;
        this.api = api;
        this.cache = cache;
        this.logger = logger;
    }

    public async Task<IEnumerable<Claim>> GetClaimsAsync(Guid subjectId)
    {
        var cacheKey = $"{source}-claims:{subjectId}";
        if (cache.TryGetValue(cacheKey, out Claim[]? cached) && cached is not null)
            return cached;

        try
        {
            logger.RemoteClaimsRequested(source, subjectId);
            var claims = (await api.GetClaimsAsync(subjectId))
                .Select(c => new Claim(c.Type, c.Value))
                .ToArray();

            logger.RemoteClaimsReceived(source, subjectId, claims.Length);
            cache.Set(cacheKey, claims, CacheDuration);
            return claims;
        }
        catch (ApiException ex)
        {
            logger.RemoteClaimsNonSuccess(source, subjectId, (int)ex.StatusCode, ex.Content ?? string.Empty);
            return [];
        }
        catch (Exception ex)
        {
            logger.RemoteClaimsFailed(ex, source, subjectId);
            return [];
        }
    }
}
