namespace BeeKingdom.Economy
{
    public readonly struct ResourceProducer { public string ProducerId { get; } public ResourceProducer(string producerId) { ProducerId = producerId; } }
    public readonly struct ResourceCarrier { public string CarrierId { get; } public ResourceCarrier(string carrierId) { CarrierId = carrierId; } }
    public readonly struct ResourceConsumer { public string ConsumerId { get; } public ResourceConsumer(string consumerId) { ConsumerId = consumerId; } }
    public readonly struct ResourceProcessor { public string ProcessorId { get; } public ResourceProcessor(string processorId) { ProcessorId = processorId; } }
}
