using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Buildings
{
    public enum ValidationCategory { Placement, Dependency, Technology, Resource, Population, World }
    public enum ValidationStatus { Success, Warning, Failed, Blocked }

    public sealed class ValidationContext
    {
        public string BuildingId { get; }
        public PlacementRequest PlacementRequest { get; }
        public bool DependenciesSatisfied { get; }
        public bool TechnologiesSatisfied { get; }
        public bool ResourcesAvailable { get; }
        public bool PopulationAvailable { get; }
        public bool WorldAllowsConstruction { get; }

        public ValidationContext(string buildingId, PlacementRequest placementRequest, bool dependenciesSatisfied = true, bool technologiesSatisfied = true, bool resourcesAvailable = true, bool populationAvailable = true, bool worldAllowsConstruction = true)
        {
            BuildingId = buildingId ?? string.Empty;
            PlacementRequest = placementRequest;
            DependenciesSatisfied = dependenciesSatisfied;
            TechnologiesSatisfied = technologiesSatisfied;
            ResourcesAvailable = resourcesAvailable;
            PopulationAvailable = populationAvailable;
            WorldAllowsConstruction = worldAllowsConstruction;
        }
    }

    public sealed class ValidationIssue
    {
        public string RuleId { get; }
        public ValidationCategory Category { get; }
        public ValidationStatus Status { get; }
        public string Cause { get; }

        public ValidationIssue(string ruleId, ValidationCategory category, ValidationStatus status, string cause)
        {
            RuleId = ruleId ?? string.Empty;
            Category = category;
            Status = status;
            Cause = cause ?? string.Empty;
        }
    }

    public sealed class ValidationResult
    {
        public ValidationStatus Status { get; }
        public IReadOnlyList<ValidationIssue> Issues { get; }
        public bool IsSuccess => Status == ValidationStatus.Success || Status == ValidationStatus.Warning;

        public ValidationResult(ValidationStatus status, IReadOnlyList<ValidationIssue> issues)
        {
            Status = status;
            Issues = issues ?? Array.Empty<ValidationIssue>();
        }
    }

    public sealed class ValidationRule
    {
        private readonly Func<ValidationContext, bool> predicate;

        public string RuleId { get; }
        public ValidationCategory Category { get; }
        public ValidationStatus FailureStatus { get; }
        public string Cause { get; }

        public ValidationRule(string ruleId, ValidationCategory category, ValidationStatus failureStatus, string cause, Func<ValidationContext, bool> predicate)
        {
            RuleId = string.IsNullOrWhiteSpace(ruleId) ? throw new ArgumentException("Rule id is required.", nameof(ruleId)) : ruleId;
            Category = category;
            FailureStatus = failureStatus;
            Cause = cause ?? string.Empty;
            this.predicate = predicate ?? (_ => true);
        }

        public bool Evaluate(ValidationContext context) => predicate(context);
    }

    public sealed class ValidationDiagnostics
    {
        public int RulesRegistered { get; private set; }
        public int Validations { get; private set; }
        public int Failures { get; private set; }
        public int Warnings { get; private set; }
        public int RulesTriggered { get; private set; }

        public void RecordRules(int count) => RulesRegistered = count;
        public void RecordValidation(ValidationResult result)
        {
            Validations++;
            if (result.Status == ValidationStatus.Failed || result.Status == ValidationStatus.Blocked) Failures++;
            if (result.Status == ValidationStatus.Warning) Warnings++;
            RulesTriggered += result.Issues.Count;
        }
    }

    public sealed class ConstructionValidationEngine
    {
        private readonly List<ValidationRule> rules = new List<ValidationRule>();

        public int RuleCount => rules.Count;

        public bool RegisterRule(ValidationRule rule)
        {
            if (rule == null) return false;
            for (int i = 0; i < rules.Count; i++)
            {
                if (rules[i].RuleId == rule.RuleId) return false;
            }

            rules.Add(rule);
            rules.Sort((left, right) => string.CompareOrdinal(left.RuleId, right.RuleId));
            return true;
        }

        public ValidationResult Validate(ValidationContext context, ValidationCategory? category = null)
        {
            List<ValidationIssue> issues = new List<ValidationIssue>();
            ValidationStatus status = ValidationStatus.Success;

            for (int i = 0; i < rules.Count; i++)
            {
                ValidationRule rule = rules[i];
                if (category.HasValue && rule.Category != category.Value) continue;
                if (rule.Evaluate(context)) continue;

                issues.Add(new ValidationIssue(rule.RuleId, rule.Category, rule.FailureStatus, rule.Cause));
                status = Merge(status, rule.FailureStatus);
            }

            return new ValidationResult(status, issues);
        }

        public IReadOnlyList<ValidationRule> QueryValidationRules()
        {
            return new List<ValidationRule>(rules);
        }

        private static ValidationStatus Merge(ValidationStatus current, ValidationStatus next)
        {
            if (next == ValidationStatus.Blocked || current == ValidationStatus.Blocked) return ValidationStatus.Blocked;
            if (next == ValidationStatus.Failed || current == ValidationStatus.Failed) return ValidationStatus.Failed;
            if (next == ValidationStatus.Warning || current == ValidationStatus.Warning) return ValidationStatus.Warning;
            return ValidationStatus.Success;
        }
    }

    public sealed class ConstructionValidationManager
    {
        private readonly ConstructionValidationEngine engine = new ConstructionValidationEngine();
        private readonly IEventBus eventBus;

        public ValidationDiagnostics Diagnostics { get; } = new ValidationDiagnostics();

        public ConstructionValidationManager(IEventBus eventBus = null)
        {
            this.eventBus = eventBus;
        }

        public bool RegisterRule(ValidationRule rule)
        {
            bool registered = engine.RegisterRule(rule);
            if (registered) Diagnostics.RecordRules(engine.RuleCount);
            return registered;
        }

        public ValidationResult ValidateConstruction(ValidationContext context) => Validate(context, null);
        public ValidationResult ValidatePlacement(ValidationContext context) => Validate(context, ValidationCategory.Placement);
        public ValidationResult ValidateDependencies(ValidationContext context) => Validate(context, ValidationCategory.Dependency);
        public ValidationResult ValidateResources(ValidationContext context) => Validate(context, ValidationCategory.Resource);
        public ValidationResult ValidatePopulation(ValidationContext context) => Validate(context, ValidationCategory.Population);
        public ValidationResult ValidateWorld(ValidationContext context) => Validate(context, ValidationCategory.World);
        public IReadOnlyList<ValidationRule> QueryValidationRules() => engine.QueryValidationRules();

        private ValidationResult Validate(ValidationContext context, ValidationCategory? category)
        {
            eventBus?.Publish(new ValidationRequested(context.BuildingId));
            ValidationResult result = engine.Validate(context, category);
            Diagnostics.RecordValidation(result);
            for (int i = 0; i < result.Issues.Count; i++)
            {
                eventBus?.Publish(new ValidationRuleTriggered(result.Issues[i].RuleId));
            }

            if (result.Status == ValidationStatus.Success) eventBus?.Publish(new ValidationSucceeded(context.BuildingId));
            else if (result.Status == ValidationStatus.Warning) eventBus?.Publish(new ValidationWarning(context.BuildingId));
            else eventBus?.Publish(new ValidationFailed(context.BuildingId));
            return result;
        }
    }

    public readonly struct ValidationRequested : IGameplayEvent, IBuildingEvent { public string BuildingId { get; } public ValidationRequested(string buildingId) { BuildingId = buildingId; } }
    public readonly struct ValidationSucceeded : IGameplayEvent, IBuildingEvent { public string BuildingId { get; } public ValidationSucceeded(string buildingId) { BuildingId = buildingId; } }
    public readonly struct ValidationFailed : IGameplayEvent, IBuildingEvent { public string BuildingId { get; } public ValidationFailed(string buildingId) { BuildingId = buildingId; } }
    public readonly struct ValidationWarning : IGameplayEvent, IBuildingEvent { public string BuildingId { get; } public ValidationWarning(string buildingId) { BuildingId = buildingId; } }
    public readonly struct ValidationRuleTriggered : IGameplayEvent, IBuildingEvent { public string RuleId { get; } public ValidationRuleTriggered(string ruleId) { RuleId = ruleId; } }
}
