using System.Collections.Generic;
using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class ColonyFrameworks091To100Tests
    {
        [Test]
        public void AnalyticsRecordsAggregatesAndDetectsTrends()
        {
            HiveAnalyticsManager manager = new HiveAnalyticsManager();
            manager.RegisterMetric(new MetricDefinition("colony-efficiency", MetricDomain.Optimization, 0.2d, 1d));

            manager.RecordMetric("colony-efficiency", 0.4d, 1);
            manager.RecordMetric("colony-efficiency", 0.6d, 2);
            manager.RecordMetric("colony-efficiency", 0.8d, 3);

            MetricAggregate aggregate = manager.AggregateMetrics("colony-efficiency", MetricAggregationPeriod.Minute);
            Assert.That(aggregate.Average, Is.EqualTo(0.6d).Within(0.0001d));
            Assert.That(aggregate.Trend, Is.EqualTo(TrendDirection.Rising));
        }

        [Test]
        public void ForecastProducesRiskFromRegisteredModel()
        {
            ColonyForecastManager manager = new ColonyForecastManager();
            manager.RegisterForecastModel(new ForecastModel(ForecastDomain.Food, "food", -3d, 20d, ColonyRiskType.FoodShortage, 0.8d));

            ForecastPrediction prediction = manager.GenerateForecast(
                new ForecastScenario("current", ForecastScenarioType.Current, 10d),
                new ColonyForecastSnapshot(new Dictionary<string, double> { { "food", 30d } }));

            Assert.That(prediction.Risks.Count, Is.EqualTo(1));
            Assert.That(prediction.Risks[0].Type, Is.EqualTo(ColonyRiskType.FoodShortage));
        }

        [Test]
        public void EventDependenciesGateScheduledEvents()
        {
            ColonyEventManager manager = new ColonyEventManager();
            manager.RegisterEventDefinition(new EventDefinition("first", ColonyEventType.Colony, ColonyEventPriority.Normal));
            manager.RegisterEventDefinition(new EventDefinition("second", ColonyEventType.Colony, ColonyEventPriority.Important, new[] { "first" }));

            EventInstance first = manager.ScheduleEvent("first", 1);
            EventInstance second = manager.ScheduleEvent("second", 1);
            manager.Update(1);

            Assert.That(first.State, Is.EqualTo(ColonyEventState.Triggered));
            Assert.That(second.State, Is.EqualTo(ColonyEventState.Scheduled));

            manager.ResolveEvent(first.EventId);
            manager.Update(2);
            Assert.That(second.State, Is.EqualTo(ColonyEventState.Triggered));
        }

        [Test]
        public void AchievementProgressUnlocksAndGrantsReward()
        {
            AchievementManager manager = new AchievementManager();
            manager.RegisterAchievement(new AchievementDefinition("build-10", AchievementType.Construction, 10d, false, new[] { new AchievementReward(AchievementRewardType.Prestige, "prestige", 5d) }));

            manager.UpdateProgress("build-10", 10d);

            Assert.That(manager.QueryProgress("build-10").IsComplete, Is.True);
            Assert.That(manager.ClaimReward("build-10").Count, Is.EqualTo(1));
        }

        [Test]
        public void ProgressionUnlocksTierContent()
        {
            ColonyProgressionManager manager = new ColonyProgressionManager();
            manager.RegisterProgressionDefinition(new ProgressionDefinition(
                new[] { new ProgressionTier(ProgressionLevel.Settlement, 0d, new[] { "basic-cell" }), new ProgressionTier(ProgressionLevel.GrowingColony, 100d, new[] { "wax-workshop" }) },
                new Dictionary<string, double> { { "construction", 2d } }));

            manager.UpdateProgression("construction", 60d);

            Assert.That(manager.QueryProgression().Level, Is.EqualTo(ProgressionLevel.GrowingColony));
            Assert.That(manager.QueryUnlockedContent(), Does.Contain("wax-workshop"));
        }

        [Test]
        public void PrestigeRecordsHistoryWhenTierChanges()
        {
            ColonyPrestigeManager manager = new ColonyPrestigeManager();
            manager.RegisterPrestigeDefinition(new PrestigeDefinition(
                new[] { new PrestigeTier(PrestigeLevel.UnknownColony, 0d, new string[0]), new PrestigeTier(PrestigeLevel.RenownedColony, 50d, new[] { "renowned-banner" }) },
                new Dictionary<string, double> { { "achievement", 1d } }));

            manager.UpdatePrestige("achievement", 60d);

            Assert.That(manager.QueryPrestige().Level, Is.EqualTo(PrestigeLevel.RenownedColony));
            Assert.That(manager.QueryHistory(), Does.Contain("RenownedColony"));
        }

        [Test]
        public void ScenarioCanLoadStartAndComplete()
        {
            ColonyScenarioManager manager = new ColonyScenarioManager();
            manager.RegisterScenario(new ScenarioDefinition("tutorial", ScenarioType.Tutorial, new[] { new ScenarioObjective("population", "population", 50d) }, new ScenarioCondition[0]));

            manager.StartScenario("tutorial");
            manager.CompleteScenario("tutorial");

            Assert.That(manager.QueryScenario("tutorial").State, Is.EqualTo(ScenarioState.Completed));
        }

        [Test]
        public void SandboxConfigurationIsApplied()
        {
            ColonySandboxManager manager = new ColonySandboxManager();
            SandboxSession session = manager.CreateSandbox(new SandboxDefinition("bench", new[] { SandboxMode.PerformanceBenchmark }, new Dictionary<string, double>()));

            manager.ConfigureSandbox(session.Definition.SandboxId, new SandboxConfigurator().SetOption("speed", 4d));

            Assert.That(manager.QuerySandbox("bench").Configurator.GetOption("speed"), Is.EqualTo(4d));
        }

        [Test]
        public void LivingHiveDemoStarts()
        {
            DemoManager manager = new DemoManager();
            manager.RegisterDemo(DemoDefinition.CreateLivingHive());

            DemoScenario scenario = manager.StartDemo("living-hive");

            Assert.That(scenario.State, Is.EqualTo(DemoState.Running));
            Assert.That(scenario.Definition.Configuration["bees"], Is.EqualTo(50d));
        }

        [Test]
        public void ConstructionGameplayCompletesAndStoresHistory()
        {
            ConstructionGameplayManager manager = new ConstructionGameplayManager();
            ConstructionRequest request = new ConstructionRequest("build-1", "nursery", 10d, 10d, 5d, 1);

            manager.StartConstruction(request);
            ConstructionGameplaySnapshot snapshot = manager.UpdateConstruction("build-1", 5d);

            Assert.That(snapshot.State, Is.EqualTo(ConstructionGameplayState.Completed));
            Assert.That(manager.QueryConstructionHistory().Count, Is.EqualTo(2));
        }
    }
}
