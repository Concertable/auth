using Concertable.Auth.Domain;
using Concertable.Kernel;
using Reunion;

namespace Concertable.Auth.Data.Entities;

internal sealed class EmailVerificationTokenEntity : IIdEntity
{
    private EmailVerificationTokenEntity() { }

    public int Id { get; private set; }
    public Guid CredentialId { get; private set; }
    public string Token { get; private set; } = null!;
    public DateTime Expires { get; private set; }

    public UnitResult<VerifyEmailError> Verify(CredentialEntity credential, DateTime utcNow)
    {
        if (credential.Id != CredentialId)
            throw new DomainException("Email verification token belongs to another credential.");

        if (utcNow >= Expires)
            return new VerifyEmailError.InvalidOrExpiredToken();

        credential.VerifyEmail();
        return new Success();
    }

    public static EmailVerificationTokenEntity Create(Guid credentialId, string token, DateTime expires) => new()
    {
        CredentialId = credentialId,
        Token = token,
        Expires = expires
    };
}
