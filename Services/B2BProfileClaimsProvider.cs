using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Concertable.Auth.Contracts;
using Concertable.Kernel.Auth;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Concertable.Auth.Services;

internal sealed class B2BProfileClaimsProvider : IProfileClaimsProvider
{
    private readonly ITokenService tokenService;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IMemoryCache cache;
    private readonly IConfiguration configuration;
    private readonly ILogger<B2BProfileClaimsProvider> logger;

    public B2BProfileClaimsProvider(
        ITokenService tokenService,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<B2BProfileClaimsProvider> logger)
    {
        this.tokenService = tokenService;
        this.httpClientFactory = httpClientFactory;
        this.cache = cache;
        this.configuration = configuration;
        this.logger = logger;
    }

    public async Task<IEnumerable<Claim>> GetClaimsAsync(Guid subjectId)
    {
        var cacheKey = $"b2b-claims:{subjectId}";
        if (cache.TryGetValue(cacheKey, out IEnumerable<Claim>? cached) && cached is not null)
            return cached;

        try
        {
            var token = await tokenService.GetTokenAsync("user:claims");
            var b2bUrl = configuration["Services:B2BApiUrl"]?.TrimEnd('/') ?? "";
            var requestUrl = $"{b2bUrl}/internal/users/{subjectId}/claims";
            logger.B2BClaimsRequested(subjectId, requestUrl);

            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await client.GetAsync(requestUrl);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                logger.B2BClaimsNonSuccess(subjectId, (int)response.StatusCode, body);
                return [];
            }

            using var doc = JsonDocument.Parse(body);
            var claims = doc.RootElement.EnumerateArray()
                .Select(e => new Claim(e.GetProperty("type").GetString()!, e.GetProperty("value").GetString()!))
                .ToList();

            logger.B2BClaimsReceived(subjectId, (int)response.StatusCode, claims.Count);
            cache.Set(cacheKey, (IEnumerable<Claim>)claims, TimeSpan.FromMinutes(5));
            return claims;
        }
        catch (Exception ex)
        {
            logger.B2BClaimsFailed(ex, subjectId);
            return [];
        }
    }
}
