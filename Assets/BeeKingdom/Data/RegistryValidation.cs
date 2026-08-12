using System.Collections.Generic;

namespace BeeKingdom.Data
{
    public enum RegistryIssueSeverity
    {
        Warning,
        Error
    }

    public readonly struct RegistryValidationIssue
    {
        public RegistryIssueSeverity Severity { get; }
        public string DefinitionId { get; }
        public string Message { get; }

        public RegistryValidationIssue(RegistryIssueSeverity severity, string definitionId, string message)
        {
            Severity = severity;
            DefinitionId = definitionId ?? string.Empty;
            Message = message ?? string.Empty;
        }
    }

    public sealed class RegistryValidationResult
    {
        public IReadOnlyList<RegistryValidationIssue> Issues { get; }
        public int ErrorCount { get; }
        public int WarningCount { get; }
        public bool HasErrors => ErrorCount > 0;

        public RegistryValidationResult(IReadOnlyList<RegistryValidationIssue> issues)
        {
            Issues = issues;
            int errors = 0;
            int warnings = 0;
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == RegistryIssueSeverity.Error)
                {
                    errors++;
                }
                else
                {
                    warnings++;
                }
            }

            ErrorCount = errors;
            WarningCount = warnings;
        }
    }
}
