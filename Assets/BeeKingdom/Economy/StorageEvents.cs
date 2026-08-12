using BeeKingdom.Core.Events;

namespace BeeKingdom.Economy
{
    public readonly struct StorageCellCreated : IResourceEvent { public string CellId { get; } public StorageCellCreated(string cellId) { CellId = cellId; } }
    public readonly struct StorageCellFilled : IResourceEvent { public string CellId { get; } public StorageCellFilled(string cellId) { CellId = cellId; } }
    public readonly struct StorageCellEmptied : IResourceEvent { public string CellId { get; } public StorageCellEmptied(string cellId) { CellId = cellId; } }
    public readonly struct StorageReservationCreated : IResourceEvent { public string ReservationId { get; } public StorageReservationCreated(string reservationId) { ReservationId = reservationId; } }
    public readonly struct StorageReservationReleased : IResourceEvent { public string ReservationId { get; } public StorageReservationReleased(string reservationId) { ReservationId = reservationId; } }
    public readonly struct StorageClusterFull : IResourceEvent { public string ClusterId { get; } public StorageClusterFull(string clusterId) { ClusterId = clusterId; } }
}
