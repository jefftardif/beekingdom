using BeeKingdom.Chambers;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class StructuralIntegrityEngineTests
    {
        [Test]
        public void StableStructureProducesHighScore()
        {
            StructuralIntegrityManager manager = new StructuralIntegrityManager();
            manager.BuildGraph(new[] { new StructuralNode("a", StructuralSupportType.ReinforcedWax, 5d, 20d, 1) });

            StructuralIntegrityResult result = manager.AnalyzeIntegrity();

            Assert.That(result.Score, Is.GreaterThan(75d));
            Assert.That(manager.QueryWeakZones(), Is.Empty);
        }

        [Test]
        public void WeakZonesRecommendReinforcement()
        {
            StructuralIntegrityManager manager = new StructuralIntegrityManager();
            manager.BuildGraph(new[] { new StructuralNode("weak", StructuralSupportType.WaxStructure, 20d, 5d, 3) });

            Assert.That(manager.QueryWeakZones(), Is.EqualTo(new[] { "weak" }));
            Assert.That(manager.RecommendReinforcements(), Is.EqualTo(new[] { "weak" }));
        }

        [Test]
        public void ValidateExpansionRejectsUnsupportedNode()
        {
            StructuralIntegrityManager manager = new StructuralIntegrityManager();

            Assert.That(manager.ValidateExpansion(new StructuralNode("x", StructuralSupportType.WaxStructure, 10d, 2d, 1)), Is.False);
        }
    }
}
