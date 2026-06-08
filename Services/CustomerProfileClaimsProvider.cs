using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Concertable.Auth.Contracts;
using Concertable.Kernel.Auth;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Concertable.Auth.Services;

internal sealed class CustomerProfileClaimsProvider : IProfileClaimsProvider
{
    private readonly ITokenService tokenService;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IMemoryCache cache;
    private readonly IConfiguration configuration;
    private readonly ILogger<CustomerProfileClaimsProvider> logger;

    public CustomerProfileClaimsProvider(
        ITokenService tokenService,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<CustomerProfileClaimsProvider> logger)
    {
        this.tokenService = tokenService;
        this.httpClientFactory = httpClientFactory;
        this.cache = cache;
        this.configuration = configuration;
        this.logger = logger;
    }

    public async Task<IEnumerable<Claim>> GetClaimsAsync(Guid subjectId)
    {
        var cacheKey = $"customer-claims:{subjectId}";
        if (cache.TryGetValue(cacheKey, out IEnumerable<Claim>? cached) && cached is not null)
            return cached;

        try
        {
            var token = await tokenService.GetTokenAsync("user:claims");
            var customerUrl = configuration["Services:CustomerApiUrl"]?.TrimEnd('/') ?? "";
            var requestUrl = $"{customerUrl}/internal/users/{subjectId}/claims";
            logger.CustomerClaimsRequested(subjectId, requestUrl);

            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await client.GetAsync(requestUrl);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                logger.CustomerClaimsNonSuccess(subjectId, (int)response.StatusCode, body);
                return [];
            }

            using var doc = JsonDocument.Parse(body);
            var claims = doc.RootElement.EnumerateArray()
                .Select(e => new Claim(e.GetProperty("type").GetString()!, e.GetProperty("value").GetString()!))
                .ToList();

            logger.CustomerClaimsReceived(subjectId, (int)response.StatusCode, claims.Count);
            cache.Set(cacheKey, (IEnumerable<Claim>)claims, TimeSpan.FromMinutes(5));
            return claims;
        }
        catch (Exception ex)
        {
            logger.CustomerClaimsFailed(ex, subjectId);
            return [];
        }
    }
}
