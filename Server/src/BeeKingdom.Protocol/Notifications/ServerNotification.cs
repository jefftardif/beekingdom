namespace BeeKingdom.Protocol.Notifications;

public sealed record ServerNotification(string Code, string Message, DateTimeOffset CreatedAtUtc);
