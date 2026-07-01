namespace Concertable.Auth.Services;

internal sealed class BCryptPasswordHasher : IPasswordHasher
{
    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password);
}
