using System;

namespace BeeKingdom.Playground
{
    public sealed class HiveSquadCompositionSnapshot
    {
        public HiveSquadCompositionSnapshot(int capacity, int guardians, int wingrunners, int darters)
        {
            Capacity = Math.Max(1, capacity);
            Guardians = Math.Max(0, guardians);
            Wingrunners = Math.Max(0, wingrunners);
            Darters = Math.Max(0, darters);
        }

        public int Capacity { get; }
        public int Guardians { get; }
        public int Wingrunners { get; }
        public int Darters { get; }
        public int Total => Guardians + Wingrunners + Darters;
        public int Remaining => Math.Max(0, Capacity - Total);
        public bool IsEmpty => Total == 0;
        public bool IsFull => Total >= Capacity;

        public int Count(string doctrineToken)
        {
            if (string.Equals(doctrineToken, "guardians", StringComparison.OrdinalIgnoreCase)) return Guardians;
            if (string.Equals(doctrineToken, "wingrunners", StringComparison.OrdinalIgnoreCase)) return Wingrunners;
            if (string.Equals(doctrineToken, "darters", StringComparison.OrdinalIgnoreCase)) return Darters;
            return 0;
        }
    }

    public sealed class HiveSquadDoctrineAssessment
    {
        public HiveSquadDoctrineAssessment(HiveCombatDoctrineOutcome outcome, int responsiveCount, int exposedCount, int neutralCount)
        {
            Outcome = outcome;
            ResponsiveCount = Math.Max(0, responsiveCount);
            ExposedCount = Math.Max(0, exposedCount);
            NeutralCount = Math.Max(0, neutralCount);
        }

        public HiveCombatDoctrineOutcome Outcome { get; }
        public int ResponsiveCount { get; }
        public int ExposedCount { get; }
        public int NeutralCount { get; }
    }

    public static class HiveSquadCompositionPlanner
    {
        public const string ContractVersion = "phase4-combat-squad-reservation-v1";
        public const int InitialCapacity = 12;

        public static HiveSquadCompositionSnapshot Empty(int capacity = InitialCapacity)
        {
            return new HiveSquadCompositionSnapshot(capacity, 0, 0, 0);
        }

        public static HiveSquadCompositionSnapshot CreateInitial(
            HiveFormationReadinessSnapshot roster,
            int capacity = InitialCapacity)
        {
            if (roster == null) throw new ArgumentNullException(nameof(roster));
            int safeCapacity = Math.Max(1, capacity);
            for (int index = 0; index < roster.Families.Count; index++)
            {
                HiveFormationRosterEntry entry = roster.Families[index];
                if (!entry.CanPrefillDraft) continue;
                int count = Math.Min(safeCapacity, entry.EligibleCount);
                return Set(Empty(safeCapacity), roster, entry.Doctrine.Token, count);
            }

            return Empty(safeCapacity);
        }

        public static HiveSquadCompositionSnapshot Normalize(
            HiveFormationReadinessSnapshot roster,
            int capacity,
            int guardians,
            int wingrunners,
            int darters)
        {
            if (roster == null) throw new ArgumentNullException(nameof(roster));
            int safeCapacity = Math.Max(1, capacity);
            int safeGuardians = Math.Min(Math.Max(0, guardians), Available(roster, "guardians"));
            int safeWingrunners = Math.Min(Math.Max(0, wingrunners), Available(roster, "wingrunners"));
            int safeDarters = Math.Min(Math.Max(0, darters), Available(roster, "darters"));
            int remaining = safeCapacity;
            safeGuardians = TakeWithin(ref remaining, safeGuardians);
            safeWingrunners = TakeWithin(ref remaining, safeWingrunners);
            safeDarters = TakeWithin(ref remaining, safeDarters);
            return new HiveSquadCompositionSnapshot(safeCapacity, safeGuardians, safeWingrunners, safeDarters);
        }

        public static HiveSquadCompositionSnapshot Adjust(
            HiveSquadCompositionSnapshot current,
            HiveFormationReadinessSnapshot roster,
            string doctrineToken,
            int delta)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            int requested = current.Count(doctrineToken) + delta;
            return Set(current, roster, doctrineToken, requested);
        }

        public static HiveSquadCompositionSnapshot Recommend(
            HiveFormationReadinessSnapshot roster,
            string threatToken,
            int capacity = InitialCapacity)
        {
            if (roster == null) throw new ArgumentNullException(nameof(roster));
            int safeCapacity = Math.Max(1, capacity);
            string counter = CounterTo(threatToken);
            if (string.IsNullOrWhiteSpace(counter)) return CreateInitial(roster, safeCapacity);

            string[] order = RecommendationOrder(counter);
            int[] desired = { (safeCapacity + 1) / 2, safeCapacity / 4, safeCapacity / 4 };
            while (desired[0] + desired[1] + desired[2] < safeCapacity) desired[0]++;

            int guardians = 0;
            int wingrunners = 0;
            int darters = 0;
            for (int index = 0; index < order.Length; index++)
                Assign(order[index], Math.Min(desired[index], Available(roster, order[index])), ref guardians, ref wingrunners, ref darters);

            int total = guardians + wingrunners + darters;
            while (total < safeCapacity)
            {
                bool changed = false;
                for (int index = 0; index < order.Length && total < safeCapacity; index++)
                {
                    string token = order[index];
                    int current = Count(token, guardians, wingrunners, darters);
                    if (current >= Available(roster, token)) continue;
                    Assign(token, current + 1, ref guardians, ref wingrunners, ref darters);
                    total++;
                    changed = true;
                }
                if (!changed) break;
            }

            return Normalize(roster, safeCapacity, guardians, wingrunners, darters);
        }

        public static HiveSquadDoctrineAssessment Assess(
            HiveSquadCompositionSnapshot composition,
            string threatToken)
        {
            if (composition == null) throw new ArgumentNullException(nameof(composition));
            if (composition.IsEmpty || !HiveCombatDoctrineCatalog.TryResolve(threatToken, out HiveCombatDoctrineDefinition threat))
                return new HiveSquadDoctrineAssessment(HiveCombatDoctrineOutcome.Pending, 0, 0, composition.Total);

            int responsive = 0;
            int exposed = 0;
            int neutral = 0;
            foreach (HiveCombatDoctrineDefinition family in HiveCombatDoctrineCatalog.All)
            {
                int count = composition.Count(family.Token);
                HiveCombatDoctrineOutcome outcome = HiveCombatDoctrineCatalog.Evaluate(family.Family, threat.Family);
                if (outcome == HiveCombatDoctrineOutcome.Advantage) responsive += count;
                else if (outcome == HiveCombatDoctrineOutcome.Vulnerable) exposed += count;
                else neutral += count;
            }

            HiveCombatDoctrineOutcome aggregate = responsive > exposed
                ? HiveCombatDoctrineOutcome.Advantage
                : exposed > responsive ? HiveCombatDoctrineOutcome.Vulnerable : HiveCombatDoctrineOutcome.Even;
            return new HiveSquadDoctrineAssessment(aggregate, responsive, exposed, neutral);
        }

        private static HiveSquadCompositionSnapshot Set(
            HiveSquadCompositionSnapshot current,
            HiveFormationReadinessSnapshot roster,
            string doctrineToken,
            int requested)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (roster == null) throw new ArgumentNullException(nameof(roster));
            int currentCount = current.Count(doctrineToken);
            int otherTotal = current.Total - currentCount;
            int allowed = Math.Min(Available(roster, doctrineToken), Math.Max(0, current.Capacity - otherTotal));
            int safe = Math.Min(Math.Max(0, requested), allowed);
            int guardians = current.Guardians;
            int wingrunners = current.Wingrunners;
            int darters = current.Darters;
            Assign(doctrineToken, safe, ref guardians, ref wingrunners, ref darters);
            return Normalize(roster, current.Capacity, guardians, wingrunners, darters);
        }

        private static int Available(HiveFormationReadinessSnapshot roster, string token)
        {
            HiveFormationRosterEntry entry = roster.Find(token);
            return entry != null && entry.HasTrustedLocalMapping ? entry.EligibleCount : 0;
        }

        private static int TakeWithin(ref int remaining, int requested)
        {
            int taken = Math.Min(Math.Max(0, remaining), Math.Max(0, requested));
            remaining -= taken;
            return taken;
        }

        private static string CounterTo(string threatToken)
        {
            if (!HiveCombatDoctrineCatalog.TryResolve(threatToken, out HiveCombatDoctrineDefinition threat)) return string.Empty;
            foreach (HiveCombatDoctrineDefinition family in HiveCombatDoctrineCatalog.All)
                if (HiveCombatDoctrineCatalog.Evaluate(family.Family, threat.Family) == HiveCombatDoctrineOutcome.Advantage)
                    return family.Token;
            return string.Empty;
        }

        private static string[] RecommendationOrder(string counter)
        {
            if (string.Equals(counter, "guardians", StringComparison.Ordinal)) return new[] { "guardians", "wingrunners", "darters" };
            if (string.Equals(counter, "wingrunners", StringComparison.Ordinal)) return new[] { "wingrunners", "darters", "guardians" };
            return new[] { "darters", "guardians", "wingrunners" };
        }

        private static int Count(string token, int guardians, int wingrunners, int darters)
        {
            if (string.Equals(token, "guardians", StringComparison.Ordinal)) return guardians;
            if (string.Equals(token, "wingrunners", StringComparison.Ordinal)) return wingrunners;
            if (string.Equals(token, "darters", StringComparison.Ordinal)) return darters;
            return 0;
        }

        private static void Assign(string token, int value, ref int guardians, ref int wingrunners, ref int darters)
        {
            if (string.Equals(token, "guardians", StringComparison.OrdinalIgnoreCase)) guardians = Math.Max(0, value);
            else if (string.Equals(token, "wingrunners", StringComparison.OrdinalIgnoreCase)) wingrunners = Math.Max(0, value);
            else if (string.Equals(token, "darters", StringComparison.OrdinalIgnoreCase)) darters = Math.Max(0, value);
        }
    }
}
