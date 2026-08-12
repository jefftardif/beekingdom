using System;
using System.Collections.Generic;

namespace BeeKingdom.Playground
{
    public enum HiveFormationRosterState
    {
        Available = 0,
        Empty = 1,
        NotMapped = 2
    }

    public sealed class HiveFormationRosterEntry
    {
        public HiveFormationRosterEntry(
            HiveCombatDoctrineDefinition doctrine,
            HiveFormationRosterState state,
            int eligibleCount,
            string sourcePopulationId)
        {
            Doctrine = doctrine ?? throw new ArgumentNullException(nameof(doctrine));
            State = state;
            EligibleCount = Math.Max(0, eligibleCount);
            SourcePopulationId = sourcePopulationId ?? string.Empty;
        }

        public HiveCombatDoctrineDefinition Doctrine { get; }
        public HiveFormationRosterState State { get; }
        public int EligibleCount { get; }
        public string SourcePopulationId { get; }
        public bool HasTrustedLocalMapping => State != HiveFormationRosterState.NotMapped;
        public bool CanPrefillDraft => State == HiveFormationRosterState.Available && EligibleCount > 0;
    }

    public sealed class HiveFormationReadinessSnapshot
    {
        public HiveFormationReadinessSnapshot(
            IReadOnlyList<HiveFormationRosterEntry> families,
            int unclassifiedSoldiers,
            int unclassifiedScouts,
            bool serverAuthoritative = false,
            IReadOnlyList<string> unclassifiedLegacyRoles = null)
        {
            Families = families ?? throw new ArgumentNullException(nameof(families));
            UnclassifiedSoldiers = Math.Max(0, unclassifiedSoldiers);
            UnclassifiedScouts = Math.Max(0, unclassifiedScouts);
            ServerAuthoritative = serverAuthoritative;
            UnclassifiedLegacyRoles =
                unclassifiedLegacyRoles ?? Array.Empty<string>();
        }

        public IReadOnlyList<HiveFormationRosterEntry> Families { get; }
        public int UnclassifiedSoldiers { get; }
        public int UnclassifiedScouts { get; }
        public bool ServerAuthoritative { get; }
        public IReadOnlyList<string> UnclassifiedLegacyRoles { get; }

        public HiveFormationRosterEntry Find(string doctrineToken)
        {
            for (int index = 0; index < Families.Count; index++)
            {
                HiveFormationRosterEntry entry = Families[index];
                if (string.Equals(entry.Doctrine.Token, doctrineToken, StringComparison.OrdinalIgnoreCase))
                    return entry;
            }

            return null;
        }
    }

    public static class HiveFormationReadinessProjection
    {
        public static HiveFormationReadinessSnapshot Project(
            int guardians,
            int wingrunners,
            int darters,
            int legacySoldiers,
            int legacyScouts)
        {
            if (!HiveCombatDoctrineCatalog.TryResolve(HiveCombatDoctrineFamily.Guardians, out HiveCombatDoctrineDefinition guardianDoctrine)
                || !HiveCombatDoctrineCatalog.TryResolve(HiveCombatDoctrineFamily.Wingrunners, out HiveCombatDoctrineDefinition wingrunnerDoctrine)
                || !HiveCombatDoctrineCatalog.TryResolve(HiveCombatDoctrineFamily.Darters, out HiveCombatDoctrineDefinition darterDoctrine))
                throw new InvalidOperationException("Combat doctrine catalog is incomplete.");

            int safeGuardians = Math.Max(0, guardians);
            int safeWingrunners = Math.Max(0, wingrunners);
            int safeDarters = Math.Max(0, darters);
            return new HiveFormationReadinessSnapshot(
                new[]
                {
                    new HiveFormationRosterEntry(
                        guardianDoctrine,
                        safeGuardians > 0 ? HiveFormationRosterState.Available : HiveFormationRosterState.Empty,
                        safeGuardians,
                        "guardians"),
                    new HiveFormationRosterEntry(
                        wingrunnerDoctrine,
                        safeWingrunners > 0 ? HiveFormationRosterState.Available : HiveFormationRosterState.Empty,
                        safeWingrunners,
                        "wingrunners"),
                    new HiveFormationRosterEntry(
                        darterDoctrine,
                        safeDarters > 0 ? HiveFormationRosterState.Available : HiveFormationRosterState.Empty,
                        safeDarters,
                        "darters")
                },
                legacySoldiers,
                legacyScouts);
        }

        public static HiveFormationReadinessSnapshot ProjectOfficial(
            IReadOnlyDictionary<string, long> counts,
            IReadOnlyList<string> legacyRoles)
        {
            if (counts == null)
                throw new ArgumentNullException(nameof(counts));
            if (legacyRoles == null)
                throw new ArgumentNullException(nameof(legacyRoles));
            if (!HiveCombatDoctrineCatalog.TryResolve(
                    HiveCombatDoctrineFamily.Guardians,
                    out HiveCombatDoctrineDefinition guardianDoctrine) ||
                !HiveCombatDoctrineCatalog.TryResolve(
                    HiveCombatDoctrineFamily.Wingrunners,
                    out HiveCombatDoctrineDefinition wingrunnerDoctrine) ||
                !HiveCombatDoctrineCatalog.TryResolve(
                    HiveCombatDoctrineFamily.Darters,
                    out HiveCombatDoctrineDefinition darterDoctrine))
                throw new InvalidOperationException(
                    "Combat doctrine catalog is incomplete.");

            int guardians = OfficialCount(counts, "guardians");
            int wingrunners = OfficialCount(counts, "wingrunners");
            int darters = OfficialCount(counts, "darters");
            return new HiveFormationReadinessSnapshot(
                new[]
                {
                    OfficialEntry(
                        guardianDoctrine,
                        guardians,
                        "guardians"),
                    OfficialEntry(
                        wingrunnerDoctrine,
                        wingrunners,
                        "wingrunners"),
                    OfficialEntry(
                        darterDoctrine,
                        darters,
                        "darters")
                },
                0,
                0,
                true,
                new List<string>(legacyRoles));
        }

        private static HiveFormationRosterEntry OfficialEntry(
            HiveCombatDoctrineDefinition doctrine,
            int count,
            string source)
        {
            return new HiveFormationRosterEntry(
                doctrine,
                count > 0
                    ? HiveFormationRosterState.Available
                    : HiveFormationRosterState.Empty,
                count,
                source);
        }

        private static int OfficialCount(
            IReadOnlyDictionary<string, long> counts,
            string family)
        {
            if (!counts.TryGetValue(family, out long value) || value <= 0)
                return 0;
            return value >= int.MaxValue ? int.MaxValue : (int)value;
        }
    }
}
