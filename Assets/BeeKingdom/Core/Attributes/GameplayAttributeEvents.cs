using BeeKingdom.Core.Events;

namespace BeeKingdom.Core.Attributes
{
    public readonly struct AttributeRegistered : IGameplayEvent { public string AttributeId { get; } public AttributeRegistered(string attributeId) { AttributeId = attributeId; } }
    public readonly struct AttributeChanged : IGameplayEvent { public string OwnerId { get; } public string AttributeId { get; } public double Value { get; } public AttributeChanged(string ownerId, string attributeId, double value) { OwnerId = ownerId; AttributeId = attributeId; Value = value; } }
    public readonly struct AttributeRecalculated : IGameplayEvent { public string OwnerId { get; } public string AttributeId { get; } public double Value { get; } public AttributeRecalculated(string ownerId, string attributeId, double value) { OwnerId = ownerId; AttributeId = attributeId; Value = value; } }
    public readonly struct AttributeClamped : IGameplayEvent { public string OwnerId { get; } public string AttributeId { get; } public AttributeClamped(string ownerId, string attributeId) { OwnerId = ownerId; AttributeId = attributeId; } }
    public readonly struct AttributeSnapshotCreated : IGameplayEvent { public string OwnerId { get; } public AttributeSnapshotCreated(string ownerId) { OwnerId = ownerId; } }
    public readonly struct AttributeRestored : IGameplayEvent { public string OwnerId { get; } public AttributeRestored(string ownerId) { OwnerId = ownerId; } }
}
