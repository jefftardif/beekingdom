using BeeKingdom.Buildings;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class BuildingFrameworkTests
    {
        [Test]
        public void RegisteredDefinitionCanCreateBuilding()
        {
            BuildingManager manager = new BuildingManager();
            manager.RegisterDefinition(CreateDefinition("nursery", BuildingCategory.Nursery, "bee:nursery"));

            BuildingInstance building = manager.CreateBuilding("nursery", new BuildingPosition(2, 3, 1), 450, "hive");

            Assert.That(building, Is.Not.Null);
            Assert.That(building.Rotation, Is.EqualTo(90));
            Assert.That(manager.BuildingCount, Is.EqualTo(1));
            Assert.That(manager.QueryByCategory(BuildingCategory.Nursery).Count, Is.EqualTo(1));
            Assert.That(manager.QueryByTag("bee:nursery").Count, Is.EqualTo(1));
        }

        [Test]
        public void DestroyBuildingRemovesInstance()
        {
            BuildingManager manager = new BuildingManager();
            manager.RegisterDefinition(CreateDefinition("storage", BuildingCategory.Storage, "bee:storage"));
            BuildingInstance building = manager.CreateBuilding("storage", new BuildingPosition(0, 0), 0, "hive");

            Assert.That(manager.DestroyBuilding(building.EntityId), Is.True);
            Assert.That(manager.TryGetBuilding(building.EntityId, out _), Is.False);
            Assert.That(manager.Diagnostics.DestroyedBuildings, Is.EqualTo(1));
        }

        [Test]
        public void SnapshotRestoresBuildingsDeterministically()
        {
            BuildingManager manager = new BuildingManager();
            manager.RegisterDefinition(CreateDefinition("entrance", BuildingCategory.Entrance, "bee:entry"));
            BuildingInstance building = manager.CreateBuilding("entrance", new BuildingPosition(4, 5), 180, "hive");
            building.ChangeState(BuildingState.Reserved);
            BuildingSnapshot snapshot = manager.Snapshot();

            BuildingManager restored = new BuildingManager();
            restored.RegisterDefinition(CreateDefinition("entrance", BuildingCategory.Entrance, "bee:entry"));
            restored.RestoreSnapshot(snapshot);

            Assert.That(restored.TryGetBuilding(building.EntityId, out BuildingInstance loaded), Is.True);
            Assert.That(loaded.DefinitionId, Is.EqualTo("entrance"));
            Assert.That(loaded.Position, Is.EqualTo(new BuildingPosition(4, 5)));
        }

        [Test]
        public void RegistryScalesWithManyDefinitions()
        {
            BuildingManager manager = new BuildingManager();
            for (int i = 0; i < 10000; i++)
            {
                Assert.That(manager.RegisterDefinition(CreateDefinition("b" + i, BuildingCategory.Utility, "tag")), Is.True);
            }

            Assert.That(manager.Diagnostics.RegisteredDefinitions, Is.EqualTo(10000));
        }

        private static BuildingDefinition CreateDefinition(string id, BuildingCategory category, string tag)
        {
            return new BuildingDefinition(id, id, category, new BuildingSize(1, 1), gameplayTags: new[] { tag });
        }
    }
}
