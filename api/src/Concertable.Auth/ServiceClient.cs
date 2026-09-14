namespace Concertable.Auth;

/// <summary>
/// A backend service's client-credentials client. Unlike <see cref="Contracts.InteractiveClient"/>, these
/// never cross the wire and their secrets are Auth's own — so they stay local, not in
/// <c>Concertable.Auth.Contracts</c>. Details via <see cref="ServiceClients"/>.
/// </summary>
public enum ServiceClient
{
    B2B,
    Customer,
    Auth,
}
