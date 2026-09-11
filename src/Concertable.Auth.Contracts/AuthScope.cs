namespace Concertable.Auth.Contracts;

/// <summary>An OAuth scope Auth issues. Wire value via <see cref="AuthScopes.Id"/>.</summary>
public enum AuthScope
{
    B2BApi,
    CustomerApi,
    SearchApi,
    PaymentWrite,
    UserClaims,
}
