using Concertable.Shared;

namespace Concertable.Auth.Data.Entities;

internal sealed class PasswordResetTokenEntity : IIdEntity
{
    private PasswordResetTokenEntity() { }

    public int Id { get; private set; }
    public Guid CredentialId { get; private set; }
    public string Token { get; private set; } = null!;
    public DateTime Expires { get; private set; }

    public bool IsActive => DateTime.UtcNow < Expires;

    public static PasswordResetTokenEntity Create(Guid credentialId, string token, DateTime expires) => new()
    {
        CredentialId = credentialId,
        Token = token,
        Expires = expires
    };
}
