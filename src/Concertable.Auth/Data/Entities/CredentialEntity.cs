using Concertable.Auth.Data.Events;
using Concertable.Auth.Domain;
using Concertable.Kernel;
using Reunion;

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

    public bool CanAuthenticate(string password, IPasswordHasher passwordHasher) =>
        IsEmailVerified && passwordHasher.Verify(password, PasswordHash);

    public UnitResult<ChangePasswordError> ChangePassword(
        string currentPassword,
        string newPassword,
        IPasswordHasher passwordHasher)
    {
        if (!passwordHasher.Verify(currentPassword, PasswordHash))
            return new ChangePasswordError.CurrentPasswordIncorrect();

        PasswordHash = passwordHasher.Hash(newPassword);
        return new Success();
    }

    public void VerifyEmail() => IsEmailVerified = true;

    public void ResetPassword(string password, IPasswordHasher passwordHasher) =>
        PasswordHash = passwordHasher.Hash(password);
}
