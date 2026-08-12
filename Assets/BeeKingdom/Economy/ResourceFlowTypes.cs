namespace BeeKingdom.Economy
{
    public enum ResourceType
    {
        Nectar,
        Pollen,
        Water,
        Wax,
        Honey,
        RoyalJelly,
        Propolis
    }

    public enum ResourceTransactionStatus
    {
        Produced,
        TransportStarted,
        Delivered,
        Stored,
        Consumed,
        Released,
        Failed
    }
}
