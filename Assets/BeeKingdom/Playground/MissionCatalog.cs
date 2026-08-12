using System;

namespace BeeKingdom.Playground
{
    // Catalogue de donnees du Centre de Missions (Sprint-016). C'est la source de verite des
    // missions et des chapitres : l'UI du presenter rend exactement ce catalogue, et l'evaluateur
    // de progression (HiveViewProductUiPresenter) lit l'etat reel du royaume. Quand les vraies
    // donnees serveur arriveront, seul ce catalogue et l'evaluateur seront remplaces par l'API
    // client ; les ecrans resteront inchanges.

    public enum MissionSectionKind
    {
        Histoire,
        Quotidiennes,
        Hebdomadaires,
        Defis,
        Succes
    }

    public enum MissionNavigationKind
    {
        None,
        Collect,
        Building,
        Alliance,
        Chat,
        Friends,
        Courier,
        Champions,
        Research,
        Bestiary
    }

    public sealed class MissionDefinition
    {
        public readonly string Id;
        public readonly MissionSectionKind Section;
        public readonly string TitleKey;
        public readonly string TitleFallback;
        public readonly string BodyKey;
        public readonly string BodyFallback;
        public readonly string IconId;
        public readonly int TargetCount;
        public readonly MissionNavigationKind Navigation;
        public readonly string NavigationPayload;
        public readonly int RewardHoney;
        public readonly int RewardWax;
        public readonly int RewardPollen;
        public readonly bool SimulatedOnly;

        public MissionDefinition(
            string id,
            MissionSectionKind section,
            string titleKey,
            string titleFallback,
            string bodyKey,
            string bodyFallback,
            string iconId,
            int targetCount,
            MissionNavigationKind navigation,
            string navigationPayload,
            int rewardHoney,
            int rewardWax,
            int rewardPollen,
            bool simulatedOnly = false)
        {
            Id = id;
            Section = section;
            TitleKey = titleKey;
            TitleFallback = titleFallback;
            BodyKey = bodyKey;
            BodyFallback = bodyFallback;
            IconId = iconId;
            TargetCount = Math.Max(1, targetCount);
            Navigation = navigation;
            NavigationPayload = navigationPayload;
            RewardHoney = rewardHoney;
            RewardWax = rewardWax;
            RewardPollen = rewardPollen;
            SimulatedOnly = simulatedOnly;
        }
    }

    public sealed class MissionChapterDefinition
    {
        public readonly string Id;
        public readonly string TitleKey;
        public readonly string TitleFallback;
        public readonly string SubtitleKey;
        public readonly string SubtitleFallback;
        public readonly string IconId;
        public readonly string[] ObjectiveIds;
        public readonly int RewardHoney;
        public readonly int RewardWax;
        public readonly int RewardPollen;

        public MissionChapterDefinition(
            string id,
            string titleKey,
            string titleFallback,
            string subtitleKey,
            string subtitleFallback,
            string iconId,
            string[] objectiveIds,
            int rewardHoney,
            int rewardWax,
            int rewardPollen)
        {
            Id = id;
            TitleKey = titleKey;
            TitleFallback = titleFallback;
            SubtitleKey = subtitleKey;
            SubtitleFallback = subtitleFallback;
            IconId = iconId;
            ObjectiveIds = objectiveIds ?? new string[0];
            RewardHoney = rewardHoney;
            RewardWax = rewardWax;
            RewardPollen = rewardPollen;
        }
    }

    public static class MissionCatalog
    {
        public const int MaxPinnedMissions = 3;

        private static readonly MissionDefinition[] MissionsInternal =
        {
            // ---- Quotidiennes ----
            new MissionDefinition("q_collect", MissionSectionKind.Quotidiennes, "missions.q_collect.title", "Recolter 500 miel", "missions.q_collect.body", "Ravie la reserve d'or dans les stocks.", "honey", 500, MissionNavigationKind.Collect, "honey_storage", 120, 40, 0),
            new MissionDefinition("q_build", MissionSectionKind.Quotidiennes, "missions.q_build.title", "Construire un batiment", "missions.q_build.body", "Agrandit ta ruche avec un batiment.", "construct", 3, MissionNavigationKind.Building, "honey_storage", 0, 150, 0),
            new MissionDefinition("q_alliance_help", MissionSectionKind.Quotidiennes, "missions.q_alliance_help.title", "Envoyer une aide Alliance", "missions.q_alliance_help.body", "Prete main a ton alliance.", "alliance", 3, MissionNavigationKind.Alliance, "alliance", 0, 0, 200),
            new MissionDefinition("q_alliance_message", MissionSectionKind.Quotidiennes, "missions.q_alliance_message.title", "Envoyer un message Alliance", "missions.q_alliance_message.body", "Dialogue dans le canal d'alliance.", "messages", 3, MissionNavigationKind.Chat, "alliance", 150, 0, 0),
            new MissionDefinition("q_champion", MissionSectionKind.Quotidiennes, "missions.q_champion.title", "Utiliser une Championne", "missions.q_champion.body", "Affecte une abeille championne.", "queen", 1, MissionNavigationKind.Champions, "champions", 0, 100, 0),
            new MissionDefinition("q_enemies", MissionSectionKind.Quotidiennes, "missions.q_enemies.title", "Battre des ennemis", "missions.q_enemies.body", "Remporte des combats.", "shield", 5, MissionNavigationKind.Bestiary, "bestiary", 250, 0, 0),
            new MissionDefinition("q_research", MissionSectionKind.Quotidiennes, "missions.q_research.title", "Rechercher une technologie", "missions.q_research.body", "Debloque un noeud de recherche.", "research", 1, MissionNavigationKind.Research, "research", 0, 0, 300),

            // ---- Hebdomadaires ----
            new MissionDefinition("w_collect", MissionSectionKind.Hebdomadaires, "missions.w_collect.title", "Recolter 2 500 miel", "missions.w_collect.body", "Recolte un gros volume d'or.", "honey", 2500, MissionNavigationKind.Collect, "honey_storage", 600, 200, 0),
            new MissionDefinition("w_build", MissionSectionKind.Hebdomadaires, "missions.w_build.title", "Construire 12 niveaux", "missions.w_build.body", "Cumule les niveaux de tous les batiments.", "construct", 12, MissionNavigationKind.Building, "honey_storage", 0, 500, 0),
            new MissionDefinition("w_help", MissionSectionKind.Hebdomadaires, "missions.w_help.title", "Envoyer 15 aides", "missions.w_help.body", "Prete main a ton alliance toute la semaine.", "alliance", 15, MissionNavigationKind.Alliance, "alliance", 0, 0, 200),
            new MissionDefinition("w_chat", MissionSectionKind.Hebdomadaires, "missions.w_chat.title", "Envoyer 20 messages", "missions.w_chat.body", "Une semaine active au canal d'alliance.", "messages", 20, MissionNavigationKind.Chat, "alliance", 300, 0, 150),
            new MissionDefinition("w_enemies", MissionSectionKind.Hebdomadaires, "missions.w_enemies.title", "Remporter 25 combats", "missions.w_enemies.body", "Repousse des vagues ennemies.", "shield", 25, MissionNavigationKind.Bestiary, "bestiary", 900, 0, 0),

            // ---- Defis ----
            new MissionDefinition("d_level_10", MissionSectionKind.Defis, "missions.d_level_10.title", "Atteindre le niveau 10", "missions.d_level_10.body", "Progresse en puissance pour devenir une force.", "crown", 10, MissionNavigationKind.None, null, 2000, 0, 0),
            new MissionDefinition("d_champion", MissionSectionKind.Defis, "missions.d_champion.title", "Debloquer une Championne", "missions.d_champion.body", "Recrute une abeille championne.", "queen", 1, MissionNavigationKind.Champions, "champions", 0, 1500, 0),
            new MissionDefinition("d_building_20", MissionSectionKind.Defis, "missions.d_building_20.title", "Batiment niveau 20", "missions.d_building_20.body", "Porte un batiment jusqu'au niveau 20.", "construct", 20, MissionNavigationKind.Building, "honey_storage", 0, 0, 2500),
            new MissionDefinition("d_power_100k", MissionSectionKind.Defis, "missions.d_power_100k.title", "Obtenir 100 000 puissance", "missions.d_power_100k.body", "Cumule la puissance des batiments, abeilles et recherches.", "shield", 100000, MissionNavigationKind.None, null, 8000, 0, 0),

            // ---- Succes (donnees simulees, connexion future au systeme real) ----
            new MissionDefinition("s_first_colony", MissionSectionKind.Succes, "missions.s_first_colony.title", "Premiere colonie", "missions.s_first_colony.body", "Construis ton premier batiment.", "construct", 1, MissionNavigationKind.Building, "honey_storage", 300, 0, 0, true),
            new MissionDefinition("s_gold", MissionSectionKind.Succes, "missions.s_gold.title", "Fil d'or", "missions.s_gold.body", "Recolte 10 000 miel au total.", "honey", 10000, MissionNavigationKind.Collect, "honey_storage", 0, 0, 1000, true),
            new MissionDefinition("s_architect", MissionSectionKind.Succes, "missions.s_architect.title", "Architecte", "missions.s_architect.body", "Accumule 50 niveaux de batiments.", "construct", 50, MissionNavigationKind.Building, "honey_storage", 0, 1000, 0, true),
            new MissionDefinition("s_scholar", MissionSectionKind.Succes, "missions.s_scholar.title", "Erudit", "missions.s_scholar.body", "Termine 5 recherches.", "research", 5, MissionNavigationKind.Research, "research", 0, 0, 800, true),
            new MissionDefinition("s_legend", MissionSectionKind.Succes, "missions.s_legend.title", "Legende naissante", "missions.s_legend.body", "Debloque 3 championnes.", "queen", 3, MissionNavigationKind.Champions, "champions", 2000, 0, 0, true),
        };

        private static readonly MissionChapterDefinition[] ChaptersInternal =
        {
            new MissionChapterDefinition("ch_1", "missions.ch.1.title", "Chapitre 1 — Le Berceau", "missions.ch.1.subtitle", "Pose les fondations du royaume.", "crown",
                new[] { "q_collect", "q_build", "q_champion" }, 800, 300, 100),
            new MissionChapterDefinition("ch_2", "missions.ch.2.title", "Chapitre 2 — La Premiere Defense", "missions.ch.2.subtitle", "Prepare la defense de ta ruche qui grandit.", "shield",
                new[] { "q_research", "q_enemies", "d_level_10" }, 1500, 600, 900),
            new MissionChapterDefinition("ch_3", "missions.ch.3.title", "Chapitre 3 — L'Expansion Sacree", "missions.ch.3.subtitle", "A l'aube d'un empire.", "crown",
                new[] { "d_building_20", "d_power_100k", "d_champion" }, 5000, 2000, 0),
        };

        public static MissionDefinition[] AllMissions => MissionsInternal;
        public static MissionChapterDefinition[] AllChapters => ChaptersInternal;
        public static int MaxPinned => MaxPinnedMissions;

        public static MissionDefinition Find(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            for (int i = 0; i < MissionsInternal.Length; i++)
            {
                if (string.Equals(MissionsInternal[i].Id, id, StringComparison.Ordinal)) return MissionsInternal[i];
            }
            return null;
        }

        public static MissionChapterDefinition ChapterById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            for (int i = 0; i < ChaptersInternal.Length; i++)
            {
                if (string.Equals(ChaptersInternal[i].Id, id, StringComparison.Ordinal)) return ChaptersInternal[i];
            }
            return null;
        }

        public static MissionDefinition[] MissionsForSection(MissionSectionKind section)
        {
            int count = 0;
            for (int i = 0; i < MissionsInternal.Length; i++)
            {
                if (MissionsInternal[i].Section == section) count++;
            }
            MissionDefinition[] result = new MissionDefinition[count];
            int index = 0;
            for (int i = 0; i < MissionsInternal.Length; i++)
            {
                if (MissionsInternal[i].Section == section) result[index++] = MissionsInternal[i];
            }
            return result;
        }

        public static int MissionCountForSection(MissionSectionKind section)
        {
            int count = 0;
            for (int i = 0; i < MissionsInternal.Length; i++)
            {
                if (MissionsInternal[i].Section == section) count++;
            }
            return count;
        }
    }
}