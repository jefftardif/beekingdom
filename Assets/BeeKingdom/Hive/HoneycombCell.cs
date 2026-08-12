namespace BeeKingdom.Hive
{
    public sealed class HoneycombCell
    {
        public string CellId { get; }
        public string ChamberId { get; private set; }
        public HivePosition Position { get; }
        public HiveElementFunction Function { get; private set; }
        public int Level { get; private set; }
        public double Integrity { get; private set; }
        public HoneycombCellState State { get; private set; }

        public HoneycombCell(string cellId, HivePosition position, HiveElementFunction function, int level = 1)
        {
            CellId = string.IsNullOrWhiteSpace(cellId) ? System.Guid.NewGuid().ToString("N") : cellId;
            Position = position;
            Function = function;
            Level = level < 1 ? 1 : level;
            Integrity = 1d;
            State = HoneycombCellState.Planned;
        }

        public void AssignToChamber(string chamberId)
        {
            ChamberId = chamberId;
        }

        public void StartConstruction()
        {
            if (State == HoneycombCellState.Planned)
            {
                State = HoneycombCellState.Building;
            }
        }

        public void Complete()
        {
            State = HoneycombCellState.Complete;
            Integrity = 1d;
        }

        public void Upgrade()
        {
            Level++;
            Integrity = 1d;
        }

        public void Damage(double amount)
        {
            if (amount <= 0d)
            {
                return;
            }

            Integrity = System.Math.Max(0d, Integrity - amount);
            if (Integrity <= 0d)
            {
                State = HoneycombCellState.Disabled;
            }
            else if (Integrity < 1d)
            {
                State = HoneycombCellState.Damaged;
            }
        }
    }
}
