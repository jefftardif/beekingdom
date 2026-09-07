using BeeKingdom.Playground;

namespace BeeKingdom.Tutorial
{
    // M055-CL - Condition de progression interrogeable par le FTUE.
    //
    // Le FTUE existant est pilote par des EVENEMENTS (FtueEventKind : fenetre ouverte,
    // batiment selectionne, etc.) et n'avait aucune notion de "condition d'etat". M055 ne
    // reconstruit PAS le tutoriel : il se contente d'exposer proprement la question que le
    // FTUE (et la progression Alpha a venir) auront besoin de poser, sans dupliquer la
    // source de verite.
    //
    // SOURCE DE VERITE UNIQUE : le niveau du Palais Royal est le niveau du batiment
    // `administration_core`, resolu par HiveViewProductUiPresenter.RoyalPalaceLevelForExternalHost()
    // (valeur serveur officielle quand une session existe, preview locale sinon) - exactement
    // la meme valeur que celle affichee par la fenetre du Palais Royal et par le profil joueur.
    // Ne JAMAIS introduire ici un compteur parallele.
    public static class FtueRoyalPalaceConditions
    {
        // Identifiant interne reel du Palais Royal cote serveur/persistance.
        public const string RoyalPalaceBuildingKey = "administration_core";

        public static int CurrentRoyalPalaceLevel()
        {
            return HiveViewProductUiPresenter.RoyalPalaceLevelForExternalHost();
        }

        // L'equivalent architectural de "RoyalPalaceLevel >= X" demande par la mission.
        public static bool IsRoyalPalaceAtLeast(int minimumLevel)
        {
            if (minimumLevel <= 0) return true;
            return CurrentRoyalPalaceLevel() >= minimumLevel;
        }

        // Le niveau du Palais Royal EST le niveau de la colonie - un seul et meme compteur.
        public static bool IsColonyAtLeast(int minimumLevel)
        {
            return IsRoyalPalaceAtLeast(minimumLevel);
        }
    }
}
