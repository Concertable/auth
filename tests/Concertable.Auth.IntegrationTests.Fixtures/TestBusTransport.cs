using Concertable.Messaging.Contracts;

namespace Concertable.Auth.IntegrationTests.Fixtures;

internal sealed class TestBusTransport : IBusTransport
{
    public Task PublishAsync<TEvent>(
        TEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default)
        where TEvent : IIntegrationEvent => Task.CompletedTask;

    public Task SendAsync<TCommand>(
        TCommand command,
        MessageEnvelope envelope,
        CancellationToken ct = default)
        where TCommand : IIntegrationCommand => Task.CompletedTask;
}
