namespace Concertable.Auth.Contracts;

/// <summary>A protected API Auth issues tokens for — a resource server / JWT audience. Details via <see cref="AuthResources"/>.</summary>
public enum AuthResource
{
    B2B,
    Customer,
    Search,
    Payment,
}
