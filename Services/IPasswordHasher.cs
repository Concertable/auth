namespace Concertable.Auth.Services;

public interface IPasswordHasher
{
    bool Verify(string password, string hash);
    string Hash(string password);
}
