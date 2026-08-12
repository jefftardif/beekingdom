using BeeKingdom.Buildings;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class BuildingDependencyGraphTests
    {
        [Test]
        public void MissingDependenciesBlockNode()
        {
            BuildingDependencyManager manager = CreateManager();

            Assert.That(manager.ValidateDependencies("nursery"), Is.False);
            Assert.That(manager.GetMissingDependencies("nursery"), Is.EqualTo(new[] { "corridor" }));
        }

        [Test]
        public void SatisfiedDependencyUnlocksNode()
        {
            BuildingDependencyManager manager = CreateManager();

            manager.MarkSatisfied("corridor");

            Assert.That(manager.ValidateDependencies("nursery"), Is.True);
            Assert.That(manager.GetUnlockedBuildings(), Does.Contain("nursery"));
        }

        [Test]
        public void DetectsCycles()
        {
            BuildingDependencyManager manager = new BuildingDependencyManager();
            manager.BuildGraph(new[] { new DependencyNode("a", DependencyEntityType.Building, "a"), new DependencyNode("b", DependencyEntityType.Building, "b") },
                new[] { new DependencyEdge("a", "b", DependencyType.BuildingRequired), new DependencyEdge("b", "a", DependencyType.BuildingRequired) });

            Assert.That(manager.DetectCycles(), Is.True);
        }

        private static BuildingDependencyManager CreateManager()
        {
            BuildingDependencyManager manager = new BuildingDependencyManager();
            manager.BuildGraph(
                new[] { new DependencyNode("corridor", DependencyEntityType.Building, "corridor"), new DependencyNode("nursery", DependencyEntityType.Chamber, "nursery") },
                new[] { new DependencyEdge("corridor", "nursery", DependencyType.BuildingRequired) });
            return manager;
        }
    }
}
