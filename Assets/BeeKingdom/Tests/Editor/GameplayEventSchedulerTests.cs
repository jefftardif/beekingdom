using BeeKingdom.Core.Events;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class GameplayEventSchedulerTests
    {
        [Test]
        public void DelayedEventBecomesDue()
        {
            GameplayEventScheduler scheduler = new GameplayEventScheduler();
            scheduler.Schedule("spawn", 10d);

            Assert.That(scheduler.Tick(9d).Count, Is.EqualTo(0));
            Assert.That(scheduler.Tick(10d).Count, Is.EqualTo(1));
        }

        [Test]
        public void PeriodicEventReschedules()
        {
            GameplayEventScheduler scheduler = new GameplayEventScheduler();
            scheduler.Schedule("regen", 5d, ScheduledGameplayEventType.Periodic, 5d);

            Assert.That(scheduler.Tick(5d).Count, Is.EqualTo(1));
            Assert.That(scheduler.Tick(10d).Count, Is.EqualTo(1));
        }

        [Test]
        public void SnapshotAndRestorePreservesEvents()
        {
            GameplayEventScheduler scheduler = new GameplayEventScheduler();
            scheduler.Schedule("liveops", 100d, ScheduledGameplayEventType.LiveOps);
            GameplayEventSchedulerSnapshot snapshot = scheduler.Snapshot();
            GameplayEventScheduler restored = new GameplayEventScheduler();

            restored.Restore(snapshot);

            Assert.That(restored.Tick(100d).Count, Is.EqualTo(1));
        }
    }
}
