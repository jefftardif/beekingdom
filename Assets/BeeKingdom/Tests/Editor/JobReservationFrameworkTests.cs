using BeeKingdom.Population;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class JobReservationFrameworkTests
    {
        [Test]
        public void ReservationPreventsLowerPriorityConflict()
        {
            JobReservationManager manager = new JobReservationManager();
            ReservationTicket first = manager.RequestReservation("bee-1", "flower-1", ReservationType.Resource, 0d, 5d, 1);
            ReservationTicket second = manager.RequestReservation("bee-2", "flower-1", ReservationType.Resource, 0d, 5d, 0);

            Assert.That(first.State, Is.EqualTo(ReservationState.Reserved));
            Assert.That(second.State, Is.EqualTo(ReservationState.Failed));
        }

        [Test]
        public void HigherPriorityPreemptsReservation()
        {
            JobReservationManager manager = new JobReservationManager();
            manager.RequestReservation("bee-1", "flower-1", ReservationType.Resource, 0d, 5d, 1);
            ReservationTicket second = manager.RequestReservation("bee-2", "flower-1", ReservationType.Resource, 0d, 5d, 2);

            Assert.That(second.State, Is.EqualTo(ReservationState.Reserved));
            Assert.That(manager.QueryReservations().Count, Is.EqualTo(1));
        }

        [Test]
        public void ExpirationRemovesReservation()
        {
            JobReservationManager manager = new JobReservationManager();
            manager.RequestReservation("bee-1", "flower-1", ReservationType.Resource, 0d, 1d, 1);

            manager.ExpireReservations(2d);

            Assert.That(manager.QueryReservations().Count, Is.EqualTo(0));
            Assert.That(manager.Diagnostics.Expired, Is.EqualTo(1));
        }

        [Test]
        public void TransferChangesOwner()
        {
            JobReservationManager manager = new JobReservationManager();
            ReservationTicket ticket = manager.RequestReservation("bee-1", "task-1", ReservationType.Task, 0d, 5d, 1);

            Assert.That(manager.TransferReservation(ticket.ReservationId, "bee-2"), Is.True);
            Assert.That(ticket.OwnerId, Is.EqualTo("bee-2"));
        }
    }
}
