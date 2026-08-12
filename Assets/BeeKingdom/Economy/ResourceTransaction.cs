namespace BeeKingdom.Economy
{
    public readonly struct ResourceTransaction
    {
        public string OriginId { get; }
        public string DestinationId { get; }
        public ResourceType ResourceType { get; }
        public double Amount { get; }
        public double TimestampSeconds { get; }
        public ResourceTransactionStatus Status { get; }

        public ResourceTransaction(string originId, string destinationId, ResourceType resourceType, double amount, double timestampSeconds, ResourceTransactionStatus status)
        {
            OriginId = originId ?? string.Empty;
            DestinationId = destinationId ?? string.Empty;
            ResourceType = resourceType;
            Amount = amount;
            TimestampSeconds = timestampSeconds;
            Status = status;
        }
    }
}
