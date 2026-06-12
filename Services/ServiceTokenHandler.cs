using System.Net.Http.Headers;
using Concertable.Kernel.Auth;

namespace Concertable.Auth.Services;

internal sealed class ServiceTokenHandler : DelegatingHandler
{
    private readonly ITokenService tokenService;
    private readonly string scope;

    public ServiceTokenHandler(ITokenService tokenService, string scope)
    {
        this.tokenService = tokenService;
        this.scope = scope;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", await tokenService.GetTokenAsync(scope, ct));
        return await base.SendAsync(request, ct);
    }
}
