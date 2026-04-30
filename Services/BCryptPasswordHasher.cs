namespace Concertable.Auth.Services;

internal sealed class BCryptPasswordHasher : IPasswordHasher
{
    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
