using BeeKingdom.Buildings;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class ConstructionQueueSystemTests
    {
        [Test]
        public void QueueOrdersByPriorityDeterministically()
        {
            ConstructionQueueManager manager = new ConstructionQueueManager();
            ConstructionQueueItem low = manager.EnqueueConstruction("w", "b1", ConstructionPriority.Low);
            ConstructionQueueItem critical = manager.EnqueueConstruction("w", "b2", ConstructionPriority.Critical);

            Assert.That(manager.QueryQueue()[0].ItemId, Is.EqualTo(critical.ItemId));
            Assert.That(manager.QueryQueue()[1].ItemId, Is.EqualTo(low.ItemId));
        }

        [Test]
        public void PromoteAndDemoteChangeOrdering()
        {
            ConstructionQueueManager manager = new ConstructionQueueManager();
            ConstructionQueueItem a = manager.EnqueueConstruction("w", "a", ConstructionPriority.Normal);
            ConstructionQueueItem b = manager.EnqueueConstruction("w", "b", ConstructionPriority.Normal);

            manager.PromoteConstruction(b.ItemId);
            Assert.That(manager.QueryQueue()[0].ItemId, Is.EqualTo(b.ItemId));
            manager.DemoteConstruction(b.ItemId);
            manager.DemoteConstruction(b.ItemId);
            Assert.That(manager.QueryQueue()[0].ItemId, Is.EqualTo(a.ItemId));
        }

        [Test]
        public void DependenciesBlockUntilCompleted()
        {
            ConstructionQueueManager manager = new ConstructionQueueManager();
            ConstructionQueueItem corridor = manager.EnqueueConstruction("corridor", "b1", ConstructionPriority.Normal);
            ConstructionQueueItem nursery = manager.EnqueueConstruction("nursery", "b2", ConstructionPriority.Critical, new[] { corridor.ItemId });

            Assert.That(manager.DequeueConstruction(out ConstructionQueueItem first), Is.True);
            Assert.That(first.ItemId, Is.EqualTo(corridor.ItemId));
            Assert.That(manager.DequeueConstruction(out _), Is.False);

            manager.CompleteConstruction(corridor.ItemId);
            Assert.That(manager.DequeueConstruction(out ConstructionQueueItem second), Is.True);
            Assert.That(second.ItemId, Is.EqualTo(nursery.ItemId));
        }

        [Test]
        public void PauseResumeCancelAreTracked()
        {
            ConstructionQueueManager manager = new ConstructionQueueManager();
            ConstructionQueueItem item = manager.EnqueueConstruction("w", "b", ConstructionPriority.Normal);

            Assert.That(manager.PauseConstruction(item.ItemId), Is.True);
            Assert.That(manager.ResumeConstruction(item.ItemId), Is.True);
            Assert.That(manager.CancelConstruction(item.ItemId), Is.True);

            Assert.That(manager.Count, Is.EqualTo(0));
            Assert.That(manager.Diagnostics.Paused, Is.EqualTo(1));
            Assert.That(manager.Diagnostics.Resumed, Is.EqualTo(1));
            Assert.That(manager.Diagnostics.Cancelled, Is.EqualTo(1));
        }
    }
}
