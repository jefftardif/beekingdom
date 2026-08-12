using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Core.Events
{
    public sealed class EventDiagnostics
    {
        private readonly Dictionary<int, EventDiagnosticEntry> entriesByEventId = new Dictionary<int, EventDiagnosticEntry>();

        public long TotalPublishedCount { get; private set; }

        public IReadOnlyList<EventDiagnosticEntry> Entries => entriesByEventId.Values.ToList();

        public void RecordPublish(int eventId, string eventName, int subscriberCount, long elapsedTicks)
        {
            TotalPublishedCount++;

            if (!entriesByEventId.TryGetValue(eventId, out EventDiagnosticEntry entry))
            {
                entry = new EventDiagnosticEntry(eventId, eventName);
                entriesByEventId[eventId] = entry;
            }

            entry.RecordPublish(subscriberCount, elapsedTicks);
        }

        public void SetSubscriberCount(int eventId, string eventName, int subscriberCount)
        {
            if (!entriesByEventId.TryGetValue(eventId, out EventDiagnosticEntry entry))
            {
                entry = new EventDiagnosticEntry(eventId, eventName);
                entriesByEventId[eventId] = entry;
            }

            entry.SetSubscriberCount(subscriberCount);
        }

        public IReadOnlyList<EventDiagnosticEntry> GetMostFrequentEvents(int count)
        {
            return entriesByEventId.Values
                .OrderByDescending(entry => entry.PublishedCount)
                .Take(Math.Max(0, count))
                .ToList();
        }

        public void Clear()
        {
            entriesByEventId.Clear();
            TotalPublishedCount = 0;
        }
    }

    public sealed class EventDiagnosticEntry
    {
        public int EventId { get; }
        public string EventName { get; }
        public long PublishedCount { get; private set; }
        public int SubscriberCount { get; private set; }
        public long TotalDispatchTicks { get; private set; }
        public double AverageDispatchTicks => PublishedCount == 0 ? 0d : (double)TotalDispatchTicks / PublishedCount;

        public EventDiagnosticEntry(int eventId, string eventName)
        {
            EventId = eventId;
            EventName = eventName;
        }

        public void RecordPublish(int subscriberCount, long elapsedTicks)
        {
            PublishedCount++;
            SubscriberCount = subscriberCount;
            TotalDispatchTicks += elapsedTicks;
        }

        public void SetSubscriberCount(int subscriberCount)
        {
            SubscriberCount = subscriberCount;
        }
    }
}
