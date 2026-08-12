using BeeKingdom.Shared.Contracts;
using BeeKingdom.Shared.Enums;

namespace BeeKingdom.Shared.Notifications;

public interface INotification : IContract
{
    Guid NotificationId { get; }
    DateTimeOffset CreatedAtUtc { get; }
    NotificationSeverity Severity { get; }
}
