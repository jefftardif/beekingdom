namespace BeeKingdom.Core.Modifiers
{
    public sealed class ModifierDiagnostics
    {
        public int ModifierCount { get; private set; }
        public int EvaluationCount { get; private set; }
        public double LastValue { get; private set; }

        public void RecordModifiers(int count) { ModifierCount = count; }
        public void RecordEvaluation(double value) { EvaluationCount++; LastValue = value; }
    }
}
