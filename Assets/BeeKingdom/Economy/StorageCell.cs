namespace BeeKingdom.Economy
{
    public sealed class StorageCell
    {
        public string CellId { get; }
        public StoragePosition Position { get; }
        public ResourceType ResourceType { get; }
        public double Capacity { get; }
        public double CurrentAmount { get; private set; }
        public double ReservedAmount { get; private set; }
        public float Accessibility { get; private set; }
        public StorageCellState State { get; private set; }
        public double AvailableSpace => Capacity - CurrentAmount - ReservedAmount;
        public double AvailableAmount => CurrentAmount - ReservedAmount;

        public StorageCell(string cellId, StoragePosition position, ResourceType resourceType, double capacity, float accessibility = 1f)
        {
            CellId = cellId;
            Position = position;
            ResourceType = resourceType;
            Capacity = capacity < 0d ? 0d : capacity;
            Accessibility = accessibility < 0f ? 0f : accessibility;
            State = StorageCellState.Empty;
        }

        public bool ReserveSpace(double amount)
        {
            if (State == StorageCellState.Locked || State == StorageCellState.Damaged || amount <= 0d || AvailableSpace < amount) return false;
            ReservedAmount += amount;
            State = StorageCellState.Reserved;
            return true;
        }

        public bool ReleaseReservation(double amount)
        {
            if (amount <= 0d || ReservedAmount < amount) return false;
            ReservedAmount -= amount;
            RefreshState();
            return true;
        }

        public bool CommitDeposit(double amount)
        {
            if (amount <= 0d || ReservedAmount < amount || CurrentAmount + amount > Capacity) return false;
            ReservedAmount -= amount;
            CurrentAmount += amount;
            RefreshState();
            return true;
        }

        public bool CommitWithdrawal(double amount)
        {
            if (amount <= 0d || CurrentAmount < amount) return false;
            CurrentAmount -= amount;
            RefreshState();
            return true;
        }

        private void RefreshState()
        {
            if (CurrentAmount <= 0d && ReservedAmount <= 0d) State = StorageCellState.Empty;
            else if (CurrentAmount >= Capacity) State = StorageCellState.Full;
            else if (ReservedAmount > 0d) State = StorageCellState.Reserved;
            else State = StorageCellState.Filling;
        }
    }
}
