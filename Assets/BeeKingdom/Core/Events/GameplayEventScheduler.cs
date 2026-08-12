using System.Collections.Generic;

namespace BeeKingdom.Core.Events
{
    public enum ScheduledGameplayEventType { Delayed, Periodic, Calendar, LiveOps }

    public sealed class ScheduledGameplayEvent
    {
        public long EventId { get; }
        public string EventKey { get; }
        public ScheduledGameplayEventType EventType { get; }
        public double DueSeconds { get; private set; }
        public double PeriodSeconds { get; }
        public bool IsCancelled { get; private set; }

        public ScheduledGameplayEvent(long eventId, string eventKey, ScheduledGameplayEventType eventType, double dueSeconds, double periodSeconds = 0d)
        {
            EventId = eventId;
            EventKey = eventKey ?? string.Empty;
            EventType = eventType;
            DueSeconds = dueSeconds < 0d ? 0d : dueSeconds;
            PeriodSeconds = periodSeconds < 0d ? 0d : periodSeconds;
        }

        public void Cancel() => IsCancelled = true;
        public bool Reschedule()
        {
            if (PeriodSeconds <= 0d) return false;
            DueSeconds += PeriodSeconds;
            return true;
        }
    }

    public sealed class GameplayEventScheduler
    {
        private readonly List<ScheduledGameplayEvent> events = new List<ScheduledGameplayEvent>();
        private long nextId = 1;

        public ScheduledGameplayEvent Schedule(string key, double dueSeconds, ScheduledGameplayEventType type = ScheduledGameplayEventType.Delayed, double periodSeconds = 0d)
        {
            ScheduledGameplayEvent item = new ScheduledGameplayEvent(nextId++, key, type, dueSeconds, periodSeconds);
            events.Add(item);
            return item;
        }

        public bool Cancel(long eventId)
        {
            ScheduledGameplayEvent item = events.Find(e => e.EventId == eventId);
            if (item == null) return false;
            item.Cancel();
            return true;
        }

        public IReadOnlyList<ScheduledGameplayEvent> Tick(double nowSeconds)
        {
            List<ScheduledGameplayEvent> due = new List<ScheduledGameplayEvent>();
            for (int i = 0; i < events.Count; i++)
            {
                ScheduledGameplayEvent item = events[i];
                if (item.IsCancelled || item.DueSeconds > nowSeconds) continue;
                due.Add(item);
                if (!item.Reschedule())
                {
                    item.Cancel();
                }
            }
            return due;
        }

        public GameplayEventSchedulerSnapshot Snapshot()
        {
            return new GameplayEventSchedulerSnapshot(nextId, new List<ScheduledGameplayEvent>(events));
        }

        public void Restore(GameplayEventSchedulerSnapshot snapshot)
        {
            nextId = snapshot.NextId;
            events.Clear();
            events.AddRange(snapshot.Events);
        }
    }

    public sealed class GameplayEventSchedulerSnapshot
    {
        public long NextId { get; }
        public IReadOnlyList<ScheduledGameplayEvent> Events { get; }

        public GameplayEventSchedulerSnapshot(long nextId, IReadOnlyList<ScheduledGameplayEvent> events)
        {
            NextId = nextId;
            Events = events;
        }
    }
}
