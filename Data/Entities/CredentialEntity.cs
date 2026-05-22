using Concertable.Auth.Data.Events;
using Concertable.Shared;

namespace Concertable.Auth.Data.Entities;

internal sealed class CredentialEntity : IGuidEntity, IEventRaiser
{
    private readonly EventRaiser events = new();

    private CredentialEntity() { }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public bool IsEmailVerified { get; private set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => events.DomainEvents;
    public void ClearDomainEvents() => events.Clear();

    public static CredentialEntity Create(string email, string passwordHash, string clientId)
    {
        var entity = new CredentialEntity
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash
        };
        entity.events.Raise(new CredentialCreatedDomainEvent(entity, clientId));
        return entity;
    }

    public static CredentialEntity Seed(Guid id, string email, string passwordHash) =>
        new()
        {
            Id = id,
            Email = email,
            PasswordHash = passwordHash,
            IsEmailVerified = true
        };

    public void VerifyEmail() => IsEmailVerified = true;

    public void SetPasswordHash(string hash) => PasswordHash = hash;
}
