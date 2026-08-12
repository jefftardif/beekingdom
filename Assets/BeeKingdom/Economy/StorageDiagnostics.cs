namespace BeeKingdom.Economy
{
    public sealed class StorageDiagnostics
    {
        public StorageStatistics LastStatistics { get; private set; }
        public int ReservationCount { get; private set; }
        public int SaturationCount { get; private set; }
        public void Record(StorageStatistics statistics) { LastStatistics = statistics; }
        public void RecordReservation() { ReservationCount++; }
        public void RecordSaturation() { SaturationCount++; }
    }
}
