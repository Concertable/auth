using Concertable.Kernel;

namespace Concertable.Auth.Data.Entities;

internal sealed class EmailVerificationTokenEntity : IIdEntity
{
    private EmailVerificationTokenEntity() { }

    public int Id { get; private set; }
    public Guid CredentialId { get; private set; }
    public string Token { get; private set; } = null!;
    public DateTime Expires { get; private set; }

    public bool IsActive => DateTime.UtcNow < Expires;

    public static EmailVerificationTokenEntity Create(Guid credentialId, string token, DateTime expires) => new()
    {
        CredentialId = credentialId,
        Token = token,
        Expires = expires
    };
}
