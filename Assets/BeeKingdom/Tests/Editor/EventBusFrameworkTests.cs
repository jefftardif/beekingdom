using System.Collections.Generic;
using System.Diagnostics;
using BeeKingdom.Core.Events;
using BeeKingdom.Services;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class EventBusFrameworkTests
    {
        [Test]
        public void PublishInvokesSubscribers()
        {
            EventBus bus = new EventBus();
            int received = 0;

            bus.Subscribe<TestGameplayEvent>(evt => received = evt.Value);
            bus.Publish(new TestGameplayEvent(12));

            Assert.That(received, Is.EqualTo(12));
            Assert.That(bus.Diagnostics.TotalPublishedCount, Is.EqualTo(1));
        }

        [Test]
        public void UnsubscribeStopsInvocation()
        {
            EventBus bus = new EventBus();
            int calls = 0;
            void Handler(TestGameplayEvent evt) => calls++;

            bus.Subscribe<TestGameplayEvent>(Handler);
            bus.Unsubscribe<TestGameplayEvent>(Handler);
            bus.Publish(new TestGameplayEvent(1));

            Assert.That(calls, Is.EqualTo(0));
            Assert.That(bus.HasSubscribers<TestGameplayEvent>(), Is.False);
        }

        [Test]
        public void SubscriptionDisposeReleasesHandler()
        {
            EventBus bus = new EventBus();
            int calls = 0;

            EventSubscription subscription = bus.Subscribe<TestGameplayEvent>(_ => calls++);
            subscription.Dispose();
            bus.Publish(new TestGameplayEvent(1));

            Assert.That(calls, Is.EqualTo(0));
            Assert.That(subscription.IsDisposed, Is.True);
            Assert.That(bus.HasSubscribers<TestGameplayEvent>(), Is.False);
        }

        [Test]
        public void SubscribeOnceInvokesOnlyOnce()
        {
            EventBus bus = new EventBus();
            int calls = 0;

            bus.SubscribeOnce<TestGameplayEvent>(_ => calls++);
            bus.Publish(new TestGameplayEvent(1));
            bus.Publish(new TestGameplayEvent(2));

            Assert.That(calls, Is.EqualTo(1));
            Assert.That(bus.HasSubscribers<TestGameplayEvent>(), Is.False);
        }

        [Test]
        public void SubscribersRunInRegistrationOrder()
        {
            EventBus bus = new EventBus();
            List<int> order = new List<int>();

            bus.Subscribe<TestGameplayEvent>(_ => order.Add(1));
            bus.Subscribe<TestGameplayEvent>(_ => order.Add(2));
            bus.Subscribe<TestGameplayEvent>(_ => order.Add(3));
            bus.Publish(new TestGameplayEvent(1));

            Assert.That(order, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void DiagnosticsTrackFrequentEventsAndSubscribers()
        {
            EventBus bus = new EventBus();
            bus.Subscribe<TestGameplayEvent>(_ => { });

            bus.Publish(new TestGameplayEvent(1));
            bus.Publish(new TestGameplayEvent(2));
            bus.Publish(new TestUIEvent());

            IReadOnlyList<EventDiagnosticEntry> frequent = bus.Diagnostics.GetMostFrequentEvents(1);

            Assert.That(frequent.Count, Is.EqualTo(1));
            Assert.That(frequent[0].EventName, Is.EqualTo(nameof(TestGameplayEvent)));
            Assert.That(frequent[0].PublishedCount, Is.EqualTo(2));
            Assert.That(frequent[0].SubscriberCount, Is.EqualTo(1));
        }

        [Test]
        public void PublishPerformanceSupportsMobileScale()
        {
            EventBus bus = new EventBus();
            int total = 0;
            bus.Subscribe<TestGameplayEvent>(evt => total += evt.Value);

            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++)
            {
                bus.Publish(new TestGameplayEvent(1));
            }
            stopwatch.Stop();

            Assert.That(total, Is.EqualTo(10000));
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(250));
        }

        private readonly struct TestGameplayEvent : IGameplayEvent
        {
            public int Value { get; }

            public TestGameplayEvent(int value)
            {
                Value = value;
            }
        }

        private readonly struct TestUIEvent : IUIEvent
        {
        }
    }
}
