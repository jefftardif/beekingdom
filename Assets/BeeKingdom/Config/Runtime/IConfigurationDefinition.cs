using System.Collections.Generic;

namespace BeeKingdom.Config.Runtime
{
    public interface IConfigurationDefinition
    {
        ConfigurationId Id { get; }
        string DisplayName { get; }
        IReadOnlyList<ConfigurationId> ReferenceIds { get; }
        IReadOnlyList<ConfigurationId> DependencyIds { get; }
        IEnumerable<ConfigurationValidationIssue> ValidateConfiguration();
    }
}
