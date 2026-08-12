using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Config.Runtime
{
    public sealed class ConfigurationLoadResult
    {
        public IReadOnlyList<IConfigurationDefinition> Definitions { get; }
        public IReadOnlyList<ConfigurationValidationIssue> Issues { get; }
        public bool HasErrors => Issues.Any(issue => issue.Severity == ConfigurationIssueSeverity.Error);

        public ConfigurationLoadResult(IReadOnlyList<IConfigurationDefinition> definitions, IReadOnlyList<ConfigurationValidationIssue> issues)
        {
            Definitions = definitions;
            Issues = issues;
        }
    }
}
