using System.Collections.Generic;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Economy
{
    public sealed class ResourceFlowManager
    {
        private readonly ResourceFlowGraph graph = new ResourceFlowGraph();
        private readonly Dictionary<string, ResourceReservation> reservations = new Dictionary<string, ResourceReservation>();
        private readonly Queue<ResourceTransaction> history = new Queue<ResourceTransaction>();
        private readonly IEventBus eventBus;
        private readonly int historyLimit;
        private int reservationCounter;

        public ResourceFlowDiagnostics Diagnostics { get; } = new ResourceFlowDiagnostics();

        public ResourceFlowManager(IEventBus eventBus = null, int historyLimit = 256)
        {
            this.eventBus = eventBus;
            this.historyLimit = historyLimit <= 0 ? 1 : historyLimit;
        }

        public ResourceStorage GetStorage(string storageId) => graph.GetOrCreateStorage(storageId);

        public double Produce(string producerId, string storageId, ResourceType type, double amount, double nowSeconds)
        {
            double stored = Store(storageId, type, amount, nowSeconds);
            Record(new ResourceTransaction(producerId, storageId, type, stored, nowSeconds, ResourceTransactionStatus.Produced));
            eventBus?.Publish(new ResourceProduced(type, stored));
            return stored;
        }

        public ResourceReservation Reserve(string storageId, ResourceType type, double amount)
        {
            ResourceStorage storage = graph.GetOrCreateStorage(storageId);
            if (!storage.Reserve(type, amount))
            {
                Diagnostics.RecordShortage();
                eventBus?.Publish(new ResourceShortage(storageId, type, amount));
                return default;
            }

            string id = "reservation-" + (++reservationCounter);
            ResourceReservation reservation = new ResourceReservation(id, storageId, type, amount);
            reservations[id] = reservation;
            return reservation;
        }

        public bool Release(ResourceReservation reservation)
        {
            if (!reservation.IsValid || !reservations.Remove(reservation.ReservationId)) return false;
            return graph.GetOrCreateStorage(reservation.StorageId).Release(reservation.ResourceType, reservation.Amount);
        }

        public bool Consume(ResourceReservation reservation, double nowSeconds)
        {
            if (!reservation.IsValid || !reservations.Remove(reservation.ReservationId)) return false;
            ResourceStorage storage = graph.GetOrCreateStorage(reservation.StorageId);
            if (!storage.Consume(reservation.ResourceType, reservation.Amount)) return false;
            Record(new ResourceTransaction(reservation.StorageId, string.Empty, reservation.ResourceType, reservation.Amount, nowSeconds, ResourceTransactionStatus.Consumed));
            eventBus?.Publish(new ResourceConsumed(reservation.ResourceType, reservation.Amount));
            return true;
        }

        public bool Transfer(string originStorageId, string destinationStorageId, ResourceType type, double amount, double nowSeconds)
        {
            ResourceReservation reservation = Reserve(originStorageId, type, amount);
            if (!reservation.IsValid) return false;
            eventBus?.Publish(new ResourceTransportStarted(type, amount));
            if (!Consume(reservation, nowSeconds)) return false;
            Store(destinationStorageId, type, amount, nowSeconds);
            Record(new ResourceTransaction(originStorageId, destinationStorageId, type, amount, nowSeconds, ResourceTransactionStatus.Delivered));
            eventBus?.Publish(new ResourceDelivered(type, amount));
            return true;
        }

        public double Store(string storageId, ResourceType type, double amount, double nowSeconds)
        {
            ResourceStorage storage = graph.GetOrCreateStorage(storageId);
            double stored = storage.Store(type, amount);
            if (stored < amount)
            {
                Diagnostics.RecordStorageFull();
                eventBus?.Publish(new ResourceStorageFull(storageId, type));
            }

            Record(new ResourceTransaction(string.Empty, storageId, type, stored, nowSeconds, ResourceTransactionStatus.Stored));
            return stored;
        }

        public double QueryFlow(string storageId, ResourceType type)
        {
            return graph.GetOrCreateStorage(storageId).GetAmount(type);
        }

        public IReadOnlyCollection<ResourceTransaction> GetHistory()
        {
            return history;
        }

        private void Record(ResourceTransaction transaction)
        {
            history.Enqueue(transaction);
            while (history.Count > historyLimit)
            {
                history.Dequeue();
            }

            Diagnostics.RecordTransaction();
        }
    }
}
