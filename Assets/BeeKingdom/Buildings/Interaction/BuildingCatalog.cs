using System;
using System.Collections.Generic;

namespace BeeKingdom.Buildings.Interaction
{
    public static class BuildingCatalog
    {
        private static readonly LivingHiveHotspotMetadata[] Hotspots =
        {
            new LivingHiveHotspotMetadata(BuildingLegacyKeys.NurseryCluster, "cell-1-0", 1, "Nurserie",
                "Affectation des jeunes abeilles", "nursery", "preview", "Examiner la couvee",
                "Apercu de developpement - aucune population officielle.", 35),
            new LivingHiveHotspotMetadata(BuildingLegacyKeys.HoneyStorage, "cell-0-0", 2, "Reserve miel",
                "Stockage et lecture des reserves", "honey", "preview", "Ameliorer reserve",
                "Apercu de developpement seulement - aucun stock serveur.", 38),
            new LivingHiveHotspotMetadata(BuildingLegacyKeys.GuardPost, "cell--1-1", 3, "Caserne",
                "Entrainement des troupes de defense", "guard", "active", "Entrainer garde",
                "Entraine des troupes pour defendre la ruche.", 34),
            new LivingHiveHotspotMetadata(BuildingLegacyKeys.DefenseGrowth, "cell-2-0", 4, "Defense",
                "Zone defensive laterale", "defense", "future", "Voir defense",
                "Fonctionnalite a venir.", 26),
            new LivingHiveHotspotMetadata(BuildingLegacyKeys.GeneticsGarden, "cell-2--1", 5, "Genetique",
                "Recherche genetique", "flower", "future", "Etudier genetique",
                "Fonctionnalite a venir.", 32),
            new LivingHiveHotspotMetadata(BuildingLegacyKeys.ResearchNode, "cell--1-0", 6, "Recherche",
                "Recherche et progression scientifique", "research", "active", "Examiner recherche",
                "Debloque des ameliorations pour la colonie.", 36),
            new LivingHiveHotspotMetadata(BuildingLegacyKeys.WarehouseCells, "cell-0-1", 7, "Entrepot",
                "Organisation de stockage", "inventory", "active", "Organiser entrepot",
                "Stocke le pollen recolte par la colonie.", 33),
            new LivingHiveHotspotMetadata(BuildingLegacyKeys.WaxWorkshop, "cell-0-1", 8, "Transformation",
                "Atelier cire et transformation", "production", "active", "Organiser atelier",
                "Transforme les ressources en cire.", 37),
            new LivingHiveHotspotMetadata(BuildingLegacyKeys.InfirmaryGrove, "cell--2-1", 9, "Infirmerie",
                "Soins et soutien", "help", "future", "Voir soins",
                "Fonctionnalite a venir.", 24),
            new LivingHiveHotspotMetadata(BuildingLegacyKeys.AcademyCanopy, "cell-2--2", 10, "Academie",
                "Apprentissage", "book", "future", "Voir academie",
                "Fonctionnalite a venir.", 22),
            new LivingHiveHotspotMetadata(BuildingLegacyKeys.HiveBank, "cell--1-2", 11, "Banque",
                "Gestion financiere", "royal-jelly", "future", "Voir banque",
                "Fonctionnalite a venir.", 23),
            new LivingHiveHotspotMetadata(BuildingLegacyKeys.AdministrationCore, "cell-0-0", 12, "Administration",
                "Coeur royal - centre de gestion de la ruche", "queen-core", "active", "Inspecter le coeur royal",
                "Le coeur de la ruche : son niveau plafonne celui des autres batiments.", 45),
            new LivingHiveHotspotMetadata(BuildingLegacyKeys.AllianceFutureHall, "cell-1-1", 13, "Centre alliance",
                "Entree pour les alliances entre joueurs", "alliance-center", "future", "Voir alliance",
                "Fonctionnalite a venir.", 31),
            new LivingHiveHotspotMetadata(BuildingLegacyKeys.ArchivesHoneyfall, "cell--2--1", 14, "Archives",
                "Historique de la colonie", "quests", "future", "Voir archives",
                "Fonctionnalite a venir.", 20)
        };

        private static readonly Dictionary<string, LivingHiveHotspotMetadata> ByLegacyKey =
            new Dictionary<string, LivingHiveHotspotMetadata>();

        private static readonly BuildingDefinition[] Definitions;

        static BuildingCatalog()
        {
            for (int i = 0; i < Hotspots.Length; i++)
                ByLegacyKey.Add(Hotspots[i].HotspotId, Hotspots[i]);

            Definitions = new BuildingDefinition[Hotspots.Length];
            for (int i = 0; i < Hotspots.Length; i++)
            {
                LivingHiveHotspotMetadata h = Hotspots[i];
                string buildingType = BuildingMappingTable.ToBuildingType(h.HotspotId);
                BuildingState state = h.StateIcon == "future" ? BuildingState.Future
                    : h.StateIcon == "active" ? BuildingState.Active
                    : BuildingState.Preview;
                BuildingCapabilities capabilities = BuildingCapabilitiesResolver.Resolve(buildingType, h.HotspotId);
                Definitions[i] = new BuildingDefinition(
                    buildingType,
                    h.HotspotId,
                    h.Label,
                    h.Role,
                    h.ZoneNumber,
                    h.CellId,
                    h.IconId,
                    state,
                    h.ActionLabel,
                    h.Disclosure,
                    capabilities,
                    BuildingCapabilitiesResolver.ResourceOf(buildingType, h.HotspotId),
                    isUpgradable: (capabilities & BuildingCapabilities.Upgrade) != 0);
            }
        }

        public static IReadOnlyList<BuildingDefinition> All
        {
            get { return Definitions; }
        }

        public static BuildingDefinition GetByBuildingType(string buildingType)
        {
            for (int i = 0; i < Definitions.Length; i++)
            {
                if (Definitions[i].BuildingType == buildingType) return Definitions[i];
            }
            throw new KeyNotFoundException("BuildingDefinition non trouvé pour buildingType " + buildingType);
        }

        public static BuildingDefinition GetByLegacyKey(string legacyKey)
        {
            for (int i = 0; i < Definitions.Length; i++)
            {
                if (Definitions[i].LegacyKey == legacyKey) return Definitions[i];
            }
            throw new KeyNotFoundException("BuildingDefinition non trouvé pour legacyKey " + legacyKey);
        }

        public static bool TryGetByBuildingType(string buildingType, out BuildingDefinition definition)
        {
            definition = null;
            if (string.IsNullOrEmpty(buildingType)) return false;
            for (int i = 0; i < Definitions.Length; i++)
            {
                if (Definitions[i].BuildingType == buildingType)
                {
                    definition = Definitions[i];
                    return true;
                }
            }
            return false;
        }

        public static bool TryGetByLegacyKey(string legacyKey, out BuildingDefinition definition)
        {
            definition = null;
            if (string.IsNullOrEmpty(legacyKey)) return false;
            for (int i = 0; i < Definitions.Length; i++)
            {
                if (Definitions[i].LegacyKey == legacyKey)
                {
                    definition = Definitions[i];
                    return true;
                }
            }
            return false;
        }

        public static LivingHiveHotspotMetadata GetMetadata(string legacyKey)
        {
            LivingHiveHotspotMetadata metadata;
            if (!ByLegacyKey.TryGetValue(legacyKey, out metadata))
                throw new KeyNotFoundException("Metadata non trouvée pour legacyKey " + legacyKey);
            return metadata;
        }
    }
}