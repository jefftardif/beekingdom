using BeeKingdom.Buildings;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class ConstructionValidationEngineTests
    {
        [Test]
        public void ValidateConstructionAggregatesRuleResults()
        {
            ConstructionValidationManager manager = CreateManager();
            ValidationContext context = new ValidationContext("nursery", new PlacementRequest("nursery", new BuildingPosition(0, 0)), resourcesAvailable: false);

            ValidationResult result = manager.ValidateConstruction(context);

            Assert.That(result.Status, Is.EqualTo(ValidationStatus.Blocked));
            Assert.That(result.Issues.Count, Is.EqualTo(1));
            Assert.That(result.Issues[0].Category, Is.EqualTo(ValidationCategory.Resource));
        }

        [Test]
        public void CategoryValidationFiltersRules()
        {
            ConstructionValidationManager manager = CreateManager();
            ValidationContext context = new ValidationContext("nursery", new PlacementRequest("nursery", new BuildingPosition(0, 0)), resourcesAvailable: false, populationAvailable: false);

            ValidationResult result = manager.ValidatePopulation(context);

            Assert.That(result.Status, Is.EqualTo(ValidationStatus.Failed));
            Assert.That(result.Issues.Count, Is.EqualTo(1));
            Assert.That(result.Issues[0].RuleId, Is.EqualTo("population"));
        }

        [Test]
        public void RulesAreQueriedDeterministically()
        {
            ConstructionValidationManager manager = CreateManager();

            Assert.That(manager.QueryValidationRules()[0].RuleId, Is.EqualTo("population"));
            Assert.That(manager.QueryValidationRules()[1].RuleId, Is.EqualTo("resources"));
        }

        private static ConstructionValidationManager CreateManager()
        {
            ConstructionValidationManager manager = new ConstructionValidationManager();
            manager.RegisterRule(new ValidationRule("resources", ValidationCategory.Resource, ValidationStatus.Blocked, "Missing resources.", context => context.ResourcesAvailable));
            manager.RegisterRule(new ValidationRule("population", ValidationCategory.Population, ValidationStatus.Failed, "Missing builders.", context => context.PopulationAvailable));
            return manager;
        }
    }
}
