using System.Collections.Generic;
using BeeKingdom.Population;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class GeneticsFrameworkTests
    {
        [Test]
        public void GenerateGenomeIsDeterministic()
        {
            GeneticsManager first = CreateManager();
            GeneticsManager second = CreateManager();

            GenomeInstance left = first.GenerateGenome("bee", "genome-1", 42);
            GenomeInstance right = second.GenerateGenome("bee", "genome-1", 42);

            Assert.That(left.Values["fertility"], Is.EqualTo(right.Values["fertility"]));
            Assert.That(left.Values["longevity"], Is.EqualTo(right.Values["longevity"]));
        }

        [Test]
        public void InheritGenomeCreatesNextGeneration()
        {
            GeneticsManager manager = CreateManager();
            manager.GenerateGenome("bee", "mother", 10);
            manager.GenerateGenome("bee", "father", 20);

            GenomeInstance child = manager.InheritGenome("bee", "child", "mother", "father", 30);

            Assert.That(child, Is.Not.Null);
            Assert.That(child.Generation, Is.EqualTo(1));
            Assert.That(child.Values.ContainsKey("productivity"), Is.True);
        }

        [Test]
        public void MutationsAreRecordedAndBounded()
        {
            GeneticsManager manager = CreateManager();
            manager.GenerateGenome("bee", "genome-1", 42);

            MutationKind mutation = manager.MutateGenome("genome-1", 7);
            IReadOnlyDictionary<string, double> traits = manager.CalculateTraits("genome-1");

            Assert.That(mutation, Is.Not.EqualTo(MutationKind.None));
            Assert.That(traits["fertility"], Is.InRange(0d, 1d));
            Assert.That(manager.QueryGenome("genome-1").MutationHistory.Count, Is.GreaterThan(0));
        }

        [Test]
        public void StatisticsTrackGeneticDiversity()
        {
            GeneticsManager manager = CreateManager();
            manager.GenerateGenome("bee", "genome-1", 1);
            manager.GenerateGenome("bee", "genome-2", 2);

            GeneticsStatistics statistics = manager.QueryGeneticStatistics();

            Assert.That(statistics.GenomeCount, Is.EqualTo(2));
            Assert.That(statistics.Diversity, Is.GreaterThan(0d));
            Assert.That(statistics.AverageTraits.ContainsKey("fertility"), Is.True);
        }

        private static GeneticsManager CreateManager()
        {
            GeneticsManager manager = new GeneticsManager();
            manager.RegisterDefinition(new GenomeDefinition(
                "bee",
                new[]
                {
                    new GeneticTrait("fertility", GeneticTraitKind.Fertility, 0d, 1d, 0.7d, 1d, 0.05d),
                    new GeneticTrait("longevity", GeneticTraitKind.Longevity, 0d, 1d, 0.5d, 1d, 0.04d),
                    new GeneticTrait("productivity", GeneticTraitKind.Productivity, 0d, 1d, 0.6d, 1d, 0.03d)
                },
                0.5d,
                0.05d));
            return manager;
        }
    }
}
