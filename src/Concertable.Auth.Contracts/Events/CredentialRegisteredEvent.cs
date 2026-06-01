using Concertable.Messaging.Contracts;

namespace Concertable.Auth.Contracts.Events;

[MessageType("concertable.auth.credential-registered.v1")]
public sealed record CredentialRegisteredEvent(Guid UserId, string Email, string ClientId) : IIntegrationEvent;
