using BeeKingdom.Buildings;
using System.Collections.Generic;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class BuildingPlacementSystemTests
    {
        [Test]
        public void ValidPlacementCanBeReservedAndConfirmed()
        {
            BuildingRegistry registry = CreateRegistry();
            BuildingPlacementManager manager = new BuildingPlacementManager(registry, new PlacementGrid(8, 8), new PlacementRules());
            PlacementRequest request = new PlacementRequest("nursery", new BuildingPosition(1, 1));

            Assert.That(manager.ReservePlacement(request, 0d, 10d, out PlacementReservation reservation), Is.True);
            Assert.That(manager.ReservationCount, Is.EqualTo(1));
            Assert.That(manager.ConfirmPlacement(reservation.ReservationId), Is.True);
            Assert.That(manager.ReservationCount, Is.EqualTo(0));
        }

        [Test]
        public void CollisionRejectsSecondPlacement()
        {
            BuildingRegistry registry = CreateRegistry();
            BuildingPlacementManager manager = new BuildingPlacementManager(registry, new PlacementGrid(8, 8), new PlacementRules());
            PlacementRequest request = new PlacementRequest("nursery", new BuildingPosition(1, 1));
            manager.ReservePlacement(request, 0d, 10d, out PlacementReservation reservation);
            manager.ConfirmPlacement(reservation.ReservationId);

            PlacementValidationResult result = manager.ValidatePlacement(request);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Status, Is.EqualTo(PlacementValidationStatus.Collision));
        }

        [Test]
        public void CancelAndExpirationFreeReservation()
        {
            BuildingRegistry registry = CreateRegistry();
            BuildingPlacementManager manager = new BuildingPlacementManager(registry, new PlacementGrid(8, 8), new PlacementRules());
            PlacementRequest request = new PlacementRequest("nursery", new BuildingPosition(2, 2));

            manager.ReservePlacement(request, 0d, 1d, out PlacementReservation reservation);
            Assert.That(manager.CancelPlacement(reservation.ReservationId), Is.True);
            Assert.That(manager.ReservePlacement(request, 0d, 1d, out _), Is.True);

            manager.ExpireReservations(2d);
            Assert.That(manager.ReservationCount, Is.EqualTo(0));
        }

        [Test]
        public void PreviewReportsInvalidDepthAndLocationsAreDeterministic()
        {
            BuildingRegistry registry = CreateRegistry();
            BuildingPlacementManager manager = new BuildingPlacementManager(registry, new PlacementGrid(3, 3), new PlacementRules(minDepth: 1, maxDepth: 2));

            PlacementPreview preview = manager.GetPlacementPreview(new PlacementRequest("nursery", new BuildingPosition(0, 0, 0)));
            Assert.That(preview.IsValid, Is.False);
            Assert.That(preview.Status, Is.EqualTo(PlacementValidationStatus.InvalidDepth));

            IReadOnlyList<BuildingPosition> locations = manager.QueryAvailableLocations("nursery", 2);
            Assert.That(locations[0], Is.EqualTo(new BuildingPosition(0, 0)));
            Assert.That(locations[1], Is.EqualTo(new BuildingPosition(1, 0)));
        }

        private static BuildingRegistry CreateRegistry()
        {
            BuildingRegistry registry = new BuildingRegistry();
            registry.RegisterDefinition(new BuildingDefinition("nursery", "Nursery", BuildingCategory.Nursery, new BuildingSize(1, 1)));
            return registry;
        }
    }
}
