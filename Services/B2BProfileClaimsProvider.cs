using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Concertable.Auth.Contracts;
using Concertable.Kernel.Auth;
using Microsoft.Extensions.Caching.Memory;

namespace Concertable.Auth.Services;

internal sealed class B2BProfileClaimsProvider : IProfileClaimsProvider
{
    private readonly ITokenService tokenService;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IMemoryCache cache;
    private readonly IConfiguration configuration;

    public B2BProfileClaimsProvider(
        ITokenService tokenService,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IConfiguration configuration)
    {
        this.tokenService = tokenService;
        this.httpClientFactory = httpClientFactory;
        this.cache = cache;
        this.configuration = configuration;
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
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await client.GetAsync($"{b2bUrl}/internal/users/{subjectId}/claims");
            if (!response.IsSuccessStatusCode)
                return [];

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var claims = doc.RootElement.EnumerateArray()
                .Select(e => new Claim(e.GetProperty("type").GetString()!, e.GetProperty("value").GetString()!))
                .ToList();

            cache.Set(cacheKey, (IEnumerable<Claim>)claims, TimeSpan.FromMinutes(5));
            return claims;
        }
        catch
        {
            return [];
        }
    }
}
