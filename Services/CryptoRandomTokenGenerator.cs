using System.Security.Cryptography;

namespace Concertable.Auth.Services;

internal sealed class CryptoRandomTokenGenerator : ITokenGenerator
{
    public string Generate() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
