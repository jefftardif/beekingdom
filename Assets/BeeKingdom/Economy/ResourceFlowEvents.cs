using BeeKingdom.Core.Events;

namespace BeeKingdom.Economy
{
    public readonly struct ResourceProduced : IResourceEvent { public ResourceType Type { get; } public double Amount { get; } public ResourceProduced(ResourceType type, double amount) { Type = type; Amount = amount; } }
    public readonly struct ResourceTransportStarted : IResourceEvent { public ResourceType Type { get; } public double Amount { get; } public ResourceTransportStarted(ResourceType type, double amount) { Type = type; Amount = amount; } }
    public readonly struct ResourceDelivered : IResourceEvent { public ResourceType Type { get; } public double Amount { get; } public ResourceDelivered(ResourceType type, double amount) { Type = type; Amount = amount; } }
    public readonly struct ResourceConsumed : IResourceEvent { public ResourceType Type { get; } public double Amount { get; } public ResourceConsumed(ResourceType type, double amount) { Type = type; Amount = amount; } }
    public readonly struct ResourceStorageFull : IResourceEvent { public string StorageId { get; } public ResourceType Type { get; } public ResourceStorageFull(string storageId, ResourceType type) { StorageId = storageId; Type = type; } }
    public readonly struct ResourceShortage : IResourceEvent { public string StorageId { get; } public ResourceType Type { get; } public double Amount { get; } public ResourceShortage(string storageId, ResourceType type, double amount) { StorageId = storageId; Type = type; Amount = amount; } }
}
