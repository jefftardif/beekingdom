using BeeKingdom.Builders;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class ResourceDeliveryFrameworkTests
    {
        [Test]
        public void DeliveryCanReserveAssignStartAndComplete()
        {
            ResourceDeliveryManager manager = new ResourceDeliveryManager();
            DeliveryOrder order = manager.CreateDeliveryRequest("construction", DeliveryResourceType.Wax, 10d, 1);

            Assert.That(manager.ReserveResources(order.OrderId, 10d), Is.True);
            Assert.That(manager.AssignTransporters(order.OrderId, 2), Is.True);
            Assert.That(manager.StartDelivery(order.OrderId), Is.True);
            Assert.That(manager.CompleteDelivery(order.OrderId, 10d), Is.True);

            Assert.That(order.State, Is.EqualTo(DeliveryState.Validated));
            Assert.That(manager.Diagnostics.Completed, Is.EqualTo(1));
        }

        [Test]
        public void PartialDeliveryKeepsOrderOpen()
        {
            ResourceDeliveryManager manager = new ResourceDeliveryManager();
            DeliveryOrder order = manager.CreateDeliveryRequest("construction", DeliveryResourceType.Honey, 10d);
            manager.ReserveResources(order.OrderId, 10d);
            manager.AssignTransporters(order.OrderId, 1);
            manager.StartDelivery(order.OrderId);

            manager.CompleteDelivery(order.OrderId, 4d);

            Assert.That(order.DeliveredAmount, Is.EqualTo(4d));
            Assert.That(order.State, Is.EqualTo(DeliveryState.Delivered));
        }

        [Test]
        public void DelaysAndCancellationAreTracked()
        {
            ResourceDeliveryManager manager = new ResourceDeliveryManager();
            DeliveryOrder order = manager.CreateDeliveryRequest("construction", DeliveryResourceType.Pollen, 2d);

            Assert.That(manager.DelayDelivery(order.OrderId), Is.True);
            Assert.That(order.State, Is.EqualTo(DeliveryState.Delayed));
            Assert.That(manager.CancelDelivery(order.OrderId), Is.True);
            Assert.That(order.State, Is.EqualTo(DeliveryState.Cancelled));
        }
    }
}
