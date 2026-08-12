using System.Collections.Generic;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Economy
{
    public sealed class HiveInventoryManager
    {
        private readonly StorageGrid grid = new StorageGrid();
        private readonly Dictionary<string, StorageCluster> clusters = new Dictionary<string, StorageCluster>();
        private readonly Dictionary<string, StorageReservation> reservations = new Dictionary<string, StorageReservation>();
        private readonly StorageLocator locator = new StorageLocator();
        private readonly ResourceFlowManager flowManager;
        private readonly IEventBus eventBus;
        private int reservationCounter;

        public StorageDiagnostics Diagnostics { get; } = new StorageDiagnostics();

        public HiveInventoryManager(ResourceFlowManager flowManager, IEventBus eventBus = null)
        {
            this.flowManager = flowManager;
            this.eventBus = eventBus;
        }

        public StorageCell CreateCell(string cellId, StoragePosition position, ResourceType type, double capacity, string clusterId = null)
        {
            StorageCell cell = new StorageCell(cellId, position, type, capacity);
            grid.AddCell(cell);
            if (!string.IsNullOrWhiteSpace(clusterId))
            {
                if (!clusters.TryGetValue(clusterId, out StorageCluster cluster))
                {
                    cluster = new StorageCluster(clusterId, type);
                    clusters[clusterId] = cluster;
                }
                cluster.AddCell(cell);
            }
            eventBus?.Publish(new StorageCellCreated(cellId));
            Record();
            return cell;
        }

        public bool FindStorage(ResourceType type, double amount, StoragePosition origin, StoragePolicy policy, out StorageCell cell)
        {
            return locator.TryFind(grid, type, amount, origin, policy, out cell);
        }

        public StorageReservation ReserveSpace(ResourceType type, double amount, StoragePosition origin, StoragePolicy policy)
        {
            if (!FindStorage(type, amount, origin, policy, out StorageCell cell) || !cell.ReserveSpace(amount))
            {
                return default;
            }

            string id = "storage-reservation-" + (++reservationCounter);
            StorageReservation reservation = new StorageReservation(id, cell.CellId, type, amount);
            reservations[id] = reservation;
            Diagnostics.RecordReservation();
            eventBus?.Publish(new StorageReservationCreated(id));
            Record();
            return reservation;
        }

        public bool ReleaseReservation(StorageReservation reservation)
        {
            if (!reservation.IsValid || !reservations.Remove(reservation.ReservationId) || !grid.TryGetCell(reservation.CellId, out StorageCell cell)) return false;
            bool released = cell.ReleaseReservation(reservation.Amount);
            if (released) eventBus?.Publish(new StorageReservationReleased(reservation.ReservationId));
            Record();
            return released;
        }

        public bool Deposit(StorageReservation reservation, double nowSeconds)
        {
            if (!reservation.IsValid || !reservations.Remove(reservation.ReservationId) || !grid.TryGetCell(reservation.CellId, out StorageCell cell)) return false;
            bool deposited = cell.CommitDeposit(reservation.Amount);
            if (deposited)
            {
                flowManager.Store(cell.CellId, reservation.ResourceType, reservation.Amount, nowSeconds);
                if (cell.State == StorageCellState.Full) eventBus?.Publish(new StorageCellFilled(cell.CellId));
            }
            Record();
            return deposited;
        }

        public bool Withdraw(string cellId, double amount)
        {
            if (!grid.TryGetCell(cellId, out StorageCell cell)) return false;
            bool withdrawn = cell.CommitWithdrawal(amount);
            if (withdrawn && cell.State == StorageCellState.Empty) eventBus?.Publish(new StorageCellEmptied(cellId));
            Record();
            return withdrawn;
        }

        public bool MoveResources(string fromCellId, string toCellId, double amount, double nowSeconds)
        {
            if (!grid.TryGetCell(fromCellId, out StorageCell from) || !grid.TryGetCell(toCellId, out StorageCell to)) return false;
            if (from.ResourceType != to.ResourceType || !from.CommitWithdrawal(amount) || !to.ReserveSpace(amount) || !to.CommitDeposit(amount)) return false;
            flowManager.Transfer(fromCellId, toCellId, from.ResourceType, amount, nowSeconds);
            Record();
            return true;
        }

        public StorageStatistics QueryInventory()
        {
            int count = 0;
            double amount = 0d;
            double capacity = 0d;
            foreach (StorageCell cell in grid.Cells.Values)
            {
                count++;
                amount += cell.CurrentAmount;
                capacity += cell.Capacity;
            }
            return new StorageStatistics(count, amount, capacity);
        }

        private void Record()
        {
            StorageStatistics stats = QueryInventory();
            Diagnostics.Record(stats);
            foreach (StorageCluster cluster in clusters.Values)
            {
                if (cluster.IsFull)
                {
                    Diagnostics.RecordSaturation();
                    eventBus?.Publish(new StorageClusterFull(cluster.ClusterId));
                }
            }
        }
    }
}
