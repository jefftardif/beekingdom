namespace BeeKingdom.Protocol.Requests;

public sealed record RuntimeHandshakeRequest(
    string ClientBuild,
    string ClientEnvironment,
    int SupportedProtocolMajor,
    int SupportedProtocolMinor);
