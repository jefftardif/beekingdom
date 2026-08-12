namespace BeeKingdom.Authentication.Security;

public interface ITokenGenerator
{
    string CreateToken();
    string HashToken(string token);
}
