using System;
using NUnit.Framework;

namespace BeeKingdom.BeeQA
{
    public sealed class BeeQATests
    {
        [Test]
        public void CatalogContainsAllPlannedCategories()
        {
            Assert.That(BeeQACatalog.Categories.Count, Is.EqualTo(18));
            Assert.That(BeeQAEntryPoint.IsDebugAvailable, Is.True);
        }

        [Test]
        public void EmptyRegistryAcceptsIndependentModulesWithoutDuplicates()
        {
            var first = new TestModule("test.module", BeeQACategory.Gameplay);
            Assert.That(BeeQAModuleRegistry.Register(first), Is.True);
            Assert.That(BeeQAModuleRegistry.Register(new TestModule("test.module", BeeQACategory.Gameplay)), Is.False);
            Assert.That(BeeQAModuleRegistry.CountFor(BeeQACategory.Gameplay), Is.EqualTo(1));
        }

        [Test]
        public void SmokeModuleIsDiscoveredAndPasses()
        {
            BeeQAModuleRegistry.RefreshDiscovery();
            IBeeQAModule smoke = null;
            for (int i = 0; i < BeeQAModuleRegistry.Modules.Count; i++)
            {
                if (BeeQAModuleRegistry.Modules[i].Id == "beeqa.smoke")
                {
                    smoke = BeeQAModuleRegistry.Modules[i];
                    break;
                }
            }

            Assert.That(smoke, Is.Not.Null);
            BeeQAResult result = BeeQAModuleRegistry.Run(smoke);
            Assert.That(result.Passed, Is.True);
            Assert.That(result.Status, Is.EqualTo("PASS"));
        }

        [Test]
        public void AllOfficialModulesAreDiscoveredAndPass()
        {
            BeeQAModuleRegistry.RefreshDiscovery();
            int officialCount = 0;
            for (int i = 0; i < BeeQAModuleRegistry.Modules.Count; i++)
            {
                IBeeQAModule module = BeeQAModuleRegistry.Modules[i];
                if (!module.Id.StartsWith("beeqa.", System.StringComparison.Ordinal)) continue;
                officialCount++;
                Assert.That(BeeQAModuleRegistry.Run(module).Passed, Is.True, module.Id);
            }
            Assert.That(officialCount, Is.GreaterThanOrEqualTo(3));
        }

        private sealed class TestModule : IBeeQAModule
        {
            public string Id { get; }
            public string DisplayName => Id;
            public string Description => "Test module";
            public string Version => "test";
            public string Author => "Tests";
            public BeeQACategory Category { get; }
            public BeeQAModuleStatus Status => BeeQAModuleStatus.Ready;
            public bool CanExecute => true;
            public BeeQAResult LastResult => null;

            public BeeQAResult Execute()
            {
                return new BeeQAResult(true, 0d, DateTime.UtcNow, "Test");
            }

            public TestModule(string id, BeeQACategory category)
            {
                Id = id;
                Category = category;
            }
        }
    }
}
