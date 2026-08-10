using Concertable.Auth.Data.Entities;
using Concertable.Auth.Domain;
using Concertable.Kernel;

namespace Concertable.Auth.UnitTests;

public sealed class TokenEntityTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);
    private readonly IPasswordHasher passwordHasher = new TestPasswordHasher();

    [Fact]
    public void Verify_ExpiredToken_ReturnsFailureWithoutVerifyingCredential()
    {
        var credential = CreateCredential();
        var token = EmailVerificationTokenEntity.Create(credential.Id, "token", UtcNow);

        var result = token.Verify(credential, UtcNow);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<VerifyEmailError.InvalidOrExpiredToken>(error);
        Assert.False(credential.IsEmailVerified);
    }

    [Fact]
    public void Verify_ActiveTokenForCredential_ReturnsSuccessAndVerifiesCredential()
    {
        var credential = CreateCredential();
        var token = EmailVerificationTokenEntity.Create(credential.Id, "token", UtcNow.AddMinutes(1));

        var result = token.Verify(credential, UtcNow);

        Assert.True(result.IsSuccess);
        Assert.True(credential.IsEmailVerified);
    }

    [Fact]
    public void Verify_TokenForAnotherCredential_Throws()
    {
        var credential = CreateCredential();
        var token = EmailVerificationTokenEntity.Create(Guid.NewGuid(), "token", UtcNow.AddMinutes(1));

        Assert.Throws<DomainException>(() => token.Verify(credential, UtcNow));
    }

    [Fact]
    public void ResetPassword_ExpiredToken_ReturnsFailureWithoutChangingPassword()
    {
        var credential = CreateCredential();
        var token = PasswordResetTokenEntity.Create(credential.Id, "token", UtcNow);

        var result = token.ResetPassword(credential, "new", passwordHasher, UtcNow);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<ResetPasswordError.InvalidOrExpiredToken>(error);
        Assert.True(passwordHasher.Verify("old", credential.PasswordHash));
    }

    [Fact]
    public void ResetPassword_ActiveTokenForCredential_ReturnsSuccessAndChangesPassword()
    {
        var credential = CreateCredential();
        var token = PasswordResetTokenEntity.Create(credential.Id, "token", UtcNow.AddMinutes(1));

        var result = token.ResetPassword(credential, "new", passwordHasher, UtcNow);

        Assert.True(result.IsSuccess);
        Assert.True(passwordHasher.Verify("new", credential.PasswordHash));
    }

    [Fact]
    public void ResetPassword_TokenForAnotherCredential_Throws()
    {
        var credential = CreateCredential();
        var token = PasswordResetTokenEntity.Create(Guid.NewGuid(), "token", UtcNow.AddMinutes(1));

        Assert.Throws<DomainException>(() =>
            token.ResetPassword(credential, "new", passwordHasher, UtcNow));
    }

    private CredentialEntity CreateCredential() =>
        CredentialEntity.Create("test@example.com", passwordHasher.Hash("old"), "customer-web");

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public bool Verify(string password, string hash) => hash == Hash(password);

        public string Hash(string password) => $"hashed:{password}";
    }
}
