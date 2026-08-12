using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Config.Runtime;

namespace BeeKingdom.Config.Validators
{
    public sealed class ConfigurationValidator
    {
        public IReadOnlyList<ConfigurationValidationIssue> Validate(IEnumerable<IConfigurationDefinition> definitions)
        {
            List<IConfigurationDefinition> definitionList = definitions.ToList();
            List<ConfigurationValidationIssue> issues = new List<ConfigurationValidationIssue>();

            issues.AddRange(ValidateLocalRules(definitionList));
            issues.AddRange(ValidateDuplicateIds(definitionList));
            issues.AddRange(ValidateMissingReferences(definitionList));
            issues.AddRange(ValidateCircularDependencies(definitionList));

            return issues;
        }

        private static IEnumerable<ConfigurationValidationIssue> ValidateLocalRules(IEnumerable<IConfigurationDefinition> definitions)
        {
            foreach (IConfigurationDefinition definition in definitions)
            {
                foreach (ConfigurationValidationIssue issue in definition.ValidateConfiguration())
                {
                    yield return issue;
                }
            }
        }

        private static IEnumerable<ConfigurationValidationIssue> ValidateDuplicateIds(IEnumerable<IConfigurationDefinition> definitions)
        {
            foreach (var group in definitions.GroupBy(definition => definition.Id).Where(group => group.Count() > 1))
            {
                yield return new ConfigurationValidationIssue(ConfigurationIssueSeverity.Error, group.Key.ToString(), "Duplicate configuration id.");
            }
        }

        private static IEnumerable<ConfigurationValidationIssue> ValidateMissingReferences(IReadOnlyList<IConfigurationDefinition> definitions)
        {
            HashSet<ConfigurationId> knownIds = definitions.Select(definition => definition.Id).ToHashSet();
            foreach (IConfigurationDefinition definition in definitions)
            {
                foreach (ConfigurationId referenceId in definition.ReferenceIds.Concat(definition.DependencyIds))
                {
                    if (!knownIds.Contains(referenceId))
                    {
                        yield return new ConfigurationValidationIssue(ConfigurationIssueSeverity.Error, definition.Id.ToString(), $"Missing referenced configuration '{referenceId}'.");
                    }
                }
            }
        }

        private static IEnumerable<ConfigurationValidationIssue> ValidateCircularDependencies(IReadOnlyList<IConfigurationDefinition> definitions)
        {
            Dictionary<ConfigurationId, IConfigurationDefinition> byId = definitions
                .GroupBy(definition => definition.Id)
                .Where(group => group.Count() == 1)
                .ToDictionary(group => group.Key, group => group.First());

            HashSet<ConfigurationId> visiting = new HashSet<ConfigurationId>();
            HashSet<ConfigurationId> visited = new HashSet<ConfigurationId>();

            foreach (IConfigurationDefinition definition in byId.Values)
            {
                if (HasCycle(definition.Id, byId, visiting, visited))
                {
                    yield return new ConfigurationValidationIssue(ConfigurationIssueSeverity.Error, definition.Id.ToString(), "Circular dependency detected.");
                }
            }
        }

        private static bool HasCycle(ConfigurationId id, IReadOnlyDictionary<ConfigurationId, IConfigurationDefinition> byId, HashSet<ConfigurationId> visiting, HashSet<ConfigurationId> visited)
        {
            if (visited.Contains(id)) return false;
            if (visiting.Contains(id)) return true;
            if (!byId.TryGetValue(id, out IConfigurationDefinition definition)) return false;

            visiting.Add(id);
            foreach (ConfigurationId dependencyId in definition.DependencyIds)
            {
                if (HasCycle(dependencyId, byId, visiting, visited))
                {
                    return true;
                }
            }

            visiting.Remove(id);
            visited.Add(id);
            return false;
        }
    }
}
