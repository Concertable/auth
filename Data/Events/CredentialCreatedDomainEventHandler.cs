using Concertable.Auth.Contracts.Events;
using Concertable.Messaging;
using Concertable.Shared;

namespace Concertable.Auth.Data.Events;

internal sealed class CredentialCreatedDomainEventHandler : IPreCommitDomainEventHandler<CredentialCreatedDomainEvent>
{
    private readonly IBus bus;

    public CredentialCreatedDomainEventHandler(IBus bus)
    {
        this.bus = bus;
    }

    public Task HandleAsync(CredentialCreatedDomainEvent e, CancellationToken ct = default) =>
        bus.PublishAsync(new CredentialRegisteredEvent(e.Credential.Id, e.Credential.Email, e.ClientId), ct);
}
