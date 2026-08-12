using BeeKingdom.Chambers;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class ChamberCategorySystemTests
    {
        [Test]
        public void RegistersBaseCategories()
        {
            ChamberCategoryManager manager = new ChamberCategoryManager();
            foreach (ChamberCategoryDefinition definition in ChamberCategoryCatalog.CreateBaseDefinitions())
            {
                manager.RegisterCategory(definition);
            }

            Assert.That(manager.QueryCategories().Count, Is.EqualTo(20));
            Assert.That(manager.GetCategory("Nursery", out _), Is.True);
        }

        [Test]
        public void AssignRemoveAndValidateCategory()
        {
            ChamberCategoryManager manager = new ChamberCategoryManager();
            manager.RegisterCategory(new ChamberCategoryDefinition("Nursery"));

            Assert.That(manager.AssignCategory("chamber", "Nursery"), Is.True);
            Assert.That(manager.ValidateCategory("chamber"), Is.True);
            Assert.That(manager.RemoveCategory("chamber", "Nursery"), Is.True);
            Assert.That(manager.Diagnostics.Assigned, Is.EqualTo(1));
            Assert.That(manager.Diagnostics.Removed, Is.EqualTo(1));
        }

        [Test]
        public void IncompatibleCategoriesFailValidation()
        {
            ChamberCategoryManager manager = new ChamberCategoryManager();
            manager.RegisterCategory(new ChamberCategoryDefinition("Waste", incompatibleCategories: new[] { "Nursery" }));
            manager.RegisterCategory(new ChamberCategoryDefinition("Nursery"));
            manager.AssignCategory("chamber", "Waste");
            manager.AssignCategory("chamber", "Nursery");

            Assert.That(manager.ValidateCategory("chamber"), Is.False);
            Assert.That(manager.Diagnostics.Invalid, Is.EqualTo(1));
        }

        [Test]
        public void QueryCompatibleCategoriesIsDeterministic()
        {
            ChamberCategoryManager manager = new ChamberCategoryManager();
            manager.RegisterCategory(new ChamberCategoryDefinition("A", compatibleCategories: new[] { "B" }));
            manager.RegisterCategory(new ChamberCategoryDefinition("C", incompatibleCategories: new[] { "A" }));
            manager.RegisterCategory(new ChamberCategoryDefinition("B"));

            var compatible = manager.QueryCompatibleCategories("A");

            Assert.That(compatible.Count, Is.EqualTo(1));
            Assert.That(compatible[0].CategoryId, Is.EqualTo("B"));
        }
    }
}
