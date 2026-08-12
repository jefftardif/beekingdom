namespace BeeKingdom.Core.Data
{
    /// <summary>
    /// Types de bâtiments disponibles dans Bee Kingdom
    /// </summary>
    public enum BuildingType
    {
        // CORE BUILDINGS
        QueensChamber,      // Bâtiment principal
        
        // STORAGE
        HoneyStorage,       // Stockage de miel
        PollenStorage,      // Stockage de pollen
        WaxStorage,         // Stockage de cire
        RoyalJellyVault,    // Stockage de gelée royale
        
        // PRODUCTION
        HoneyFarm,          // Produit du miel
        FlowerGarden,       // Produit du pollen
        WaxWorkshop,        // Produit de la cire
        RoyalJellyLab,      // Produit de la gelée royale
        
        // MILITARY
        Barracks,           // Entraîne les abeilles
        DefenseTower,       // Défend la ruche
        HealingHut,         // Soigne les abeilles
        TrainingGround,     // Améliore les stats
        
        // SPECIAL
        ResearchLab,        // Recherche de technologies
        Market,             // Échange de ressources
        Academy,            // Formation d'abeilles spéciales
        AllianceHall,       // Gestion d'alliance
        ScoutTower,         // Exploration
        
        // DECORATIVE
        Garden,             // Bonus de bonheur
        FestivalGrounds,    // Événements spéciaux
        
        // EMPTY
        Empty               // Slot vide
    }
}
