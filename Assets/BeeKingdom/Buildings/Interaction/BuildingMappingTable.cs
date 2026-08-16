using System;
using System.Collections.Generic;

namespace BeeKingdom.Buildings.Interaction
{
    public sealed class LegacyMappingEntry
    {
        public string BuildingType { get; }
        public string LegacyKey { get; }

        public LegacyMappingEntry(string buildingType, string legacyKey)
        {
            BuildingType = buildingType;
            LegacyKey = legacyKey;
        }
    }

    public static class BuildingMappingTable
    {
        private static readonly LegacyMappingEntry[] Entries =
        {
            new LegacyMappingEntry(BuildingTypes.Nursery, BuildingLegacyKeys.NurseryCluster),
            new LegacyMappingEntry(BuildingTypes.HoneyReserve, BuildingLegacyKeys.HoneyStorage),
            new LegacyMappingEntry(BuildingTypes.Barrack, BuildingLegacyKeys.GuardPost),
            new LegacyMappingEntry(BuildingTypes.Defense, BuildingLegacyKeys.DefenseGrowth),
            new LegacyMappingEntry(BuildingTypes.Genetics, BuildingLegacyKeys.GeneticsGarden),
            new LegacyMappingEntry(BuildingTypes.Research, BuildingLegacyKeys.ResearchNode),
            new LegacyMappingEntry(BuildingTypes.Warehouse, BuildingLegacyKeys.WarehouseCells),
            new LegacyMappingEntry(BuildingTypes.Transformation, BuildingLegacyKeys.WaxWorkshop),
            new LegacyMappingEntry(BuildingTypes.Infirmary, BuildingLegacyKeys.InfirmaryGrove),
            new LegacyMappingEntry(BuildingTypes.Academy, BuildingLegacyKeys.AcademyCanopy),
            new LegacyMappingEntry(BuildingTypes.Bank, BuildingLegacyKeys.HiveBank),
            new LegacyMappingEntry(BuildingTypes.RoyalPalace, BuildingLegacyKeys.AdministrationCore),
            new LegacyMappingEntry(BuildingTypes.AllianceCenter, BuildingLegacyKeys.AllianceFutureHall),
            new LegacyMappingEntry(BuildingTypes.ChampionHall, BuildingLegacyKeys.ArchivesHoneyfall)
        };

        private static readonly Dictionary<string, LegacyMappingEntry> ByBuildingType = new Dictionary<string, LegacyMappingEntry>();
        private static readonly Dictionary<string, LegacyMappingEntry> ByLegacyKey = new Dictionary<string, LegacyMappingEntry>();

        static BuildingMappingTable()
        {
            Validate(Entries);
            for (int i = 0; i < Entries.Length; i++)
            {
                ByBuildingType.Add(Entries[i].BuildingType, Entries[i]);
                ByLegacyKey.Add(Entries[i].LegacyKey, Entries[i]);
            }
        }

        public static int Count
        {
            get { return Entries.Length; }
        }

        public static IReadOnlyList<LegacyMappingEntry> All
        {
            get { return Entries; }
        }

        public static LegacyMappingEntry GetByBuildingType(string buildingType)
        {
            if (buildingType == null) throw new ArgumentNullException("buildingType");
            LegacyMappingEntry entry;
            if (!ByBuildingType.TryGetValue(buildingType, out entry))
                throw new KeyNotFoundException("Mapping non trouvé pour buildingType " + buildingType);
            return entry;
        }

        public static LegacyMappingEntry GetByLegacyKey(string legacyKey)
        {
            if (legacyKey == null) throw new ArgumentNullException("legacyKey");
            LegacyMappingEntry entry;
            if (!ByLegacyKey.TryGetValue(legacyKey, out entry))
                throw new KeyNotFoundException("Mapping non trouvé pour legacyKey " + legacyKey);
            return entry;
        }

        public static bool TryGetByBuildingType(string buildingType, out LegacyMappingEntry entry)
        {
            entry = null;
            if (buildingType == null) return false;
            return ByBuildingType.TryGetValue(buildingType, out entry);
        }

        public static bool TryGetByLegacyKey(string legacyKey, out LegacyMappingEntry entry)
        {
            entry = null;
            if (legacyKey == null) return false;
            return ByLegacyKey.TryGetValue(legacyKey, out entry);
        }

        public static string ToLegacyKey(string buildingType)
        {
            return GetByBuildingType(buildingType).LegacyKey;
        }

        public static string ToBuildingType(string legacyKey)
        {
            return GetByLegacyKey(legacyKey).BuildingType;
        }

        public static void Validate(IEnumerable<LegacyMappingEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException("entries");
            List<LegacyMappingEntry> list = new List<LegacyMappingEntry>(entries);
            if (list.Count != BuildingTypes.All.Length)
                throw new InvalidOperationException("BuildingMappingTable invalide : " + list.Count +
                                                    " entrées au lieu de 14 (" + BuildingTypes.All.Length + ").");

            HashSet<string> types = new HashSet<string>();
            HashSet<string> legacy = new HashSet<string>();
            for (int i = 0; i < list.Count; i++)
            {
                LegacyMappingEntry e = list[i];
                if (e == null || string.IsNullOrEmpty(e.BuildingType) || string.IsNullOrEmpty(e.LegacyKey))
                    throw new InvalidOperationException("BuildingMappingTable invalide : entrée vide à l'index " + i + ".");
                if (!types.Add(e.BuildingType))
                    throw new InvalidOperationException("BuildingMappingTable invalide : buildingType dupliqué '" + e.BuildingType + "'.");
                if (!legacy.Add(e.LegacyKey))
                    throw new InvalidOperationException("BuildingMappingTable invalide : legacyKey dupliquée '" + e.LegacyKey + "'.");
            }

            for (int i = 0; i < BuildingTypes.All.Length; i++)
            {
                if (!types.Contains(BuildingTypes.All[i]))
                    throw new InvalidOperationException("BuildingMappingTable invalide : buildingType '" + BuildingTypes.All[i] + "' manquant.");
            }

            for (int i = 0; i < BuildingLegacyKeys.All.Length; i++)
            {
                if (!legacy.Contains(BuildingLegacyKeys.All[i]))
                    throw new InvalidOperationException("BuildingMappingTable invalide : legacyKey '" + BuildingLegacyKeys.All[i] + "' manquante.");
            }
        }
    }
}