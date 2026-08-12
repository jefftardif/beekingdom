using System.Collections.Generic;
using BeeKingdom.Config.Runtime;

namespace BeeKingdom.Data
{
    public sealed class RegistryValidator
    {
        public RegistryValidationResult Validate(IReadOnlyList<IConfigurationDefinition> definitions)
        {
            List<RegistryValidationIssue> issues = new List<RegistryValidationIssue>();
            Dictionary<ConfigurationId, IConfigurationDefinition> byId = new Dictionary<ConfigurationId, IConfigurationDefinition>();
            Dictionary<ConfigurationId, int> incoming = new Dictionary<ConfigurationId, int>();

            for (int i = 0; i < definitions.Count; i++)
            {
                IConfigurationDefinition definition = definitions[i];
                if (string.IsNullOrWhiteSpace(definition.Id.Value))
                {
                    issues.Add(new RegistryValidationIssue(RegistryIssueSeverity.Error, string.Empty, "Definition id is required."));
                    continue;
                }

                if (byId.ContainsKey(definition.Id))
                {
                    issues.Add(new RegistryValidationIssue(RegistryIssueSeverity.Error, definition.Id.Value, "Duplicate definition id."));
                }
                else
                {
                    byId[definition.Id] = definition;
                    incoming[definition.Id] = 0;
                }
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                ValidateReferences(definitions[i], byId, incoming, issues);
            }

            HashSet<ConfigurationId> visiting = new HashSet<ConfigurationId>();
            HashSet<ConfigurationId> visited = new HashSet<ConfigurationId>();
            foreach (ConfigurationId id in byId.Keys)
            {
                if (HasCycle(id, byId, visiting, visited))
                {
                    issues.Add(new RegistryValidationIssue(RegistryIssueSeverity.Error, id.Value, "Circular dependency detected."));
                }
            }

            foreach (KeyValuePair<ConfigurationId, int> pair in incoming)
            {
                IConfigurationDefinition definition = byId[pair.Key];
                if (pair.Value == 0 && definition.ReferenceIds.Count == 0 && definition.DependencyIds.Count == 0 && definitions.Count > 1)
                {
                    issues.Add(new RegistryValidationIssue(RegistryIssueSeverity.Warning, pair.Key.Value, "Orphan definition has no references or dependencies."));
                }
            }

            return new RegistryValidationResult(issues);
        }

        private static void ValidateReferences(
            IConfigurationDefinition definition,
            Dictionary<ConfigurationId, IConfigurationDefinition> byId,
            Dictionary<ConfigurationId, int> incoming,
            List<RegistryValidationIssue> issues)
        {
            CountLinks(definition.Id, definition.ReferenceIds, byId, incoming, issues, "Missing referenced definition.");
            CountLinks(definition.Id, definition.DependencyIds, byId, incoming, issues, "Missing dependency definition.");
        }

        private static void CountLinks(
            ConfigurationId sourceId,
            IReadOnlyList<ConfigurationId> links,
            Dictionary<ConfigurationId, IConfigurationDefinition> byId,
            Dictionary<ConfigurationId, int> incoming,
            List<RegistryValidationIssue> issues,
            string message)
        {
            for (int i = 0; i < links.Count; i++)
            {
                ConfigurationId target = links[i];
                if (!byId.ContainsKey(target))
                {
                    issues.Add(new RegistryValidationIssue(RegistryIssueSeverity.Error, sourceId.Value, message + " " + target.Value));
                }
                else
                {
                    incoming[target] = incoming[target] + 1;
                }
            }
        }

        private static bool HasCycle(
            ConfigurationId id,
            Dictionary<ConfigurationId, IConfigurationDefinition> byId,
            HashSet<ConfigurationId> visiting,
            HashSet<ConfigurationId> visited)
        {
            if (visited.Contains(id))
            {
                return false;
            }

            if (visiting.Contains(id))
            {
                return true;
            }

            if (!byId.TryGetValue(id, out IConfigurationDefinition definition))
            {
                return false;
            }

            visiting.Add(id);
            for (int i = 0; i < definition.DependencyIds.Count; i++)
            {
                if (HasCycle(definition.DependencyIds[i], byId, visiting, visited))
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
