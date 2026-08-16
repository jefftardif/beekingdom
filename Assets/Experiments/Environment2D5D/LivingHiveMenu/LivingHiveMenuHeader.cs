using UnityEngine;

namespace BeeKingdom.LivingHiveMenu
{
    // DONNÉES ET GÉOMÉTRIE du Header supérieur, sans référence Unity.UI.
    //
    // Le monolithe (HiveViewProductUiPresenter) n'instancie aucune économie dans
    // Environment2D5D : ses valeurs affichées sont des previews locales
    // (localPreviewHoney=125800, ... L.615-621). Le Header reproduit cette logique :
    // LivingHiveMenuHeaderData centralise ici les previews (une seule source locale,
    // conforme à DrawPortraitTopHud/DrawStrategyTopHud, FPOINT DE BRANCHEMENT futur
    // quand l'économie réelle existera — remplacer les constantes ci-dessous).
    public static class LivingHiveMenuHeaderData
    {
        // --- Preview locales (miroir des valeurs statiques du monolithe) ---
        public const int PreviewHoney = 125800;
        public const int PreviewWax = 72300;
        public const int PreviewPollen = 98450;
        public const int PreviewBees = 52300;
        public const int PreviewCapacityUsed = 400;
        public const int PreviewCapacityMax = 600;
        public const int PreviewQueenLevel = 3;

        // --- Accents par ressource (miroir ResourceAccentColor du monolithe L.22133) ---
        public static Color ResourceAccent(string resourceId)
        {
            switch (resourceId)
            {
                case "honey": return new Color(1f, 0.68f, 0.12f);
                case "wax": return new Color(1f, 0.82f, 0.28f);
                case "pollen": return new Color(0.78f, 0.90f, 0.32f);
                case "bees": return new Color(0.56f, 0.82f, 1f);
                case "capacity": return new Color(0.84f, 0.76f, 0.60f);
                default: return new Color(0.90f, 0.70f, 0.30f);
            }
        }

        // Valeur preview d'une ressource (id -> valeur entière).
        public static int PreviewValue(string resourceId)
        {
            switch (resourceId)
            {
                case "honey": return PreviewHoney;
                case "wax": return PreviewWax;
                case "pollen": return PreviewPollen;
                case "bees": return PreviewBees;
                case "capacity": return PreviewCapacityUsed;
                default: return 0;
            }
        }

        public static int PreviewMax(string resourceId)
        {
            return string.Equals(resourceId, "capacity", System.StringComparison.Ordinal) ? PreviewCapacityMax : 0;
        }

        // Miroir de FormatResource du monolithe (L.25456) : "0.0" + culture invariante.
        public static string FormatResource(int value)
        {
            float f = value;
            if (f >= 1000000f) return (f / 1000000f).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "M";
            if (f >= 1000f) return (f / 1000f).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "K";
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        // --- Géométrie Header (miroir DrawPortraitTopHud / DrawStrategyTopHud) ---

        // Portrait : Header (8,8,w-16,94), 3 chips de ressources sous la bande Reine.
        // Miroir exact de DrawPortraitTopHud (L.22009/22012) :
        //   chipW=(panel.width-22-gap*2)/3, panel.width=w-16,
        //   x départ = panel.x+11, gap=6, chipY=panel.y+52, chipH=36.
        public static Rect PortraitHeaderRect(float screenWidth, float screenHeight)
        {
            return new Rect(8f, 8f, screenWidth - 16f, 94f);
        }

        // Bande profil Reine en portrait : icône 36 + nom + niveau (bande supérieure).
        public static Rect PortraitQueenRect(float screenWidth, float screenHeight)
        {
            return new Rect(16f, 14f, 154f, 44f);
        }

        public static Rect[] PortraitResourceChipRects(float screenWidth, float screenHeight)
        {
            const float gap = 6f;
            Rect panel = PortraitHeaderRect(screenWidth, screenHeight);
            float chipW = (panel.width - 22f - gap * 2f) / 3f;
            float chipX = panel.x + 11f;
            float chipY = panel.y + 52f;
            Rect[] result = new Rect[3];
            for (int i = 0; i < 3; i++)
                result[i] = new Rect(chipX + i * (chipW + gap), chipY, chipW, 36f);
            return result;
        }

        // Paysage : hauteur 132 tablette / 112, 5 pils, Boutique à droite.
        public static float LandscapeHeaderHeight(float screenWidth, float screenHeight)
        {
            return (screenWidth >= 1600f && screenHeight >= 900f) ? 132f : 112f;
        }

        public static Rect LandscapeHeaderRect(float screenWidth, float screenHeight)
        {
            return new Rect(8f, 8f, screenWidth - 16f, LandscapeHeaderHeight(screenWidth, screenHeight));
        }

        public static Rect LandscapeQueenRect(float screenWidth, float screenHeight)
        {
            return new Rect(18f, 16f, 178f, 64f);
        }

        // Miroir DrawStrategyTopHud : resourceW=max(82,(w-resourceX-42-18-5*gap)/5), gap 5.
        public static Rect[] LandscapeResourceRects(float screenWidth, float screenHeight)
        {
            const float gap = 5f;
            float resourceX = 18f + 178f + 16f;
            float resourceW = Mathf.Max(82f, (screenWidth - resourceX - 42f - 18f - 5f * gap) / 5f);
            Rect[] result = new Rect[5];
            for (int i = 0; i < 5; i++)
                result[i] = new Rect(resourceX + i * (resourceW + gap), 15f, resourceW, 38f);
            return result;
        }

        public static Rect LandscapeShopRect(float screenWidth, float screenHeight)
        {
            return new Rect(screenWidth - 8f - 44f, 8f + 30f, 44f, 52f);
        }

        // Boutique en portrait : bouton compact à droite de la bande Reine, sans
        // déborder de la largeur du Header (les 3 chips de ressources restent intacts).
        public static Rect PortraitShopRect(float screenWidth, float screenHeight)
        {
            return new Rect(screenWidth - 16f - 46f, 12f, 46f, 40f);
        }

        // Panneau profil Reine (coquille : titre + niveau + progression).
        public static Rect QueenProfilePanelRect(bool portrait, float screenWidth, float screenHeight)
        {
            if (portrait) return new Rect(10f, 110f, 320f, 170f);
            return new Rect(18f, LandscapeHeaderHeight(screenWidth, screenHeight) + 18f, 320f, 170f);
        }

        // Panneau Boutique (accès uniquement, contenu différé).
        public static Rect ShopPanelRect(bool portrait, float screenWidth, float screenHeight)
        {
            if (!portrait) return new Rect(screenWidth - 310f, LandscapeHeaderHeight(screenWidth, screenHeight) + 18f, 300f, 190f);
            return new Rect(screenWidth - 310f, 110f, 300f, 190f);
        }
    }
}