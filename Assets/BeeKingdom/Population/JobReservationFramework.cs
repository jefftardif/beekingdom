using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Population
{
    public enum ReservationType { Resource, Task, Building, Chamber, Target, Position, Path, MultiAgent, Custom }
    public enum ReservationState { Requested, Validated, Reserved, Active, Released, Expired, Cancelled, Failed, Transferred }

    public sealed class ReservationTicket
    {
        public string ReservationId { get; }
        public string OwnerId { get; private set; }
        public string TargetId { get; }
        public ReservationType Type { get; }
        public ReservationState State { get; private set; }
        public double ExpiresAt { get; private set; }
        public int Priority { get; }

        public ReservationTicket(string reservationId, string ownerId, string targetId, ReservationType type, double expiresAt, int priority)
        {
            ReservationId = string.IsNullOrWhiteSpace(reservationId) ? throw new ArgumentException("Reservation id is required.", nameof(reservationId)) : reservationId;
            OwnerId = string.IsNullOrWhiteSpace(ownerId) ? throw new ArgumentException("Owner id is required.", nameof(ownerId)) : ownerId;
            TargetId = targetId ?? string.Empty;
            Type = type;
            ExpiresAt = Math.Max(0d, expiresAt);
            Priority = priority;
            State = ReservationState.Requested;
        }

        public void SetState(ReservationState state) => State = state;
        public void Renew(double expiresAt) => ExpiresAt = Math.Max(ExpiresAt, expiresAt);
        public void Transfer(string newOwnerId) { OwnerId = string.IsNullOrWhiteSpace(newOwnerId) ? OwnerId : newOwnerId; State = ReservationState.Transferred; }
        public bool IsExpired(double time) => State != ReservationState.Released && State != ReservationState.Cancelled && time >= ExpiresAt;
    }

    public sealed class ReservationRegistry
    {
        private readonly Dictionary<string, ReservationTicket> tickets = new Dictionary<string, ReservationTicket>();
        private readonly Dictionary<string, string> byTarget = new Dictionary<string, string>();
        public bool Register(ReservationTicket ticket)
        {
            if (ticket == null || tickets.ContainsKey(ticket.ReservationId)) return false;
            tickets.Add(ticket.ReservationId, ticket);
            byTarget[ticket.Type + ":" + ticket.TargetId] = ticket.ReservationId;
            return true;
        }
        public bool TryGet(string reservationId, out ReservationTicket ticket) => tickets.TryGetValue(reservationId, out ticket);
        public bool TryGetByTarget(ReservationType type, string targetId, out ReservationTicket ticket)
        {
            if (byTarget.TryGetValue(type + ":" + (targetId ?? string.Empty), out string id)) return tickets.TryGetValue(id, out ticket);
            ticket = null;
            return false;
        }
        public void Remove(string reservationId)
        {
            if (!tickets.TryGetValue(reservationId, out ReservationTicket ticket)) return;
            byTarget.Remove(ticket.Type + ":" + ticket.TargetId);
            tickets.Remove(reservationId);
        }
        public IReadOnlyList<ReservationTicket> Query()
        {
            List<ReservationTicket> result = new List<ReservationTicket>(tickets.Values);
            result.Sort((left, right) => string.CompareOrdinal(left.ReservationId, right.ReservationId));
            return result;
        }
    }

    public sealed class ReservationValidator
    {
        public bool Validate(ReservationTicket existing, ReservationTicket requested)
        {
            if (existing == null) return true;
            if (existing.OwnerId == requested.OwnerId) return true;
            return requested.Priority > existing.Priority;
        }
    }

    public sealed class ReservationEngine
    {
        private readonly ReservationValidator validator = new ReservationValidator();
        public bool ValidateReservation(ReservationTicket existing, ReservationTicket requested) => validator.Validate(existing, requested);
    }

    public sealed class ReservationDiagnostics
    {
        public int Requested { get; private set; }
        public int Granted { get; private set; }
        public int Rejected { get; private set; }
        public int Released { get; private set; }
        public int Expired { get; private set; }
        public int Transferred { get; private set; }
        public void RecordRequested() => Requested++;
        public void RecordGranted() => Granted++;
        public void RecordRejected() => Rejected++;
        public void RecordReleased() => Released++;
        public void RecordExpired() => Expired++;
        public void RecordTransferred() => Transferred++;
    }

    public sealed class JobReservationManager
    {
        private readonly ReservationRegistry registry = new ReservationRegistry();
        private readonly ReservationEngine engine = new ReservationEngine();
        private readonly IEventBus eventBus;
        private int sequence;
        public ReservationDiagnostics Diagnostics { get; } = new ReservationDiagnostics();

        public JobReservationManager(IEventBus eventBus = null) { this.eventBus = eventBus; }

        public ReservationTicket RequestReservation(string ownerId, string targetId, ReservationType type, double currentTime, double duration, int priority = 0)
        {
            ReservationTicket requested = new ReservationTicket("reservation-" + (++sequence).ToString("D6"), ownerId, targetId, type, currentTime + Math.Max(0d, duration), priority);
            Diagnostics.RecordRequested();
            eventBus?.Publish(new ReservationRequested(requested.ReservationId));
            registry.TryGetByTarget(type, targetId, out ReservationTicket existing);
            if (!engine.ValidateReservation(existing, requested))
            {
                requested.SetState(ReservationState.Failed);
                Diagnostics.RecordRejected();
                eventBus?.Publish(new ReservationRejected(requested.ReservationId));
                return requested;
            }
            if (existing != null) registry.Remove(existing.ReservationId);
            requested.SetState(ReservationState.Reserved);
            registry.Register(requested);
            Diagnostics.RecordGranted();
            eventBus?.Publish(new ReservationGranted(requested.ReservationId));
            return requested;
        }

        public bool ValidateReservation(string reservationId)
        {
            if (!registry.TryGet(reservationId, out ReservationTicket ticket)) return false;
            ticket.SetState(ReservationState.Validated);
            return true;
        }

        public bool RenewReservation(string reservationId, double expiresAt)
        {
            if (!registry.TryGet(reservationId, out ReservationTicket ticket)) return false;
            ticket.Renew(expiresAt);
            return true;
        }

        public bool ReleaseReservation(string reservationId)
        {
            if (!registry.TryGet(reservationId, out ReservationTicket ticket)) return false;
            ticket.SetState(ReservationState.Released);
            registry.Remove(reservationId);
            Diagnostics.RecordReleased();
            eventBus?.Publish(new ReservationReleased(reservationId));
            return true;
        }

        public bool CancelReservation(string reservationId)
        {
            if (!registry.TryGet(reservationId, out ReservationTicket ticket)) return false;
            ticket.SetState(ReservationState.Cancelled);
            registry.Remove(reservationId);
            return true;
        }

        public bool TransferReservation(string reservationId, string newOwnerId)
        {
            if (!registry.TryGet(reservationId, out ReservationTicket ticket)) return false;
            ticket.Transfer(newOwnerId);
            Diagnostics.RecordTransferred();
            eventBus?.Publish(new ReservationTransferred(reservationId, newOwnerId));
            return true;
        }

        public void ExpireReservations(double currentTime)
        {
            List<ReservationTicket> tickets = new List<ReservationTicket>(registry.Query());
            for (int i = 0; i < tickets.Count; i++)
            {
                if (!tickets[i].IsExpired(currentTime)) continue;
                tickets[i].SetState(ReservationState.Expired);
                registry.Remove(tickets[i].ReservationId);
                Diagnostics.RecordExpired();
                eventBus?.Publish(new ReservationExpired(tickets[i].ReservationId));
            }
        }

        public IReadOnlyList<ReservationTicket> QueryReservations() => registry.Query();
    }

    public readonly struct ReservationRequested : IGameplayEvent, IBeeEvent { public string ReservationId { get; } public ReservationRequested(string reservationId) { ReservationId = reservationId; } }
    public readonly struct ReservationGranted : IGameplayEvent, IBeeEvent { public string ReservationId { get; } public ReservationGranted(string reservationId) { ReservationId = reservationId; } }
    public readonly struct ReservationRejected : IGameplayEvent, IBeeEvent { public string ReservationId { get; } public ReservationRejected(string reservationId) { ReservationId = reservationId; } }
    public readonly struct ReservationReleased : IGameplayEvent, IBeeEvent { public string ReservationId { get; } public ReservationReleased(string reservationId) { ReservationId = reservationId; } }
    public readonly struct ReservationExpired : IGameplayEvent, IBeeEvent { public string ReservationId { get; } public ReservationExpired(string reservationId) { ReservationId = reservationId; } }
    public readonly struct ReservationTransferred : IGameplayEvent, IBeeEvent { public string ReservationId { get; } public string NewOwnerId { get; } public ReservationTransferred(string reservationId, string newOwnerId) { ReservationId = reservationId; NewOwnerId = newOwnerId; } }
}
