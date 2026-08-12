using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BeeKingdom.Config.Runtime
{
    public abstract class ConfigurationDefinition : ScriptableObject, IConfigurationDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string[] referenceIds;
        [SerializeField] private string[] dependencyIds;

        public ConfigurationId Id => new ConfigurationId(id);
        public string DisplayName => displayName;
        public IReadOnlyList<ConfigurationId> ReferenceIds => ToIds(referenceIds);
        public IReadOnlyList<ConfigurationId> DependencyIds => ToIds(dependencyIds);

        public virtual IEnumerable<ConfigurationValidationIssue> ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                yield return Error("Configuration id is required.");
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                yield return Error("Display name is required.");
            }
        }

        protected ConfigurationValidationIssue Error(string message)
        {
            return new ConfigurationValidationIssue(ConfigurationIssueSeverity.Error, id, message);
        }

        protected ConfigurationValidationIssue Warning(string message)
        {
            return new ConfigurationValidationIssue(ConfigurationIssueSeverity.Warning, id, message);
        }

        private static IReadOnlyList<ConfigurationId> ToIds(IEnumerable<string> values)
        {
            if (values == null)
            {
                return new List<ConfigurationId>();
            }

            return values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => new ConfigurationId(value))
                .ToList();
        }
    }
}
