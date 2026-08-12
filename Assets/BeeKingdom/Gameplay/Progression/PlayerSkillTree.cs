using System;
using System.Collections.Generic;

namespace BeeKingdom.Gameplay.Progression
{
    public enum PlayerClass
    {
        Neutral,
        RoyalGuard,
        Striker,
        Nurturer,
        Scout,
        Alchemist
    }

    public enum SkillTreeId
    {
        Combat,
        Resources,
        Class
    }

    public sealed class SkillDefinition
    {
        private readonly float[] effectValuesByRank;

        public string SkillId { get; }
        public SkillTreeId TreeId { get; }
        public PlayerClass? ClassId { get; }
        public int RequiredLevel { get; }
        public int MaxRank { get; }
        public int CostPerRank { get; }
        public IReadOnlyList<string> PrerequisiteSkillIds { get; }
        public string EffectKey { get; }

        public SkillDefinition(
            string skillId,
            SkillTreeId treeId,
            PlayerClass? classId,
            int requiredLevel,
            int maxRank,
            int costPerRank,
            string effectKey,
            IReadOnlyList<float> effectValuesByRank,
            IReadOnlyList<string> prerequisiteSkillIds = null)
        {
            if (string.IsNullOrWhiteSpace(skillId)) throw new ArgumentException("Skill id is required.", nameof(skillId));
            if (requiredLevel < 1) throw new ArgumentOutOfRangeException(nameof(requiredLevel));
            if (maxRank < 1) throw new ArgumentOutOfRangeException(nameof(maxRank));
            if (costPerRank < 1) throw new ArgumentOutOfRangeException(nameof(costPerRank));
            if (string.IsNullOrWhiteSpace(effectKey)) throw new ArgumentException("Effect key is required.", nameof(effectKey));
            if (effectValuesByRank == null || effectValuesByRank.Count != maxRank)
            {
                throw new ArgumentException("One effect value is required for each rank.", nameof(effectValuesByRank));
            }

            SkillId = skillId;
            TreeId = treeId;
            ClassId = classId;
            RequiredLevel = requiredLevel;
            MaxRank = maxRank;
            CostPerRank = costPerRank;
            EffectKey = effectKey;
            this.effectValuesByRank = new float[maxRank];
            for (int i = 0; i < maxRank; i++) this.effectValuesByRank[i] = effectValuesByRank[i];
            PrerequisiteSkillIds = prerequisiteSkillIds == null
                ? Array.Empty<string>()
                : new List<string>(prerequisiteSkillIds).AsReadOnly();
        }

        public float EffectValueAtRank(int rank)
        {
            if (rank < 1 || rank > MaxRank) throw new ArgumentOutOfRangeException(nameof(rank));
            return effectValuesByRank[rank - 1];
        }

        public bool IsAvailableFor(PlayerClass playerClass)
        {
            return !ClassId.HasValue || ClassId.Value == playerClass;
        }
    }

    public sealed class SkillCatalog
    {
        public const string Version = "skill_tree_v1";
        private readonly Dictionary<string, SkillDefinition> definitionsById;

        public IReadOnlyCollection<SkillDefinition> Definitions => definitionsById.Values;

        public SkillCatalog(IEnumerable<SkillDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            definitionsById = new Dictionary<string, SkillDefinition>(StringComparer.Ordinal);
            foreach (SkillDefinition definition in definitions)
            {
                if (definition == null) throw new ArgumentException("Catalog cannot contain a null skill.", nameof(definitions));
                if (!definitionsById.TryAdd(definition.SkillId, definition))
                {
                    throw new ArgumentException("Duplicate skill id: " + definition.SkillId, nameof(definitions));
                }
            }

            foreach (SkillDefinition definition in definitionsById.Values)
            {
                foreach (string prerequisiteId in definition.PrerequisiteSkillIds)
                {
                    if (!definitionsById.ContainsKey(prerequisiteId))
                    {
                        throw new ArgumentException("Unknown prerequisite '" + prerequisiteId + "' for '" + definition.SkillId + "'.", nameof(definitions));
                    }
                }
            }
        }

        public SkillDefinition Get(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId)) throw new ArgumentException("Skill id is required.", nameof(skillId));
            if (!definitionsById.TryGetValue(skillId, out SkillDefinition definition))
            {
                throw new KeyNotFoundException("Unknown skill id: " + skillId);
            }
            return definition;
        }

        public bool TryGet(string skillId, out SkillDefinition definition)
        {
            return definitionsById.TryGetValue(skillId, out definition);
        }

        public static SkillCatalog CreateDefault()
        {
            var definitions = new List<SkillDefinition>();
            AddCommonDefinitions(definitions);
            AddClassDefinitions(definitions, PlayerClass.RoyalGuard, "royalguard");
            AddClassDefinitions(definitions, PlayerClass.Striker, "striker");
            AddClassDefinitions(definitions, PlayerClass.Nurturer, "nurturer");
            AddClassDefinitions(definitions, PlayerClass.Scout, "scout");
            AddClassDefinitions(definitions, PlayerClass.Alchemist, "alchemist");
            return new SkillCatalog(definitions);
        }

        private static void AddCommonDefinitions(List<SkillDefinition> definitions)
        {
            definitions.Add(Common("combat_foundation", SkillTreeId.Combat, 3, "combat.damage_percent", 0.02f, 0.04f, 0.06f));
            definitions.Add(Common("combat_vitality", SkillTreeId.Combat, 3, "combat.max_health_percent", 0.03f, 0.06f, 0.09f));
            definitions.Add(Common("combat_command", SkillTreeId.Combat, 3, "combat.soldier_power_percent", 0.03f, 0.06f, 0.09f, "combat_foundation"));
            definitions.Add(Common("combat_guard", SkillTreeId.Combat, 3, "combat.damage_taken_percent", -0.02f, -0.04f, -0.06f, "combat_vitality"));
            definitions.Add(Common("combat_focus", SkillTreeId.Combat, 2, "combat.elite_damage_percent", 0.05f, 0.10f, "combat_command"));
            definitions.Add(Common("combat_swarm", SkillTreeId.Combat, 3, "combat.attack_cooldown_percent", -0.02f, -0.04f, -0.06f, "combat_command"));
            definitions.Add(Common("combat_counter", SkillTreeId.Combat, 3, "combat.counter_chance", 0.03f, 0.06f, 0.09f, "combat_guard"));
            definitions.Add(Common("combat_raid", SkillTreeId.Combat, 3, "combat.raid_efficiency_percent", 0.03f, 0.06f, 0.09f, "combat_focus", "combat_swarm"));
            definitions.Add(Common("combat_last_stand", SkillTreeId.Combat, 1, "combat.emergency_guard", 1f, "combat_guard", "combat_vitality"));
            definitions.Add(Common("combat_mastery", SkillTreeId.Combat, 1, "combat.mastery_percent", 0.05f, "combat_last_stand"));

            definitions.Add(Common("resource_foraging", SkillTreeId.Resources, 3, "resource.collection_speed_percent", 0.04f, 0.08f, 0.12f));
            definitions.Add(Common("resource_sense", SkillTreeId.Resources, 3, "resource.detection_percent", 0.04f, 0.08f, 0.12f));
            definitions.Add(Common("resource_capacity", SkillTreeId.Resources, 3, "resource.transport_capacity_percent", 0.05f, 0.10f, 0.15f, "resource_foraging"));
            definitions.Add(Common("resource_route", SkillTreeId.Resources, 3, "resource.travel_time_percent", -0.03f, -0.06f, -0.09f, "resource_sense"));
            definitions.Add(Common("resource_refine", SkillTreeId.Resources, 3, "resource.refinement_yield_percent", 0.03f, 0.06f, 0.09f, "resource_capacity"));
            definitions.Add(Common("resource_construction", SkillTreeId.Resources, 3, "resource.construction_cost_percent", -0.02f, -0.04f, -0.06f, "resource_capacity"));
            definitions.Add(Common("resource_rare", SkillTreeId.Resources, 2, "resource.rare_detection_percent", 0.05f, 0.10f, "resource_sense"));
            definitions.Add(Common("resource_recovery", SkillTreeId.Resources, 3, "resource.recovery_percent", 0.03f, 0.06f, 0.09f, "resource_refine", "resource_route"));
            definitions.Add(Common("resource_specialist", SkillTreeId.Resources, 1, "resource.global_efficiency_percent", 0.05f, "resource_recovery"));
            definitions.Add(Common("resource_mastery", SkillTreeId.Resources, 1, "resource.evolution_queue_bonus", 1f, "resource_specialist"));
        }

        private static void AddClassDefinitions(List<SkillDefinition> definitions, PlayerClass classId, string prefix)
        {
            definitions.Add(ClassSkill(prefix + "_foundation", classId, prefix + ".identity_percent", 3, null, 0.03f, 0.06f, 0.09f));
            definitions.Add(ClassSkill(prefix + "_specialist", classId, prefix + ".specialist_percent", 3, prefix + "_foundation", 0.03f, 0.06f, 0.09f));
            definitions.Add(ClassSkill(prefix + "_mastery", classId, prefix + ".mastery_percent", 2, prefix + "_specialist", 0.05f, 0.10f));
            definitions.Add(ClassSkill(prefix + "_signature", classId, prefix + ".signature_percent", 2, prefix + "_foundation", 0.05f, 0.10f));
            definitions.Add(ClassSkill(prefix + "_capstone", classId, prefix + ".capstone", 1, prefix + "_mastery", 1f));
        }

        private static SkillDefinition Common(string id, SkillTreeId tree, int maxRank, string effectKey, params object[] valuesAndPrerequisites)
        {
            return Build(id, tree, null, maxRank, effectKey, valuesAndPrerequisites);
        }

        private static SkillDefinition ClassSkill(string id, PlayerClass classId, string effectKey, int maxRank, string prerequisite, params float[] values)
        {
            IReadOnlyList<string> prerequisites = string.IsNullOrEmpty(prerequisite)
                ? Array.Empty<string>()
                : new[] { prerequisite };
            return new SkillDefinition(id, SkillTreeId.Class, classId, 10, maxRank, 1, effectKey, values, prerequisites);
        }

        private static SkillDefinition Build(string id, SkillTreeId tree, PlayerClass? classId, int maxRank, string effectKey, params object[] valuesAndPrerequisites)
        {
            var values = new List<float>();
            var prerequisites = new List<string>();
            foreach (object value in valuesAndPrerequisites)
            {
                if (value is float floatValue) values.Add(floatValue);
                else if (value is double doubleValue) values.Add((float)doubleValue);
                else if (value is string stringValue) prerequisites.Add(stringValue);
                else throw new ArgumentException("Unsupported skill value type.", nameof(valuesAndPrerequisites));
            }

            return new SkillDefinition(id, tree, classId, 10, maxRank, 1, effectKey, values, prerequisites);
        }
    }

    public static class PlayerXpCurve
    {
        public const int MaxLevel = 50;

        public static int XpToNextLevel(int level)
        {
            if (level < 1 || level >= MaxLevel) return 0;
            return Math.Max(1, (int)Math.Round(100d * Math.Pow(level, 1.65d), MidpointRounding.AwayFromZero));
        }

        public static long CumulativeXpForLevel(int level)
        {
            if (level < 1 || level > MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            long total = 0;
            for (int current = 1; current < level; current++) total += XpToNextLevel(current);
            return total;
        }

        public static int LevelForTotalXp(long totalExperience)
        {
            if (totalExperience < 0) throw new ArgumentOutOfRangeException(nameof(totalExperience));
            for (int level = MaxLevel; level >= 1; level--)
            {
                if (totalExperience >= CumulativeXpForLevel(level)) return level;
            }
            return 1;
        }
    }

    public sealed class PlayerSkillProfile
    {
        private readonly Dictionary<string, float> bonuses;

        public PlayerClass ClassId { get; }
        public IReadOnlyDictionary<string, float> Bonuses => bonuses;

        internal PlayerSkillProfile(PlayerClass classId, IDictionary<string, float> bonuses)
        {
            ClassId = classId;
            this.bonuses = new Dictionary<string, float>(bonuses, StringComparer.Ordinal);
        }

        public float GetBonus(string effectKey)
        {
            return bonuses.TryGetValue(effectKey, out float value) ? value : 0f;
        }
    }

    public sealed class PlayerSkillState
    {
        private readonly SkillCatalog catalog;
        private readonly Dictionary<string, int> ranks = new Dictionary<string, int>(StringComparer.Ordinal);

        public const int ClassUnlockLevel = 10;
        public SkillCatalog Catalog => catalog;
        public long TotalExperience { get; private set; }
        public int Level { get; private set; }
        public PlayerClass ClassId { get; private set; }
        public bool IsLocalPreview { get; }
        public int SkillPointsAwarded => Level;
        public int SkillPointsSpent { get; private set; }
        public int SkillPointsUnspent => SkillPointsAwarded - SkillPointsSpent;
        public IReadOnlyDictionary<string, int> Ranks => ranks;

        public PlayerSkillState(SkillCatalog catalog, bool isLocalPreview = false)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            IsLocalPreview = isLocalPreview;
            Level = 1;
            ClassId = PlayerClass.Neutral;
        }

        public int AddExperience(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            int previousLevel = Level;
            long maxExperience = PlayerXpCurve.CumulativeXpForLevel(PlayerXpCurve.MaxLevel);
            TotalExperience = Math.Min(maxExperience, TotalExperience + amount);
            Level = PlayerXpCurve.LevelForTotalXp(TotalExperience);
            return Level - previousLevel;
        }

        public bool TryChooseClass(PlayerClass classId, out string error)
        {
            if (Level < ClassUnlockLevel) return Fail("Class selection requires level 10.", out error);
            if (classId == PlayerClass.Neutral) return Fail("Neutral cannot be selected as a level 10 class.", out error);
            if (ClassId != PlayerClass.Neutral && ClassId != classId) return Fail("Class is already selected; re-specialization is required.", out error);
            ClassId = classId;
            error = string.Empty;
            return true;
        }

        public bool TryPurchase(string skillId, out string error)
        {
            if (!catalog.TryGet(skillId, out SkillDefinition definition)) return Fail("Unknown skill.", out error);
            if (Level < definition.RequiredLevel) return Fail("Required level is not reached.", out error);
            if (!definition.IsAvailableFor(ClassId)) return Fail("Skill does not belong to the selected class.", out error);
            if (definition.TreeId == SkillTreeId.Class && ClassId == PlayerClass.Neutral) return Fail("Choose a class before purchasing class skills.", out error);

            int currentRank = GetRank(skillId);
            if (currentRank >= definition.MaxRank) return Fail("Skill is already at maximum rank.", out error);
            foreach (string prerequisiteId in definition.PrerequisiteSkillIds)
            {
                if (GetRank(prerequisiteId) < 1) return Fail("Prerequisites are not satisfied.", out error);
            }
            if (SkillPointsUnspent < definition.CostPerRank) return Fail("Not enough skill points.", out error);

            ranks[skillId] = currentRank + 1;
            SkillPointsSpent += definition.CostPerRank;
            error = string.Empty;
            return true;
        }

        public bool TryResetSkills(out string error)
        {
            ranks.Clear();
            SkillPointsSpent = 0;
            error = string.Empty;
            return true;
        }

        public bool TrySetLocalLevel(int level, out string error)
        {
            if (!IsLocalPreview) return Fail("Level override is available only in the local preview.", out error);
            if (level < 1 || level > PlayerXpCurve.MaxLevel) return Fail("Level is outside 1..50.", out error);
            Level = level;
            TotalExperience = PlayerXpCurve.CumulativeXpForLevel(level);
            TryResetSkills(out error);
            if (level < ClassUnlockLevel) ClassId = PlayerClass.Neutral;
            return string.IsNullOrEmpty(error);
        }

        public bool TrySetLocalClass(PlayerClass classId, out string error)
        {
            if (!IsLocalPreview) return Fail("Class override is available only in the local preview.", out error);
            if (Level < ClassUnlockLevel) return Fail("Class override requires level 10.", out error);
            if (classId == PlayerClass.Neutral) return Fail("Neutral cannot be selected after level 10.", out error);
            ClassId = classId;
            TryResetSkills(out error);
            return string.IsNullOrEmpty(error);
        }

        public int GetRank(string skillId)
        {
            return ranks.TryGetValue(skillId, out int rank) ? rank : 0;
        }

        public PlayerSkillProfile BuildProfile()
        {
            var totals = new Dictionary<string, float>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, int> pair in ranks)
            {
                SkillDefinition definition = catalog.Get(pair.Key);
                float value = definition.EffectValueAtRank(pair.Value);
                if (totals.TryGetValue(definition.EffectKey, out float existing)) totals[definition.EffectKey] = existing + value;
                else totals[definition.EffectKey] = value;
            }
            return new PlayerSkillProfile(ClassId, totals);
        }

        public static PlayerSkillState CreateLocalPreview(SkillCatalog catalog, int level, PlayerClass classId)
        {
            var state = new PlayerSkillState(catalog, true);
            if (!state.TrySetLocalLevel(level, out string error)) throw new InvalidOperationException(error);
            if (level >= ClassUnlockLevel && classId != PlayerClass.Neutral && !state.TrySetLocalClass(classId, out error)) throw new InvalidOperationException(error);
            return state;
        }

        private static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }
    }
}
