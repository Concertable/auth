using Concertable.Auth.Data.Entities;
using Concertable.Kernel;

namespace Concertable.Auth.Data.Events;

internal sealed record CredentialCreatedDomainEvent(CredentialEntity Credential, string ClientId) : IDomainEvent;
