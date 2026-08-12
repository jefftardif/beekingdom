namespace BeeKingdom.Economy
{
    public readonly struct StorageReservation
    {
        public string ReservationId { get; }
        public string CellId { get; }
        public ResourceType ResourceType { get; }
        public double Amount { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(ReservationId);
        public StorageReservation(string reservationId, string cellId, ResourceType resourceType, double amount) { ReservationId = reservationId; CellId = cellId; ResourceType = resourceType; Amount = amount; }
    }
}
