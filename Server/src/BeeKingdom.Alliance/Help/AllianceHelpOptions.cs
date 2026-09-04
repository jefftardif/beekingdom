namespace BeeKingdom.Alliance.Help;

// M045-CL Alpha balance policy - centralized here rather than scattered magic numbers, per mission
// instruction. No prior Alliance Help design doc exists in Docs/ (confirmed by inventory before
// writing this), so these are the mission's own suggested Alpha defaults, made configurable so a
// future balance pass never needs a code change.
public sealed class AllianceHelpOptions
{
    public const string SectionName = "AllianceHelp";

    public bool Enabled { get; set; }

    public int MaxHelpCount { get; set; } = 10;

    // Each valid help reduces: clamp(percentage * original duration, MinReductionSeconds, MaxReductionSeconds),
    // and never more than the current remaining duration (enforced by the caller, not here).
    public double ReductionPercentOfOriginalDuration { get; set; } = 0.01;
    public int MinReductionSeconds { get; set; } = 60;
    public int MaxReductionSeconds { get; set; } = 300;

    // Operations shorter than this never generate a help request at all - without this, a 3-minute
    // early-game construction could receive a flat 60s-minimum reduction (a third of its duration),
    // trivializing FTUE pacing. 5 minutes is the mission's own suggested Alpha threshold.
    public int MinEligibleOriginalDurationSeconds { get; set; } = 300;

    public void Validate()
    {
        if (!Enabled) return;
        if (MaxHelpCount is <= 0 or > 1000) throw new InvalidDataException("AllianceHelp:MaxHelpCount must be between 1 and 1000.");
        if (ReductionPercentOfOriginalDuration is <= 0 or > 1) throw new InvalidDataException("AllianceHelp:ReductionPercentOfOriginalDuration must be between 0 and 1.");
        if (MinReductionSeconds <= 0 || MaxReductionSeconds < MinReductionSeconds) throw new InvalidDataException("AllianceHelp:Min/MaxReductionSeconds are invalid.");
        if (MinEligibleOriginalDurationSeconds < 0) throw new InvalidDataException("AllianceHelp:MinEligibleOriginalDurationSeconds must not be negative.");
    }

    public long ComputeReductionSeconds(long originalDurationSeconds)
    {
        long percentBased = (long)Math.Round(originalDurationSeconds * ReductionPercentOfOriginalDuration, MidpointRounding.AwayFromZero);
        return Math.Clamp(percentBased, MinReductionSeconds, MaxReductionSeconds);
    }
}
