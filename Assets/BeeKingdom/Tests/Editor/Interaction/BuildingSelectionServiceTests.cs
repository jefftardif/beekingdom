using BeeKingdom.Buildings.Interaction;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor.Interaction
{
    public class BuildingSelectionServiceTests
    {
        private BuildingSelectionService _service;
        private int _clickedCount;
        private int _changedCount;
        private BuildingDefinition _lastSelected;
        private bool _lastIsSelected;

        [SetUp]
        public void SetUp()
        {
            _service = new BuildingSelectionService();
            _clickedCount = 0;
            _changedCount = 0;
            _lastSelected = null;
            _lastIsSelected = false;
            _service.BuildingClicked += delegate { _clickedCount++; };
            _service.SelectionChanged += (args) =>
            {
                _changedCount++;
                _lastSelected = args.Building;
                _lastIsSelected = args.IsSelected;
            };
        }

        [Test]
        public void InitiallyNoSelection()
        {
            Assert.That(_service.HasSelection, Is.False);
            Assert.That(_service.CurrentSelection, Is.Null);
        }

        [Test]
        public void SelectSetsCurrent()
        {
            BuildingDefinition building = BuildingCatalog.GetByBuildingType(BuildingTypes.Nursery);
            _service.Select(building);
            Assert.That(_service.HasSelection, Is.True);
            Assert.That(_service.CurrentSelection, Is.SameAs(building));
            Assert.That(_service.IsSelected(building), Is.True);
            Assert.That(_changedCount, Is.EqualTo(1));
            Assert.That(_lastSelected, Is.SameAs(building));
            Assert.That(_lastIsSelected, Is.True);
        }

        [Test]
        public void DeselectClears()
        {
            BuildingDefinition building = BuildingCatalog.GetByBuildingType(BuildingTypes.Nursery);
            _service.Select(building);
            _service.Deselect();
            Assert.That(_service.HasSelection, Is.False);
            Assert.That(_service.CurrentSelection, Is.Null);
            Assert.That(_service.IsSelected(building), Is.False);
            Assert.That(_changedCount, Is.EqualTo(2));
            Assert.That(_lastIsSelected, Is.False);
        }

        [Test]
        public void SwitchingSelectionKeepsOneCurrent()
        {
            BuildingDefinition a = BuildingCatalog.GetByBuildingType(BuildingTypes.Bank);
            BuildingDefinition b = BuildingCatalog.GetByBuildingType(BuildingTypes.Academy);
            _service.Select(a);
            _service.Select(b);
            Assert.That(_service.CurrentSelection, Is.SameAs(b));
            Assert.That(_service.IsSelected(b), Is.True);
            Assert.That(_service.IsSelected(a), Is.False);
            Assert.That(_changedCount, Is.EqualTo(2));
        }

        [Test]
        public void CyclingBackKeepsOneCurrent()
        {
            BuildingDefinition a = BuildingCatalog.GetByBuildingType(BuildingTypes.Bank);
            BuildingDefinition b = BuildingCatalog.GetByBuildingType(BuildingTypes.Academy);
            _service.Select(a);
            _service.Select(b);
            _service.Select(a);
            Assert.That(_service.CurrentSelection, Is.SameAs(a));
            Assert.That(_service.IsSelected(a), Is.True);
            Assert.That(_service.IsSelected(b), Is.False);
        }

        [Test]
        public void RepeatedSelectionOfSameBuildingStaysSelected()
        {
            BuildingDefinition a = BuildingCatalog.GetByBuildingType(BuildingTypes.Bank);
            _service.Select(a);
            _service.Select(a);
            Assert.That(_service.CurrentSelection, Is.SameAs(a));
            Assert.That(_service.IsSelected(a), Is.True);
            Assert.That(_changedCount, Is.EqualTo(2));
        }

        [Test]
        public void SelectByBuildingType()
        {
            _service.SelectByBuildingType(BuildingTypes.HoneyReserve);
            Assert.That(_service.CurrentSelection.BuildingType, Is.EqualTo(BuildingTypes.HoneyReserve));
        }

        [Test]
        public void SelectByLegacyKey()
        {
            _service.SelectByLegacyKey(BuildingLegacyKeys.AdministrationCore);
            Assert.That(_service.CurrentSelection.LegacyKey, Is.EqualTo(BuildingLegacyKeys.AdministrationCore));
        }

        [Test]
        public void ClickNotificationDoesNotSelect()
        {
            BuildingDefinition building = BuildingCatalog.GetByBuildingType(BuildingTypes.Nursery);
            _service.NotifyClicked(building);
            Assert.That(_clickedCount, Is.EqualTo(1));
            Assert.That(_service.HasSelection, Is.False);
        }

        [Test]
        public void InterfaceContractWorks()
        {
            ISelectionService contract = _service;
            contract.SelectByLegacyKey(BuildingLegacyKeys.WarehouseCells);
            Assert.That(contract.CurrentSelection.BuildingType, Is.EqualTo(BuildingTypes.Warehouse));
            contract.Deselect();
            Assert.That(contract.HasSelection, Is.False);
        }

        [Test]
        public void DeselectWhenNothingSelectedDoesNothing()
        {
            _service.Deselect();
            Assert.That(_changedCount, Is.EqualTo(0));
        }
    }
}