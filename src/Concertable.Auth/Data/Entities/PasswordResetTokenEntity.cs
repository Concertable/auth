using Concertable.Auth.Domain;
using Concertable.Kernel;
using Reunion;

namespace Concertable.Auth.Data.Entities;

internal sealed class PasswordResetTokenEntity : IIdEntity
{
    private PasswordResetTokenEntity() { }

    public int Id { get; private set; }
    public Guid CredentialId { get; private set; }
    public string Token { get; private set; } = null!;
    public DateTime Expires { get; private set; }

    public UnitResult<ResetPasswordError> ResetPassword(
        CredentialEntity credential,
        string newPassword,
        IPasswordHasher passwordHasher,
        DateTime utcNow)
    {
        if (credential.Id != CredentialId)
            throw new DomainException("Password reset token belongs to another credential.");

        if (utcNow >= Expires)
            return new ResetPasswordError.InvalidOrExpiredToken();

        credential.ResetPassword(newPassword, passwordHasher);
        return new Success();
    }

    public static PasswordResetTokenEntity Create(Guid credentialId, string token, DateTime expires) => new()
    {
        CredentialId = credentialId,
        Token = token,
        Expires = expires
    };
}
