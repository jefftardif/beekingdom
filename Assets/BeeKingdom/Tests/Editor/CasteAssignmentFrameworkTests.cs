using BeeKingdom.Population;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class CasteAssignmentFrameworkTests
    {
        [Test]
        public void AssignCasteUsesGeneticsAndUpdatesPopulation()
        {
            CasteAssignmentManager manager = CreateManager(out PopulationManager population, out GeneticsManager genetics);
            genetics.GenerateGenome("bee", "genome-1", 5);

            BeeCaste caste = manager.AssignCaste(new CasteAssignmentContext("bee-1", "genome-1", 12d));

            Assert.That(caste, Is.EqualTo(BeeCaste.Forager));
            Assert.That(population.QueryByCaste(BeeCaste.Forager).Count, Is.EqualTo(1));
        }

        [Test]
        public void ReassignCasteKeepsPopulationIndexesCoherent()
        {
            CasteAssignmentManager manager = CreateManager(out PopulationManager population, out _);

            Assert.That(manager.ReassignCaste("bee-1", BeeCaste.Guard), Is.True);

            Assert.That(population.QueryByCaste(BeeCaste.Worker).Count, Is.EqualTo(0));
            Assert.That(population.QueryByCaste(BeeCaste.Guard).Count, Is.EqualTo(1));
        }

        [Test]
        public void PopulationNeedsDetectMissingCaste()
        {
            CasteAssignmentManager manager = CreateManager(out _, out _);

            PopulationBalance balance = manager.EvaluatePopulationNeeds();

            Assert.That(balance.IsBalanced, Is.False);
            Assert.That(balance.MostNeededCaste, Is.EqualTo(BeeCaste.Nurse));
        }

        private static CasteAssignmentManager CreateManager(out PopulationManager population, out GeneticsManager genetics)
        {
            population = new PopulationManager();
            population.RegisterDefinition(new PopulationDefinition("worker", BeeCaste.Worker, 30d, 1d));
            population.RegisterBee(new BeePopulationRecord("bee-1", "worker", BeeCaste.Worker, 10d));

            genetics = new GeneticsManager();
            genetics.RegisterDefinition(new GenomeDefinition(
                "bee",
                new[] { new GeneticTrait("navigation", GeneticTraitKind.Navigation, 1d, 1d, 1d, 0d, 0d) },
                0d,
                0d));

            CasteAssignmentManager manager = new CasteAssignmentManager(population, genetics);
            manager.RegisterCasteDefinition(new CasteDefinition("nurse", BeeCaste.Nurse, 0.5d, 1));
            manager.RegisterCasteDefinition(new CasteDefinition("forager", BeeCaste.Forager, 0.25d, 0));
            manager.RegisterCasteDefinition(new CasteDefinition("guard", BeeCaste.Guard, 0.25d, 0));
            manager.RegisterAssignmentRule(new CasteAssignmentRule("forager-navigation", BeeCaste.Forager, "navigation", 1d, 0d, 0d, 0d, 0d));
            manager.RegisterAssignmentRule(new CasteAssignmentRule("nurse-need", BeeCaste.Nurse, "care", 0d, 0d, 0d, 1d, 0d));
            return manager;
        }
    }
}
