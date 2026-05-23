using Concertable.Messaging.Contracts;

namespace Concertable.Auth.Contracts.Events;

public record CredentialRegisteredEvent(Guid UserId, string Email, string ClientId) : IIntegrationEvent;
