using System;
using System.Collections.Generic;

namespace BeeKingdom.Playground
{
    public sealed class LocalPreviewResearchDefinition
    {
        public LocalPreviewResearchDefinition(
            string researchId,
            string titleKey,
            string summaryKey,
            string resultKey,
            string iconId,
            float honeyCost,
            float pollenCost,
            float durationSeconds,
            int assignedBeeCount)
        {
            ResearchId = researchId ?? string.Empty;
            TitleKey = titleKey ?? string.Empty;
            SummaryKey = summaryKey ?? string.Empty;
            ResultKey = resultKey ?? string.Empty;
            IconId = iconId ?? "research";
            HoneyCost = Math.Max(0f, honeyCost);
            PollenCost = Math.Max(0f, pollenCost);
            DurationSeconds = Math.Max(0.1f, durationSeconds);
            AssignedBeeCount = Math.Max(1, assignedBeeCount);
        }

        public string ResearchId { get; }
        public string TitleKey { get; }
        public string SummaryKey { get; }
        public string ResultKey { get; }
        public string IconId { get; }
        public float HoneyCost { get; }
        public float PollenCost { get; }
        public float DurationSeconds { get; }
        public int AssignedBeeCount { get; }
    }

    public readonly struct LocalPreviewResearchEffects
    {
        public LocalPreviewResearchEffects(float honeyProductionBonus, float waxCapacityBonus)
        {
            HoneyProductionBonus = Math.Max(0f, honeyProductionBonus);
            WaxCapacityBonus = Math.Max(0f, waxCapacityBonus);
        }

        public float HoneyProductionBonus { get; }
        public float WaxCapacityBonus { get; }
    }

    public static class LocalPreviewResearchCatalog
    {
        public const string ForagingRoutesId = "foraging_routes_i";
        public const string TemperedCombsId = "tempered_combs_i";

        // Les huit entrees suivantes completent la Branche 1 (Economie) de
        // Docs/Product/BeeKingdom_ResearchTree_Design.md. Ce catalogue pilote uniquement les
        // libelles/icones affiches (titre, resume, icone) pour CHAQUE recherche du panneau,
        // officiel ou local - le cout/duree/effet reels viennent du serveur des qu'il est
        // configure (voir HiveResearchScreenModel.OfferFor). Sans une entree ici, une recherche
        // pourtant valide cote serveur resterait invisible dans l'interface.
        private static readonly LocalPreviewResearchDefinition[] Definitions =
        {
            new LocalPreviewResearchDefinition(
                ForagingRoutesId,
                "research.foraging_routes_i.title",
                "research.foraging_routes_i.summary",
                "research.foraging_routes_i.result",
                "bee",
                240f,
                90f,
                16f,
                2),
            new LocalPreviewResearchDefinition(
                "foraging_routes_ii",
                "research.foraging_routes_ii.title",
                "research.foraging_routes_ii.summary",
                "research.foraging_routes_ii.result",
                "bee",
                900f,
                500f,
                360f,
                2),
            new LocalPreviewResearchDefinition(
                "foraging_routes_iii",
                "research.foraging_routes_iii.title",
                "research.foraging_routes_iii.summary",
                "research.foraging_routes_iii.result",
                "bee",
                2400f,
                1400f,
                720f,
                2),
            new LocalPreviewResearchDefinition(
                TemperedCombsId,
                "research.tempered_combs_i.title",
                "research.tempered_combs_i.summary",
                "research.tempered_combs_i.result",
                "wax",
                180f,
                120f,
                16f,
                2),
            new LocalPreviewResearchDefinition(
                "tempered_combs_ii",
                "research.tempered_combs_ii.title",
                "research.tempered_combs_ii.summary",
                "research.tempered_combs_ii.result",
                "wax",
                900f,
                500f,
                360f,
                2),
            new LocalPreviewResearchDefinition(
                "tempered_combs_iii",
                "research.tempered_combs_iii.title",
                "research.tempered_combs_iii.summary",
                "research.tempered_combs_iii.result",
                "wax",
                2400f,
                1400f,
                720f,
                2),
            new LocalPreviewResearchDefinition(
                "pollen_sorting_i",
                "research.pollen_sorting_i.title",
                "research.pollen_sorting_i.summary",
                "research.pollen_sorting_i.result",
                "pollen",
                200f,
                150f,
                120f,
                2),
            new LocalPreviewResearchDefinition(
                "pollen_sorting_ii",
                "research.pollen_sorting_ii.title",
                "research.pollen_sorting_ii.summary",
                "research.pollen_sorting_ii.result",
                "pollen",
                800f,
                600f,
                360f,
                2),
            new LocalPreviewResearchDefinition(
                "pollen_sorting_iii",
                "research.pollen_sorting_iii.title",
                "research.pollen_sorting_iii.summary",
                "research.pollen_sorting_iii.result",
                "pollen",
                2200f,
                1600f,
                720f,
                2),
            new LocalPreviewResearchDefinition(
                "sealed_reserves",
                "research.sealed_reserves.title",
                "research.sealed_reserves.summary",
                "research.sealed_reserves.result",
                "capacity",
                6000f,
                4000f,
                1200f,
                2)
        };

        public static IReadOnlyList<LocalPreviewResearchDefinition> All => Definitions;

        public static LocalPreviewResearchDefinition Find(string researchId)
        {
            for (int i = 0; i < Definitions.Length; i++)
            {
                if (string.Equals(Definitions[i].ResearchId, researchId, StringComparison.Ordinal)) return Definitions[i];
            }

            return null;
        }

        public static bool Contains(IReadOnlyList<string> completedResearchIds, string researchId)
        {
            if (completedResearchIds == null || string.IsNullOrWhiteSpace(researchId)) return false;
            for (int i = 0; i < completedResearchIds.Count; i++)
            {
                if (string.Equals(completedResearchIds[i], researchId, StringComparison.Ordinal)) return true;
            }

            return false;
        }

        public static LocalPreviewResearchEffects Derive(IReadOnlyList<string> completedResearchIds)
        {
            float honeyProductionBonus = Contains(completedResearchIds, ForagingRoutesId) ? 0.02f : 0f;
            float waxCapacityBonus = Contains(completedResearchIds, TemperedCombsId) ? 0.05f : 0f;
            return new LocalPreviewResearchEffects(honeyProductionBonus, waxCapacityBonus);
        }
    }
}
