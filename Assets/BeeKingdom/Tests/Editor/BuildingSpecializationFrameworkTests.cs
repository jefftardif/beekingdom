using BeeKingdom.Buildings;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class BuildingSpecializationFrameworkTests
    {
        [Test]
        public void ApplyAndQuerySpecialization()
        {
            BuildingSpecializationManager manager = CreateManager();

            Assert.That(manager.ApplySpecialization("building", "production", 1), Is.True);
            Assert.That(manager.QueryCurrentSpecialization("building"), Is.EqualTo(new[] { "production" }));
        }

        [Test]
        public void ExclusiveSpecializationsAreRejected()
        {
            BuildingSpecializationManager manager = CreateManager();
            manager.ApplySpecialization("building", "production", 1);

            Assert.That(manager.ApplySpecialization("building", "storage", 1), Is.False);
        }

        [Test]
        public void RemoveAndResetSpecialization()
        {
            BuildingSpecializationManager manager = CreateManager();
            manager.ApplySpecialization("building", "production", 1);

            Assert.That(manager.RemoveSpecialization("building", "production"), Is.True);
            manager.ApplySpecialization("building", "production", 1);
            Assert.That(manager.ResetSpecialization("building"), Is.True);
            Assert.That(manager.QueryCurrentSpecialization("building"), Is.Empty);
        }

        private static BuildingSpecializationManager CreateManager()
        {
            BuildingSpecializationManager manager = new BuildingSpecializationManager();
            manager.RegisterSpecialization(new SpecializationDefinition("production", SpecializationType.Production));
            manager.RegisterSpecialization(new SpecializationDefinition("storage", SpecializationType.Storage, exclusiveWith: new[] { "production" }));
            return manager;
        }
    }
}
