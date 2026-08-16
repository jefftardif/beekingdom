using BeeKingdom.Buildings.Interaction;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor.Interaction
{
    public class BuildingWindowContractTests
    {
        private sealed class RecordingHost : IBuildingWindowHost
        {
            public BuildingWindowContext LastContext;
            public int OpenCount;
            public int CloseCount;
            public bool _isOpen;

            public bool IsOpen { get { return _isOpen; } }

            public void Open(BuildingWindowContext context)
            {
                LastContext = context;
                OpenCount++;
                _isOpen = true;
            }

            public void Close()
            {
                CloseCount++;
                _isOpen = false;
            }
        }

        [Test]
        public void OpenBuildsContextWithAllFields()
        {
            BuildingDefinition building = BuildingCatalog.GetByBuildingType(BuildingTypes.HoneyReserve);
            BuildingWindowContext context = new BuildingWindowContext(building);
            Assert.That(context.BuildingType, Is.EqualTo(BuildingTypes.HoneyReserve));
            Assert.That(context.LegacyKey, Is.EqualTo(BuildingLegacyKeys.HoneyStorage));
            Assert.That(context.DisplayName, Is.EqualTo("Reserve miel"));
            Assert.That(context.Role, Is.EqualTo("Stockage et lecture des reserves"));
            Assert.That(context.ZoneNumber, Is.EqualTo(2));
            Assert.That(context.State, Is.EqualTo(BuildingState.Preview));
            Assert.That(context.Capabilities, Is.EqualTo(BuildingCapabilities.Production | BuildingCapabilities.Upgrade));
            Assert.That(context.ProductionResource, Is.EqualTo(BuildingResource.Honey));
            Assert.That(context.IsUpgradable, Is.True);
        }

        [Test]
        public void CloseBuildsContextForFutureBuilding()
        {
            BuildingDefinition building = BuildingCatalog.GetByBuildingType(BuildingTypes.ChampionHall);
            BuildingWindowContext context = new BuildingWindowContext(building);
            Assert.That(context.State, Is.EqualTo(BuildingState.Future));
            Assert.That(context.Capabilities, Is.EqualTo(BuildingCapabilities.None));
            Assert.That(context.IsUpgradable, Is.False);
        }

        [Test]
        public void RouterWithoutHostDoesNotOpen()
        {
            IBuildingWindowHost previous = BuildingWindowRouter.Host;
            try
            {
                BuildingWindowRouter.Host = null;
                BuildingDefinition building = BuildingCatalog.GetByBuildingType(BuildingTypes.Nursery);
                Assert.That(BuildingWindowRouter.TryOpen(building), Is.False);
                Assert.That(BuildingWindowRouter.TryClose(), Is.False);
            }
            finally
            {
                BuildingWindowRouter.Host = previous;
            }
        }

        [Test]
        public void RouterWithHostOpensAndCloses()
        {
            IBuildingWindowHost previous = BuildingWindowRouter.Host;
            RecordingHost host = new RecordingHost();
            try
            {
                BuildingWindowRouter.Host = host;
                BuildingDefinition building = BuildingCatalog.GetByBuildingType(BuildingTypes.Bank);
                Assert.That(BuildingWindowRouter.TryOpen(building), Is.True);
                Assert.That(host.OpenCount, Is.EqualTo(1));
                Assert.That(host.LastContext.BuildingType, Is.EqualTo(BuildingTypes.Bank));
                Assert.That(host.IsOpen, Is.True);
                Assert.That(BuildingWindowRouter.TryClose(), Is.True);
                Assert.That(host.CloseCount, Is.EqualTo(1));
                Assert.That(host.IsOpen, Is.False);
            }
            finally
            {
                BuildingWindowRouter.Host = previous;
            }
        }
    }
}