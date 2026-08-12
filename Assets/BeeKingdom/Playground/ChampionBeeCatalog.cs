using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public enum ChampionBeeRole
    {
        Guardians,
        Wingrunners,
        Darters,
        Civilian
    }

    public enum ChampionBeeRarity
    {
        Rare,
        Legendary
    }

    public sealed class ChampionBeeDefinition
    {
        public ChampionBeeDefinition(
            string id,
            string fallbackName,
            string fallbackLore,
            ChampionBeeRole role,
            ChampionBeeRarity rarity,
            int armyBonusPerLevel,
            float roleStatBonusPercentPerLevel,
            float globalStatBonusPercentPerLevel,
            float productionBonusPercentPerLevel)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            FallbackName = fallbackName ?? throw new ArgumentNullException(nameof(fallbackName));
            FallbackLore = fallbackLore ?? string.Empty;
            Role = role;
            Rarity = rarity;
            ArmyBonusPerLevel = Mathf.Max(0, armyBonusPerLevel);
            RoleStatBonusPercentPerLevel = Mathf.Max(0f, roleStatBonusPercentPerLevel);
            GlobalStatBonusPercentPerLevel = Mathf.Max(0f, globalStatBonusPercentPerLevel);
            ProductionBonusPercentPerLevel = Mathf.Max(0f, productionBonusPercentPerLevel);
        }

        public string Id { get; }
        public string FallbackName { get; }
        public string FallbackLore { get; }
        public ChampionBeeRole Role { get; }
        public ChampionBeeRarity Rarity { get; }

        // Bonus de taille d'armee (capacite de troupes envoyees) par niveau, applique uniquement
        // pour les roles de combat (Guardians/Wingrunners/Darters) sur la famille correspondante.
        public int ArmyBonusPerLevel { get; }

        // Bonus de statistique (%) par niveau, applique a la famille de troupe correspondant au role.
        public float RoleStatBonusPercentPerLevel { get; }

        // Petit bonus de statistique (%) par niveau, applique a l'ensemble de l'escouade de combat.
        public float GlobalStatBonusPercentPerLevel { get; }

        // Bonus de production (%) par niveau, applique uniquement pour le role Civilian a la
        // recolte manuelle de ressources.
        public float ProductionBonusPercentPerLevel { get; }
    }

    public static class ChampionBeeCatalog
    {
        public const string Version = "champion-bees-v1";

        // Niveau du Coeur royal (hotspot "administration_core") requis pour pouvoir obtenir une
        // abeille championne d'une rarete donnee (evenement ou achat) - v1, ajustable avec Jeff.
        public const int RareUnlockCoeurRoyalLevel = 3;
        public const int LegendaryUnlockCoeurRoyalLevel = 10;

        private static readonly ChampionBeeDefinition[] Definitions =
        {
            new ChampionBeeDefinition(
                "striga", "Striga",
                "Striga a tenu la breche de la Nursery-Sud lors du grand effondrement de rayon, protegeant trois couvees a elle seule pendant que les secours arrivaient. Elle porte depuis une cicatrice en forme de croissant sur l'aile gauche - et plus aucune gardienne ne recule devant elle.",
                ChampionBeeRole.Guardians, ChampionBeeRarity.Rare, 15, 3f, 0.5f, 0f),
            new ChampionBeeDefinition(
                "zephyra", "Zephyra",
                "Zephyra detient le record de vitesse de la ruche pour avoir relie l'Entrepot au Poste de garde en moins de neuf battements d'ailes, un exploit jamais egale depuis. Elle forme aujourd'hui les jeunes voltigeuses aux couloirs d'urgence.",
                ChampionBeeRole.Wingrunners, ChampionBeeRarity.Rare, 15, 3f, 0.5f, 0f),
            new ChampionBeeDefinition(
                "ambra", "Ambra",
                "Ambra a repousse seule un essaim de frelons eclaireurs venus sonder les defenses exterieures, tirant vingt-trois volees de dard sans jamais manquer sa cible. Son nom est desormais chuchote par les recrues comme une mise en garde aux envahisseurs.",
                ChampionBeeRole.Darters, ChampionBeeRarity.Legendary, 30, 5f, 1f, 0f),
            new ChampionBeeDefinition(
                "nectaria", "Nectaria",
                "Nectaria a cartographie sept nouveaux champs de trefle en une seule saison, doublant les reserves de la ruche avant l'hiver. Les butineuses suivent encore aujourd'hui les routes qu'elle a tracees.",
                ChampionBeeRole.Civilian, ChampionBeeRarity.Rare, 0, 0f, 0f, 2f),
            new ChampionBeeDefinition(
                "aurelia", "Aurelia",
                "Aurelia a survecu a un hiver que la ruche croyait ne jamais voir finir, rationnant le pollen goutte par goutte jusqu'au degel. On raconte qu'elle refuse encore de gaspiller la moindre miette.",
                ChampionBeeRole.Civilian, ChampionBeeRarity.Legendary, 0, 0f, 0f, 4f)
        };

        public static IReadOnlyList<ChampionBeeDefinition> All => Definitions;

        public static bool TryResolve(string id, out ChampionBeeDefinition definition)
        {
            for (int index = 0; index < Definitions.Length; index++)
            {
                ChampionBeeDefinition candidate = Definitions[index];
                if (!string.Equals(candidate.Id, id, StringComparison.Ordinal)) continue;
                definition = candidate;
                return true;
            }

            definition = null;
            return false;
        }

        public static int UnlockCoeurRoyalLevel(ChampionBeeRarity rarity)
        {
            return rarity == ChampionBeeRarity.Legendary ? LegendaryUnlockCoeurRoyalLevel : RareUnlockCoeurRoyalLevel;
        }

        public static Vector2 LevelUpCost(ChampionBeeDefinition definition, int currentLevel)
        {
            int level = Mathf.Max(1, currentLevel);
            bool legendary = definition.Rarity == ChampionBeeRarity.Legendary;
            float honey = legendary ? 300f + level * 220f : 150f + level * 90f;
            float pollen = legendary ? 120f + level * 70f : 60f + level * 30f;
            return new Vector2(honey, pollen);
        }

        public static string CombatFamilyId(ChampionBeeRole role)
        {
            switch (role)
            {
                case ChampionBeeRole.Guardians: return "guardians";
                case ChampionBeeRole.Wingrunners: return "wingrunners";
                case ChampionBeeRole.Darters: return "darters";
                default: return string.Empty;
            }
        }
    }
}
