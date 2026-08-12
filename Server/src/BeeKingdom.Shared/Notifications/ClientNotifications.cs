using BeeKingdom.Shared.Enums;
using BeeKingdom.Shared.ValueObjects;
using BeeKingdom.Shared.Versioning;

namespace BeeKingdom.Shared.Notifications;

public sealed record AchievementUnlocked(Guid NotificationId, DateTimeOffset CreatedAtUtc, NotificationSeverity Severity, PlayerId PlayerId, string AchievementId, ContractVersion ContractVersion) : INotification;

public sealed record BuildingDamaged(Guid NotificationId, DateTimeOffset CreatedAtUtc, NotificationSeverity Severity, ColonyId ColonyId, BuildingId BuildingId, double HealthRatio, ContractVersion ContractVersion) : INotification;

public sealed record NewMail(Guid NotificationId, DateTimeOffset CreatedAtUtc, NotificationSeverity Severity, PlayerId PlayerId, string MailId, ContractVersion ContractVersion) : INotification;

public sealed record AllianceInvitation(Guid NotificationId, DateTimeOffset CreatedAtUtc, NotificationSeverity Severity, PlayerId PlayerId, AllianceId AllianceId, ContractVersion ContractVersion) : INotification;
