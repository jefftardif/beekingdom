namespace BeeKingdom.Protocol.Requests;

public sealed record PingRequest(string ClientBuild, DateTimeOffset SentAtUtc);
