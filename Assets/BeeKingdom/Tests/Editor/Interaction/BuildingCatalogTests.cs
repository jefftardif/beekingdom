using BeeKingdom.Buildings.Interaction;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor.Interaction
{
    public class BuildingCatalogTests
    {
        [Test]
        public void CatalogHasFourteenDefinitions()
        {
            Assert.That(BuildingCatalog.All.Count, Is.EqualTo(14));
        }

        [Test]
        public void EveryBuildingTypeResolvable()
        {
            for (int i = 0; i < BuildingTypes.All.Length; i++)
            {
                BuildingDefinition definition = BuildingCatalog.GetByBuildingType(BuildingTypes.All[i]);
                Assert.That(definition, Is.Not.Null, "Définition manquante : " + BuildingTypes.All[i]);
                Assert.That(definition.BuildingType, Is.EqualTo(BuildingTypes.All[i]));
            }
        }

        [Test]
        public void EveryLegacyKeyResolvable()
        {
            for (int i = 0; i < BuildingLegacyKeys.All.Length; i++)
            {
                BuildingDefinition definition = BuildingCatalog.GetByLegacyKey(BuildingLegacyKeys.All[i]);
                Assert.That(definition, Is.Not.Null, "Définition manquante : " + BuildingLegacyKeys.All[i]);
                Assert.That(definition.LegacyKey, Is.EqualTo(BuildingLegacyKeys.All[i]));
            }
        }

        [Test]
        public void ArchivesMapsToChampionHall()
        {
            BuildingDefinition definition = BuildingCatalog.GetByLegacyKey(BuildingLegacyKeys.ArchivesHoneyfall);
            Assert.That(definition.BuildingType, Is.EqualTo(BuildingTypes.ChampionHall));
        }

        [Test]
        public void FutureBuildingsAreMarkedFuture()
        {
            string[] future = { BuildingTypes.Defense, BuildingTypes.Genetics,
                BuildingTypes.Infirmary, BuildingTypes.Academy, BuildingTypes.Bank,
                BuildingTypes.AllianceCenter, BuildingTypes.ChampionHall };
            for (int i = 0; i < future.Length; i++)
            {
                BuildingDefinition definition = BuildingCatalog.GetByBuildingType(future[i]);
                Assert.That(definition.State, Is.EqualTo(BuildingState.Future), future[i] + " doit être Future");
                Assert.That(definition.StateIsFuture, Is.True, future[i] + " StateIsFuture doit être true");
            }
        }

        [Test]
        public void FutureBuildingsHaveNoFakeCapabilities()
        {
            string[] future = { BuildingTypes.Defense, BuildingTypes.Genetics,
                BuildingTypes.Infirmary, BuildingTypes.Academy, BuildingTypes.Bank,
                BuildingTypes.AllianceCenter, BuildingTypes.ChampionHall };
            for (int i = 0; i < future.Length; i++)
            {
                BuildingDefinition definition = BuildingCatalog.GetByBuildingType(future[i]);
                Assert.That(definition.Capabilities, Is.EqualTo(BuildingCapabilities.None),
                    future[i] + " ne doit pas avoir de capacités inventées");
            }
        }

        [Test]
        public void UpgradeableBuildingsAreUpgradable()
        {
            string[] upgradeable = { BuildingTypes.HoneyReserve, BuildingTypes.Warehouse,
                BuildingTypes.Transformation, BuildingTypes.RoyalPalace };
            for (int i = 0; i < upgradeable.Length; i++)
            {
                BuildingDefinition definition = BuildingCatalog.GetByBuildingType(upgradeable[i]);
                Assert.That((definition.Capabilities & BuildingCapabilities.Upgrade) != 0, Is.True,
                    upgradeable[i] + " doit être upgradeable");
            }
        }

        [Test]
        public void HoneyReserveProducesHoney()
        {
            BuildingDefinition definition = BuildingCatalog.GetByBuildingType(BuildingTypes.HoneyReserve);
            Assert.That(definition.ProductionResource, Is.EqualTo(BuildingResource.Honey));
            Assert.That((definition.Capabilities & BuildingCapabilities.Production) != 0, Is.True);
        }

        [Test]
        public void TransformationProducesWax()
        {
            BuildingDefinition definition = BuildingCatalog.GetByBuildingType(BuildingTypes.Transformation);
            Assert.That(definition.ProductionResource, Is.EqualTo(BuildingResource.Wax));
        }

        [Test]
        public void WarehouseProducesPollen()
        {
            BuildingDefinition definition = BuildingCatalog.GetByBuildingType(BuildingTypes.Warehouse);
            Assert.That(definition.ProductionResource, Is.EqualTo(BuildingResource.Pollen));
        }

        [Test]
        public void ResearchHasResearchCapability()
        {
            BuildingDefinition definition = BuildingCatalog.GetByBuildingType(BuildingTypes.Research);
            Assert.That((definition.Capabilities & BuildingCapabilities.Research) != 0, Is.True);
        }

        [Test]
        public void MetadataExposed()
        {
            BuildingDefinition definition = BuildingCatalog.GetByBuildingType(BuildingTypes.HoneyReserve);
            Assert.That(definition.DisplayName, Is.EqualTo("Reserve miel"));
            Assert.That(definition.ZoneNumber, Is.EqualTo(2));
            Assert.That(definition.CellId, Is.EqualTo("cell-0-0"));
            Assert.That(definition.IconId, Is.EqualTo("honey"));
            Assert.That(definition.ActionLabel, Is.EqualTo("Ameliorer reserve"));
            Assert.That(string.IsNullOrEmpty(definition.Disclosure), Is.False);
        }

        [Test]
        public void MetadataLookupByLegacyKey()
        {
            LivingHiveHotspotMetadata metadata = BuildingCatalog.GetMetadata(BuildingLegacyKeys.AdministrationCore);
            Assert.That(metadata.Label, Is.EqualTo("Administration"));
            Assert.That(metadata.StateIcon, Is.EqualTo("active"));
            Assert.That(metadata.Priority, Is.EqualTo(45));
        }

        [Test]
        public void TryGetByBuildingType()
        {
            BuildingDefinition definition;
            Assert.That(BuildingCatalog.TryGetByBuildingType(BuildingTypes.Nursery, out definition), Is.True);
            Assert.That(definition, Is.Not.Null);
            Assert.That(BuildingCatalog.TryGetByBuildingType("INCONNU", out definition), Is.False);
        }

        [Test]
        public void TryGetByLegacyKey()
        {
            BuildingDefinition definition;
            Assert.That(BuildingCatalog.TryGetByLegacyKey(BuildingLegacyKeys.NurseryCluster, out definition), Is.True);
            Assert.That(definition, Is.Not.Null);
            Assert.That(BuildingCatalog.TryGetByLegacyKey("inconnu", out definition), Is.False);
        }
    }
}