using BeeKingdom.Population;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class BeeMemoryFrameworkTests
    {
        [Test]
        public void RememberCreatesMemory()
        {
            BeeMemoryManager manager = CreateManager();

            MemoryEntry memory = manager.Remember("bee-1", "flower", "flower-1", 1d, 0.8d, 0.7d);

            Assert.That(memory, Is.Not.Null);
            Assert.That(manager.QueryMemories("bee-1", MemoryType.ResourceMemory).Count, Is.EqualTo(1));
        }

        [Test]
        public void ReinforceMemoryRaisesImportance()
        {
            BeeMemoryManager manager = CreateManager();
            MemoryEntry memory = manager.Remember("bee-1", "flower", "flower-1", 1d, 0.2d, 0.2d);

            manager.ReinforceMemory("bee-1", memory.MemoryId, 2d);

            Assert.That(memory.Importance, Is.GreaterThan(0.2d));
        }

        [Test]
        public void ExpiredMemoryIsForgotten()
        {
            BeeMemoryManager manager = CreateManager();
            MemoryEntry memory = manager.Remember("bee-1", "flower", "flower-1", 1d, 0.1d, 0.1d);

            manager.UpdateMemory("bee-1", "flower", 10d);

            Assert.That(manager.QueryMemories("bee-1").Count, Is.EqualTo(0));
            Assert.That(manager.Diagnostics.Expired, Is.EqualTo(1));
            Assert.That(memory.IsExpired(10d), Is.True);
        }

        [Test]
        public void BestMemoryUsesImportanceAndConfidence()
        {
            BeeMemoryManager manager = CreateManager();
            manager.Remember("bee-1", "flower", "weak", 1d, 0.2d, 0.2d);
            MemoryEntry strong = manager.Remember("bee-1", "flower", "strong", 1d, 0.9d, 0.9d);

            Assert.That(manager.GetBestMemory("bee-1", MemoryType.ResourceMemory).MemoryId, Is.EqualTo(strong.MemoryId));
        }

        private static BeeMemoryManager CreateManager()
        {
            BeeMemoryManager manager = new BeeMemoryManager();
            manager.RegisterMemoryDefinition(new MemoryDefinition("flower", MemoryType.ResourceMemory, 2d, 0.05d, 0.2d));
            return manager;
        }
    }
}
