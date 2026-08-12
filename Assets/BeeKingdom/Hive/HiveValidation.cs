using System.Collections.Generic;

namespace BeeKingdom.Hive
{
    public readonly struct HiveValidationIssue
    {
        public string Message { get; }

        public HiveValidationIssue(string message)
        {
            Message = message ?? string.Empty;
        }
    }

    public sealed class HiveValidationResult
    {
        public IReadOnlyList<HiveValidationIssue> Issues { get; }
        public bool IsValid => Issues.Count == 0;

        public HiveValidationResult(IReadOnlyList<HiveValidationIssue> issues)
        {
            Issues = issues;
        }
    }
}
