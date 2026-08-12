using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Builders
{
    public enum DeliveryResourceType { Wax, Honey, Nectar, Pollen, Water, RoyalJelly, Propolis, Custom }
    public enum DeliveryState { Requested, ResourcesReserved, TransportAssigned, Pickup, Transport, Delivered, Validated, WaitingResources, Delayed, Cancelled, Failed }

    public sealed class DeliveryRequest
    {
        public string RequestId { get; }
        public string ConstructionId { get; }
        public DeliveryResourceType ResourceType { get; }
        public double Amount { get; }
        public int Priority { get; }

        public DeliveryRequest(string requestId, string constructionId, DeliveryResourceType resourceType, double amount, int priority = 0)
        {
            RequestId = string.IsNullOrWhiteSpace(requestId) ? throw new ArgumentException("Request id is required.", nameof(requestId)) : requestId;
            ConstructionId = constructionId ?? string.Empty;
            ResourceType = resourceType;
            Amount = amount <= 0d ? 1d : amount;
            Priority = priority;
        }
    }

    public sealed class DeliveryReservation
    {
        public string ReservationId { get; }
        public string RequestId { get; }
        public double ReservedAmount { get; }
        public int ReservedTransporters { get; private set; }

        public DeliveryReservation(string reservationId, string requestId, double reservedAmount)
        {
            ReservationId = reservationId ?? string.Empty;
            RequestId = requestId ?? string.Empty;
            ReservedAmount = reservedAmount < 0d ? 0d : reservedAmount;
        }

        public void AssignTransporters(int count) => ReservedTransporters = count < 0 ? 0 : count;
    }

    public sealed class DeliveryBatch
    {
        public string BatchId { get; }
        public double Amount { get; }
        public bool Completed { get; private set; }

        public DeliveryBatch(string batchId, double amount)
        {
            BatchId = batchId ?? string.Empty;
            Amount = amount < 0d ? 0d : amount;
        }

        public void Complete() => Completed = true;
    }

    public sealed class DeliveryOrder
    {
        private readonly List<DeliveryBatch> batches = new List<DeliveryBatch>();

        public string OrderId { get; }
        public DeliveryRequest Request { get; }
        public DeliveryReservation Reservation { get; private set; }
        public DeliveryState State { get; private set; }
        public double DeliveredAmount { get; private set; }
        public IReadOnlyList<DeliveryBatch> Batches => batches;

        public DeliveryOrder(string orderId, DeliveryRequest request)
        {
            OrderId = orderId ?? string.Empty;
            Request = request ?? throw new ArgumentNullException(nameof(request));
            State = DeliveryState.Requested;
        }

        public void Reserve(DeliveryReservation reservation)
        {
            Reservation = reservation;
            State = reservation.ReservedAmount >= Request.Amount ? DeliveryState.ResourcesReserved : DeliveryState.WaitingResources;
        }

        public void AssignTransporters(int count)
        {
            Reservation?.AssignTransporters(count);
            if (count > 0 && State == DeliveryState.ResourcesReserved) State = DeliveryState.TransportAssigned;
        }

        public void Start()
        {
            if (State == DeliveryState.TransportAssigned || State == DeliveryState.ResourcesReserved) State = DeliveryState.Transport;
        }

        public void CompleteBatch(DeliveryBatch batch)
        {
            if (batch == null || batch.Completed) return;
            batch.Complete();
            DeliveredAmount += batch.Amount;
            State = DeliveredAmount >= Request.Amount ? DeliveryState.Validated : DeliveryState.Delivered;
        }

        public void AddBatch(DeliveryBatch batch) => batches.Add(batch);
        public void Delay() => State = DeliveryState.Delayed;
        public void Cancel() => State = DeliveryState.Cancelled;
        public void Fail() => State = DeliveryState.Failed;
    }

    public sealed class DeliveryDiagnostics
    {
        public int Requested { get; private set; }
        public int Reserved { get; private set; }
        public int TransportAssigned { get; private set; }
        public int Started { get; private set; }
        public int Completed { get; private set; }
        public int Delayed { get; private set; }
        public int Cancelled { get; private set; }
        public int Failed { get; private set; }
        public void RecordRequested() => Requested++;
        public void RecordReserved() => Reserved++;
        public void RecordTransportAssigned() => TransportAssigned++;
        public void RecordStarted() => Started++;
        public void RecordCompleted() => Completed++;
        public void RecordDelayed() => Delayed++;
        public void RecordCancelled() => Cancelled++;
        public void RecordFailed() => Failed++;
    }

    public sealed class ResourceDeliveryManager
    {
        private readonly Dictionary<string, DeliveryOrder> orders = new Dictionary<string, DeliveryOrder>();
        private readonly IEventBus eventBus;
        private long counter;

        public DeliveryDiagnostics Diagnostics { get; } = new DeliveryDiagnostics();

        public ResourceDeliveryManager(IEventBus eventBus = null)
        {
            this.eventBus = eventBus;
        }

        public DeliveryOrder CreateDeliveryRequest(string constructionId, DeliveryResourceType resourceType, double amount, int priority = 0)
        {
            DeliveryRequest request = new DeliveryRequest("delivery-request-" + (++counter), constructionId, resourceType, amount, priority);
            DeliveryOrder order = new DeliveryOrder("delivery-order-" + counter, request);
            orders.Add(order.OrderId, order);
            Diagnostics.RecordRequested();
            eventBus?.Publish(new DeliveryRequested(order.OrderId));
            return order;
        }

        public bool ReserveResources(string orderId, double availableAmount)
        {
            if (!orders.TryGetValue(orderId, out DeliveryOrder order)) return false;
            order.Reserve(new DeliveryReservation("delivery-reservation-" + orderId, order.Request.RequestId, Math.Max(0d, availableAmount)));
            Diagnostics.RecordReserved();
            eventBus?.Publish(new ResourcesReserved(orderId));
            return true;
        }

        public bool AssignTransporters(string orderId, int transporterCount)
        {
            if (!orders.TryGetValue(orderId, out DeliveryOrder order)) return false;
            order.AssignTransporters(transporterCount);
            Diagnostics.RecordTransportAssigned();
            eventBus?.Publish(new TransportAssigned(orderId));
            return true;
        }

        public bool StartDelivery(string orderId)
        {
            if (!orders.TryGetValue(orderId, out DeliveryOrder order)) return false;
            order.Start();
            Diagnostics.RecordStarted();
            eventBus?.Publish(new DeliveryStarted(orderId));
            return true;
        }

        public bool CompleteDelivery(string orderId, double amount)
        {
            if (!orders.TryGetValue(orderId, out DeliveryOrder order)) return false;
            DeliveryBatch batch = new DeliveryBatch("delivery-batch-" + order.Batches.Count, amount);
            order.AddBatch(batch);
            order.CompleteBatch(batch);
            if (order.State == DeliveryState.Validated) { Diagnostics.RecordCompleted(); eventBus?.Publish(new DeliveryCompleted(orderId)); }
            return true;
        }

        public bool DelayDelivery(string orderId)
        {
            if (!orders.TryGetValue(orderId, out DeliveryOrder order)) return false;
            order.Delay();
            Diagnostics.RecordDelayed();
            eventBus?.Publish(new DeliveryDelayed(orderId));
            return true;
        }

        public bool CancelDelivery(string orderId)
        {
            if (!orders.TryGetValue(orderId, out DeliveryOrder order)) return false;
            order.Cancel();
            Diagnostics.RecordCancelled();
            eventBus?.Publish(new DeliveryCancelled(orderId));
            return true;
        }

        public IReadOnlyList<DeliveryOrder> QueryDeliveries()
        {
            List<DeliveryOrder> result = new List<DeliveryOrder>(orders.Values);
            result.Sort((left, right) => string.CompareOrdinal(left.OrderId, right.OrderId));
            return result;
        }
    }

    public readonly struct DeliveryRequested : IGameplayEvent, IBuildingEvent { public string OrderId { get; } public DeliveryRequested(string orderId) { OrderId = orderId; } }
    public readonly struct ResourcesReserved : IGameplayEvent, IBuildingEvent { public string OrderId { get; } public ResourcesReserved(string orderId) { OrderId = orderId; } }
    public readonly struct TransportAssigned : IGameplayEvent, IBuildingEvent { public string OrderId { get; } public TransportAssigned(string orderId) { OrderId = orderId; } }
    public readonly struct DeliveryStarted : IGameplayEvent, IBuildingEvent { public string OrderId { get; } public DeliveryStarted(string orderId) { OrderId = orderId; } }
    public readonly struct DeliveryCompleted : IGameplayEvent, IBuildingEvent { public string OrderId { get; } public DeliveryCompleted(string orderId) { OrderId = orderId; } }
    public readonly struct DeliveryDelayed : IGameplayEvent, IBuildingEvent { public string OrderId { get; } public DeliveryDelayed(string orderId) { OrderId = orderId; } }
    public readonly struct DeliveryCancelled : IGameplayEvent, IBuildingEvent { public string OrderId { get; } public DeliveryCancelled(string orderId) { OrderId = orderId; } }
    public readonly struct DeliveryFailed : IGameplayEvent, IBuildingEvent { public string OrderId { get; } public DeliveryFailed(string orderId) { OrderId = orderId; } }
}
