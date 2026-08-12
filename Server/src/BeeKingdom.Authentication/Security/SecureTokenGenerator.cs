using System.Security.Cryptography;
using System.Text;

namespace BeeKingdom.Authentication.Security;

public sealed class SecureTokenGenerator : ITokenGenerator
{
    public string CreateToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    }

    public string HashToken(string token)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
