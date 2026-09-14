using System.Collections.Frozen;
using Concertable.Auth.Contracts;

namespace Concertable.Auth;

/// <summary>The client-credentials service-client roster — the single source for their ids, secret config keys and granted scopes.</summary>
public static class ServiceClients
{
    private static readonly FrozenDictionary<ServiceClient, ServiceClientInfo> ByClient = new[]
    {
        new ServiceClientInfo(ServiceClient.B2B, "concertable-b2b", "ServiceAuth:B2BClientSecret", AuthScope.PaymentWrite),
        new ServiceClientInfo(ServiceClient.Customer, "concertable-customer", "ServiceAuth:CustomerClientSecret", AuthScope.PaymentWrite),
        new ServiceClientInfo(ServiceClient.Auth, "concertable-auth", "ServiceAuth:AuthClientSecret", AuthScope.UserClaims),
    }.ToFrozenDictionary(info => info.Client);

    /// <summary>Every registered service client.</summary>
    public static IReadOnlyCollection<ServiceClientInfo> All => ByClient.Values;

    /// <summary>The catalog row for a known service client.</summary>
    public static ServiceClientInfo Info(this ServiceClient client) => ByClient[client];
}
