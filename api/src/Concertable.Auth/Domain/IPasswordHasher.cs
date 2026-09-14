namespace Concertable.Auth.Domain;

public interface IPasswordHasher
{
    bool Verify(string password, string hash);
    string Hash(string password);
}
