namespace BeeKingdom.Economy
{
    public readonly struct ResourceReservation
    {
        public string ReservationId { get; }
        public string StorageId { get; }
        public ResourceType ResourceType { get; }
        public double Amount { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(ReservationId);

        public ResourceReservation(string reservationId, string storageId, ResourceType resourceType, double amount)
        {
            ReservationId = reservationId;
            StorageId = storageId;
            ResourceType = resourceType;
            Amount = amount;
        }
    }
}
