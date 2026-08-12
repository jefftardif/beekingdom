using System.Collections.Generic;
using BeeKingdom.Gameplay.Events;
using NUnit.Framework;

namespace BeeKingdom.Playground.Editor
{
    public sealed class GameEventBusTests
    {
        [Test]
        public void PublishReceivesStronglyTypedEventWithOrderedSubscribers()
        {
            var bus = new GameEventBus();
            var order = new List<int>();
            string building = string.Empty;
            long sequence = 0L;
            bus.Subscribe<BuildingCompleted>((eventData, context) => { order.Add(1); building = eventData.BuildingId; sequence = context.Sequence; });
            bus.Subscribe<BuildingCompleted>((eventData, context) => order.Add(2));

            bus.Publish(new BuildingCompleted("honey_storage", System.Guid.Empty), "test");

            Assert.That(building, Is.EqualTo("honey_storage"));
            Assert.That(sequence, Is.EqualTo(1L));
            Assert.That(order, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void SubscriptionDisposesSafelyAndOnceRunsOnlyOnce()
        {
            var bus = new GameEventBus();
            int calls = 0;
            GameEventSubscription once = bus.SubscribeOnce<RewardGranted>((eventData, context) => calls++);
            once.Dispose();
            once.Dispose();
            bus.Publish(new RewardGranted("honey", 1L, "test"));
            Assert.That(calls, Is.EqualTo(0));

            bus.SubscribeOnce<RewardGranted>((eventData, context) => calls++);
            bus.Publish(new RewardGranted("honey", 1L, "test"));
            bus.Publish(new RewardGranted("honey", 1L, "test"));
            Assert.That(calls, Is.EqualTo(1));
        }
    }
}
