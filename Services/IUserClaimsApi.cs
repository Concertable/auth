using Refit;

namespace Concertable.Auth.Services;

/// <summary>
/// Wire contract for the <c>/internal/users/{sub}/claims</c> endpoint each data service exposes.
/// Bearer token is attached by <see cref="ServiceTokenHandler"/>.
/// </summary>
internal interface IUserClaimsApi
{
    /// <summary>Returns the service's claims for the subject; empty if the subject is unknown to it.</summary>
    [Get("/internal/users/{subjectId}/claims")]
    Task<List<ClaimDto>> GetClaimsAsync(Guid subjectId);
}

/// <summary>B2B claims endpoint. Refit configures clients per interface type, hence one marker per source service.</summary>
internal interface IB2BUserClaimsApi : IUserClaimsApi;

/// <summary>Customer claims endpoint. See <see cref="IB2BUserClaimsApi"/>.</summary>
internal interface ICustomerUserClaimsApi : IUserClaimsApi;

/// <summary>Mirrors the <c>ClaimDto</c> serialized by each service's <c>UserClaimsController</c>.</summary>
internal sealed record ClaimDto(string Type, string Value);
