using Concertable.Auth.Data.Entities;
using static Concertable.Seeding.Extensions.EntityReflectionExtensions;

namespace Concertable.Auth.Data.Factories;

internal static class CredentialFactory
{
    public static CredentialEntity Create(Guid id, string email, string passwordHash, string clientId)
    {
        var credential = CredentialEntity.Create(email, passwordHash, clientId)
            .With(nameof(CredentialEntity.Id), id);
        credential.VerifyEmail();
        return credential;
    }
}
