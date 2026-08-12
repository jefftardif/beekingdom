using System;

namespace BeeKingdom.Config.Runtime
{
    public enum ConfigurationIssueSeverity
    {
        Warning,
        Error
    }

    [Serializable]
    public readonly struct ConfigurationValidationIssue
    {
        public ConfigurationIssueSeverity Severity { get; }
        public string DefinitionId { get; }
        public string Message { get; }

        public ConfigurationValidationIssue(ConfigurationIssueSeverity severity, string definitionId, string message)
        {
            Severity = severity;
            DefinitionId = definitionId;
            Message = message;
        }

        public override string ToString() => $"{Severity}: {DefinitionId} - {Message}";
    }
}
