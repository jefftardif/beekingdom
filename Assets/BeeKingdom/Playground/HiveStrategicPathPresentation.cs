using System;
using System.Collections.Generic;
using BeeKingdom.Gameplay.Progression;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed class HiveStrategicPathDefinition
    {
        public HiveStrategicPathDefinition(
            PlayerClass classId,
            string token,
            string iconId,
            Color accent,
            string trialScenarioId,
            int preferredTrialChoice)
        {
            if (classId == PlayerClass.Neutral) throw new ArgumentException("Neutral is not a selectable strategic path.", nameof(classId));
            if (preferredTrialChoice < 0 || preferredTrialChoice > 1) throw new ArgumentOutOfRangeException(nameof(preferredTrialChoice));
            ClassId = classId;
            Token = token ?? string.Empty;
            IconId = iconId ?? string.Empty;
            Accent = accent;
            TrialScenarioId = trialScenarioId ?? string.Empty;
            PreferredTrialChoice = preferredTrialChoice;
        }

        public PlayerClass ClassId { get; }
        public string Token { get; }
        public string IconId { get; }
        public Color Accent { get; }
        public string TrialScenarioId { get; }
        public int PreferredTrialChoice { get; }
        public string NameKey => "strategic_path." + Token + ".name";
        public string RoleKey => "strategic_path." + Token + ".role";
        public string SummaryKey => "strategic_path." + Token + ".summary";
        public string StrengthOneKey => "strategic_path." + Token + ".strength_1";
        public string StrengthTwoKey => "strategic_path." + Token + ".strength_2";
        public string TradeoffKey => "strategic_path." + Token + ".tradeoff";
        public string TrialScenarioKey => "strategic_path." + Token + ".trial.scenario";
        public string TrialChoiceOneKey => "strategic_path." + Token + ".trial.choice_1";
        public string TrialChoiceTwoKey => "strategic_path." + Token + ".trial.choice_2";
        public string TrialFitKey => "strategic_path." + Token + ".trial.fit";
        public string TrialTradeoffKey => "strategic_path." + Token + ".trial.tradeoff";
    }

    public static class HiveStrategicPathCatalog
    {
        private static readonly HiveStrategicPathDefinition[] Entries =
        {
            new HiveStrategicPathDefinition(PlayerClass.RoyalGuard, "royal_guard", "guard-bee", new Color(0.28f, 0.72f, 1f, 0.96f), "hold_brood_gate", 0),
            new HiveStrategicPathDefinition(PlayerClass.Striker, "striker", "sword", new Color(1f, 0.42f, 0.25f, 0.96f), "break_threat_leader", 1),
            new HiveStrategicPathDefinition(PlayerClass.Nurturer, "nurturer", "nursery", new Color(0.36f, 0.92f, 0.68f, 0.96f), "restore_exhausted_escort", 0),
            new HiveStrategicPathDefinition(PlayerClass.Scout, "scout", "map", new Color(0.92f, 0.82f, 0.28f, 0.96f), "survey_orchard_route", 1),
            new HiveStrategicPathDefinition(PlayerClass.Alchemist, "alchemist", "research", new Color(0.74f, 0.46f, 1f, 0.96f), "prepare_propolis_counter", 0)
        };

        public static IReadOnlyList<HiveStrategicPathDefinition> All => Entries;

        public static bool TryResolve(PlayerClass classId, out HiveStrategicPathDefinition definition)
        {
            for (int index = 0; index < Entries.Length; index++)
            {
                if (Entries[index].ClassId != classId) continue;
                definition = Entries[index];
                return true;
            }
            definition = null;
            return false;
        }

        public static bool TryResolve(string token, out HiveStrategicPathDefinition definition)
        {
            for (int index = 0; index < Entries.Length; index++)
            {
                if (!string.Equals(Entries[index].Token, token, StringComparison.Ordinal)) continue;
                definition = Entries[index];
                return true;
            }
            definition = null;
            return false;
        }
    }

    public enum HiveCombatDoctrineFamily
    {
        Guardians = 0,
        Wingrunners = 1,
        Darters = 2
    }

    public enum HiveCombatDoctrineOutcome
    {
        Pending = 0,
        Even = 1,
        Advantage = 2,
        Vulnerable = 3
    }

    public sealed class HiveCombatDoctrineDefinition
    {
        public HiveCombatDoctrineDefinition(
            HiveCombatDoctrineFamily family,
            string token,
            string iconId,
            Color accent,
            HiveCombatDoctrineFamily beats,
            HiveCombatDoctrineFamily losesTo)
        {
            Family = family;
            Token = token ?? string.Empty;
            IconId = iconId ?? string.Empty;
            Accent = accent;
            Beats = beats;
            LosesTo = losesTo;
        }

        public HiveCombatDoctrineFamily Family { get; }
        public string Token { get; }
        public string IconId { get; }
        public Color Accent { get; }
        public HiveCombatDoctrineFamily Beats { get; }
        public HiveCombatDoctrineFamily LosesTo { get; }
        public string NameKey => "combat_doctrine.family." + Token + ".name";
        public string RoleKey => "combat_doctrine.family." + Token + ".role";
        public string TechniqueKey => "combat_doctrine.family." + Token + ".technique";
    }

    public static class HiveCombatDoctrineCatalog
    {
        public const string Version = "phase4-combat-v1";

        private static readonly HiveCombatDoctrineDefinition[] Entries =
        {
            new HiveCombatDoctrineDefinition(
                HiveCombatDoctrineFamily.Guardians,
                "guardians",
                "guard-bee",
                new Color(0.28f, 0.72f, 1f, 0.96f),
                HiveCombatDoctrineFamily.Darters,
                HiveCombatDoctrineFamily.Wingrunners),
            new HiveCombatDoctrineDefinition(
                HiveCombatDoctrineFamily.Wingrunners,
                "wingrunners",
                "map",
                new Color(0.94f, 0.78f, 0.26f, 0.96f),
                HiveCombatDoctrineFamily.Guardians,
                HiveCombatDoctrineFamily.Darters),
            new HiveCombatDoctrineDefinition(
                HiveCombatDoctrineFamily.Darters,
                "darters",
                "sword",
                new Color(0.78f, 0.46f, 1f, 0.96f),
                HiveCombatDoctrineFamily.Wingrunners,
                HiveCombatDoctrineFamily.Guardians)
        };

        public static IReadOnlyList<HiveCombatDoctrineDefinition> All => Entries;

        public static bool TryResolve(HiveCombatDoctrineFamily family, out HiveCombatDoctrineDefinition definition)
        {
            for (int index = 0; index < Entries.Length; index++)
            {
                if (Entries[index].Family != family) continue;
                definition = Entries[index];
                return true;
            }

            definition = null;
            return false;
        }

        public static bool TryResolve(string token, out HiveCombatDoctrineDefinition definition)
        {
            for (int index = 0; index < Entries.Length; index++)
            {
                if (!string.Equals(Entries[index].Token, token, StringComparison.OrdinalIgnoreCase)) continue;
                definition = Entries[index];
                return true;
            }

            definition = null;
            return false;
        }

        public static HiveCombatDoctrineOutcome Evaluate(HiveCombatDoctrineFamily attacker, HiveCombatDoctrineFamily defender)
        {
            if (attacker == defender) return HiveCombatDoctrineOutcome.Even;
            if (!TryResolve(attacker, out HiveCombatDoctrineDefinition definition)) return HiveCombatDoctrineOutcome.Pending;
            if (definition.Beats == defender) return HiveCombatDoctrineOutcome.Advantage;
            if (definition.LosesTo == defender) return HiveCombatDoctrineOutcome.Vulnerable;
            return HiveCombatDoctrineOutcome.Pending;
        }
    }
}
