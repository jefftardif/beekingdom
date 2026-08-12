namespace BeeKingdom.Hive
{
    public sealed class QueenDiagnostics
    {
        public QueenState State { get; private set; }
        public int Level { get; private set; }
        public int EggsProduced { get; private set; }
        public int BonusCount { get; private set; }
        public bool LastValidationPassed { get; private set; }

        public void Record(QueenAggregate queen, bool isValid)
        {
            State = queen.State;
            Level = queen.Evolution.Level;
            EggsProduced = queen.EggProduction.TotalProduced;
            BonusCount = queen.BonusCount;
            LastValidationPassed = isValid;
        }
    }
}
