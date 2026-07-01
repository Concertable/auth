using Concertable.Auth.Contracts;
using Concertable.Auth.Services;
using Concertable.Kernel.Auth;
using Microsoft.Extensions.Caching.Memory;
using Refit;

namespace Concertable.Auth.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRemoteProfileClaimsProvider<TApi>(
        this IServiceCollection services, string source, string? baseUrl)
        where TApi : class, IUserClaimsApi
    {
        // Host doesn't run this source service — the provider would never contribute claims.
        if (string.IsNullOrEmpty(baseUrl))
            return services;

        services.AddRefitClient<TApi>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri(baseUrl.TrimEnd('/')))
            .AddHttpMessageHandler(sp => new ServiceTokenHandler(sp.GetRequiredService<ITokenService>(), "user:claims"));

        services.AddScoped<IProfileClaimsProvider>(sp => new RemoteProfileClaimsProvider(
            source,
            sp.GetRequiredService<TApi>(),
            sp.GetRequiredService<IMemoryCache>(),
            sp.GetRequiredService<ILogger<RemoteProfileClaimsProvider>>()));

        return services;
    }
}
