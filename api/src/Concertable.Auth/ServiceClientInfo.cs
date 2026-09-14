using Concertable.Auth.Contracts;

namespace Concertable.Auth;

/// <summary>
/// Catalog row for a <see cref="ServiceClient"/>: its client id, the configuration key its secret is bound
/// under, and the one scope it is granted. Obtain from <see cref="ServiceClients"/>.
/// </summary>
public readonly record struct ServiceClientInfo(
    ServiceClient Client,
    string Id,
    string SecretConfigKey,
    AuthScope GrantedScope);
