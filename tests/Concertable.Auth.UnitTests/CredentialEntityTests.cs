using Concertable.Auth.Data.Entities;
using Concertable.Auth.Domain;

namespace Concertable.Auth.UnitTests;

public sealed class CredentialEntityTests
{
    private const string Password = "Password123!";
    private const string NewPassword = "NewPassword123!";
    private readonly IPasswordHasher passwordHasher = new TestPasswordHasher();

    [Fact]
    public void CanAuthenticate_RequiresVerifiedCredentialAndMatchingPassword()
    {
        var credential = CreateCredential();

        Assert.False(credential.CanAuthenticate(Password, passwordHasher));

        credential.VerifyEmail();

        Assert.True(credential.CanAuthenticate(Password, passwordHasher));
        Assert.False(credential.CanAuthenticate("wrong", passwordHasher));
    }

    [Fact]
    public void ChangePassword_IncorrectCurrentPassword_ReturnsFailureWithoutMutation()
    {
        var credential = CreateCredential();

        var result = credential.ChangePassword("wrong", NewPassword, passwordHasher);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<ChangePasswordError.CurrentPasswordIncorrect>(error);
        Assert.True(passwordHasher.Verify(Password, credential.PasswordHash));
    }

    [Fact]
    public void ChangePassword_MatchingCurrentPassword_ReturnsSuccessAndMutatesHash()
    {
        var credential = CreateCredential();

        var result = credential.ChangePassword(Password, NewPassword, passwordHasher);

        Assert.True(result.IsSuccess);
        Assert.True(passwordHasher.Verify(NewPassword, credential.PasswordHash));
    }

    private CredentialEntity CreateCredential() =>
        CredentialEntity.Create("test@example.com", passwordHasher.Hash(Password), "customer-web");

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public bool Verify(string password, string hash) => hash == Hash(password);

        public string Hash(string password) => $"hashed:{password}";
    }
}
