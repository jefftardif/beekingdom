using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Buildings
{
    public enum ExpansionGoal { PopulationGrowth, FoodProduction, StorageCapacity, BroodExpansion, RoyalExpansion, Logistics, Defense, Research, Economy, EmergencyExpansion }

    public sealed class ExpansionPhase
    {
        public string PhaseId { get; }
        public string RecommendedConstruction { get; }
        public double EstimatedCost { get; }
        public double EstimatedDuration { get; }

        public ExpansionPhase(string phaseId, string recommendedConstruction, double estimatedCost, double estimatedDuration)
        {
            PhaseId = phaseId ?? string.Empty;
            RecommendedConstruction = recommendedConstruction ?? string.Empty;
            EstimatedCost = estimatedCost < 0d ? 0d : estimatedCost;
            EstimatedDuration = estimatedDuration < 0d ? 0d : estimatedDuration;
        }
    }

    public sealed class ExpansionPlan
    {
        public string PlanId { get; }
        public ExpansionGoal Goal { get; }
        public int Priority { get; }
        public double EstimatedCost { get; }
        public double EstimatedDuration { get; }
        public IReadOnlyList<string> RequiredBuildings { get; }
        public IReadOnlyList<BuildingResourceCost> RequiredResources { get; }
        public IReadOnlyList<ExpansionPhase> RecommendedOrder { get; }
        public string ExpectedBenefits { get; }

        public ExpansionPlan(string planId, ExpansionGoal goal, int priority, double estimatedCost, double estimatedDuration, IReadOnlyList<string> requiredBuildings, IReadOnlyList<BuildingResourceCost> requiredResources, IReadOnlyList<ExpansionPhase> recommendedOrder, string expectedBenefits)
        {
            PlanId = planId ?? string.Empty;
            Goal = goal;
            Priority = priority;
            EstimatedCost = estimatedCost;
            EstimatedDuration = estimatedDuration;
            RequiredBuildings = requiredBuildings ?? Array.Empty<string>();
            RequiredResources = requiredResources ?? Array.Empty<BuildingResourceCost>();
            RecommendedOrder = recommendedOrder ?? Array.Empty<ExpansionPhase>();
            ExpectedBenefits = expectedBenefits ?? string.Empty;
        }
    }

    public sealed class ExpansionForecast
    {
        public double SaturationDays { get; }
        public int FutureCapacity { get; }
        public double GrowthRisk { get; }
        public ExpansionForecast(double saturationDays, int futureCapacity, double growthRisk)
        {
            SaturationDays = saturationDays;
            FutureCapacity = futureCapacity;
            GrowthRisk = growthRisk;
        }
    }

    public sealed class ExpansionDiagnostics
    {
        public int PlansGenerated { get; private set; }
        public int Evaluations { get; private set; }
        public int Recommendations { get; private set; }
        public int Forecasts { get; private set; }
        public void RecordPlan() => PlansGenerated++;
        public void RecordEvaluation() => Evaluations++;
        public void RecordRecommendation() => Recommendations++;
        public void RecordForecast() => Forecasts++;
    }

    public sealed class ExpansionPlanner
    {
        public ExpansionPlan GenerateExpansionPlan(ExpansionGoal goal, int population, int capacity, double logisticsScore)
        {
            int urgency = capacity <= 0 ? 100 : Math.Max(0, 100 - (capacity - population) * 10);
            if (goal == ExpansionGoal.Logistics) urgency += logisticsScore < 60d ? 50 : 0;
            string construction = goal == ExpansionGoal.StorageCapacity ? "storage-chamber" : goal == ExpansionGoal.Logistics ? "corridor" : "nursery";
            ExpansionPhase phase = new ExpansionPhase("phase-1", construction, 100d + urgency, 60d + urgency);
            return new ExpansionPlan("expansion-" + goal, goal, urgency, phase.EstimatedCost, phase.EstimatedDuration, Array.Empty<string>(), new[] { new BuildingResourceCost("wax", phase.EstimatedCost) }, new[] { phase }, "Improves " + goal);
        }

        public ExpansionForecast PredictCapacity(int population, int capacity, double growthPerDay)
        {
            int remaining = Math.Max(0, capacity - population);
            double days = growthPerDay <= 0d ? double.PositiveInfinity : remaining / growthPerDay;
            return new ExpansionForecast(days, capacity + Math.Max(1, remaining), days < 3d ? 1d : 0d);
        }
    }

    public sealed class HiveExpansionManager
    {
        private readonly ExpansionPlanner planner = new ExpansionPlanner();
        private readonly List<ExpansionPlan> plans = new List<ExpansionPlan>();
        private readonly IEventBus eventBus;

        public ExpansionDiagnostics Diagnostics { get; } = new ExpansionDiagnostics();
        public HiveExpansionManager(IEventBus eventBus = null) { this.eventBus = eventBus; }

        public ExpansionPlan GenerateExpansionPlan(ExpansionGoal goal, int population, int capacity, double logisticsScore)
        {
            ExpansionPlan plan = planner.GenerateExpansionPlan(goal, population, capacity, logisticsScore);
            plans.Add(plan);
            plans.Sort((left, right) => right.Priority.CompareTo(left.Priority));
            Diagnostics.RecordPlan();
            eventBus?.Publish(new ExpansionPlanned(plan.PlanId));
            return plan;
        }

        public double EvaluateExpansion(ExpansionPlan plan)
        {
            Diagnostics.RecordEvaluation();
            return plan == null ? 0d : plan.Priority * 10d - plan.EstimatedCost * 0.01d;
        }

        public IReadOnlyList<ExpansionPlan> QueryExpansionPlans() => new List<ExpansionPlan>(plans);
        public double CalculateExpansionCost(ExpansionPlan plan) => plan?.EstimatedCost ?? 0d;

        public ExpansionForecast PredictCapacity(int population, int capacity, double growthPerDay)
        {
            Diagnostics.RecordForecast();
            ExpansionForecast forecast = planner.PredictCapacity(population, capacity, growthPerDay);
            eventBus?.Publish(new ExpansionForecastUpdated(forecast.SaturationDays));
            return forecast;
        }

        public ExpansionPlan RecommendNextConstruction()
        {
            if (plans.Count == 0) return null;
            Diagnostics.RecordRecommendation();
            eventBus?.Publish(new ExpansionRecommended(plans[0].PlanId));
            return plans[0];
        }

        public void MarkStarted(string planId) => eventBus?.Publish(new ExpansionStarted(planId));
        public void MarkCompleted(string planId) => eventBus?.Publish(new ExpansionCompleted(planId));
    }

    public readonly struct ExpansionPlanned : IGameplayEvent, IBuildingEvent { public string PlanId { get; } public ExpansionPlanned(string planId) { PlanId = planId; } }
    public readonly struct ExpansionRecommended : IGameplayEvent, IBuildingEvent { public string PlanId { get; } public ExpansionRecommended(string planId) { PlanId = planId; } }
    public readonly struct ExpansionStarted : IGameplayEvent, IBuildingEvent { public string PlanId { get; } public ExpansionStarted(string planId) { PlanId = planId; } }
    public readonly struct ExpansionCompleted : IGameplayEvent, IBuildingEvent { public string PlanId { get; } public ExpansionCompleted(string planId) { PlanId = planId; } }
    public readonly struct ExpansionForecastUpdated : IGameplayEvent, IBuildingEvent { public double SaturationDays { get; } public ExpansionForecastUpdated(double saturationDays) { SaturationDays = saturationDays; } }
}
