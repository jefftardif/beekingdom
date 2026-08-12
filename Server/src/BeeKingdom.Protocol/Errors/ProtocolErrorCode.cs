namespace BeeKingdom.Protocol.Errors;

public enum ProtocolErrorCode
{
    None = 0,
    InvalidMessage = 1,
    UnsupportedVersion = 2,
    Unauthorized = 3,
    ValidationError = 4,
    ServerError = 5,
    Timeout = 6,
    RateLimited = 7
}
