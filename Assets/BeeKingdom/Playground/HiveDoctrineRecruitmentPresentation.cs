using System;
using System.Collections.Generic;

namespace BeeKingdom.Playground
{
    public sealed class HiveDoctrineRecruitmentDefinition
    {
        public HiveDoctrineRecruitmentDefinition(
            string doctrineToken,
            string troopType,
            string populationId,
            int batchSize,
            int honeyCost,
            int pollenCost,
            float durationSeconds,
            string sourceHotspotId)
        {
            DoctrineToken = doctrineToken ?? throw new ArgumentNullException(nameof(doctrineToken));
            TroopType = troopType ?? throw new ArgumentNullException(nameof(troopType));
            PopulationId = populationId ?? throw new ArgumentNullException(nameof(populationId));
            BatchSize = Math.Max(1, batchSize);
            HoneyCost = Math.Max(0, honeyCost);
            PollenCost = Math.Max(0, pollenCost);
            DurationSeconds = Math.Max(1f, durationSeconds);
            SourceHotspotId = sourceHotspotId ?? throw new ArgumentNullException(nameof(sourceHotspotId));
        }

        public string DoctrineToken { get; }
        public string TroopType { get; }
        public string PopulationId { get; }
        public int BatchSize { get; }
        public int HoneyCost { get; }
        public int PollenCost { get; }
        public float DurationSeconds { get; }
        public string SourceHotspotId { get; }
    }

    public static class HiveDoctrineRecruitmentCatalog
    {
        public const string Version = "phase4-combat-recruitment-v1";

        private static readonly HiveDoctrineRecruitmentDefinition[] Definitions =
        {
            new HiveDoctrineRecruitmentDefinition("guardians", "Gardiennes", "guardians", 4, 680, 180, 14f, "guard_post"),
            new HiveDoctrineRecruitmentDefinition("wingrunners", "Voltigeuses", "wingrunners", 6, 420, 260, 14f, "guard_post"),
            new HiveDoctrineRecruitmentDefinition("darters", "Lanceuses", "darters", 8, 500, 120, 14f, "guard_post")
        };

        public static IReadOnlyList<HiveDoctrineRecruitmentDefinition> All => Definitions;

        public static bool TryResolveDoctrine(string doctrineToken, out HiveDoctrineRecruitmentDefinition definition)
        {
            for (int index = 0; index < Definitions.Length; index++)
            {
                HiveDoctrineRecruitmentDefinition candidate = Definitions[index];
                if (!string.Equals(candidate.DoctrineToken, doctrineToken, StringComparison.OrdinalIgnoreCase)) continue;
                definition = candidate;
                return true;
            }

            definition = null;
            return false;
        }

        public static bool TryResolveTroopType(string troopType, out HiveDoctrineRecruitmentDefinition definition)
        {
            for (int index = 0; index < Definitions.Length; index++)
            {
                HiveDoctrineRecruitmentDefinition candidate = Definitions[index];
                if (!string.Equals(candidate.TroopType, troopType, StringComparison.Ordinal)) continue;
                definition = candidate;
                return true;
            }

            definition = null;
            return false;
        }
    }
}
