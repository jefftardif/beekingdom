using System;
using UnityEngine;

namespace BeeKingdom.LivingHiveMenu
{
    // SPÉCIFICATION DES DONNÉES ET GÉOMÉTRIE de la fenêtre Recherche plein écran
    // (Local Preview) pour la scène Environment2D5D_SpatialV3.
    //
    // Miroir pur-C# de la fenêtre IMGUI du monolithe (HiveViewProductUiPresenter) :
    //   - les 4 filtres plein écran et leur logique d'appariement = miroir exact de
    //     ResearchMatchesFullscreenFilter (L.31791) — aucune réimplémentation arbitraire ;
    //   - la géométrie (bannière, rail de filtres, colonnes de cartes) = miroir exact de
    //     DrawResearchFullscreen (L.33317) ;
    //   - les couleurs d'accent par état de carte = miroir de DrawResearchFullscreenCard
    //     (L.33420).
    public static class LivingHiveResearchSpec
    {
        public const string FilterAll = "all";
        public const string FilterForage = "forage";
        public const string FilterResources = "resources";
        public const string FilterDefense = "defense";

        public static readonly string[] Filters = { FilterAll, FilterForage, FilterResources, FilterDefense };

        public static readonly string[] FilterLabels = { "Toutes les études", "Forage", "Ressources", "Défense" };

        // --- Bannière (miroir DrawResearchFullscreen L.33353) ---
        public const string BannerTitle = "RECHERCHE";
        public const string BannerSubtitle = "Études persistantes de la colonie";

        // --- Couleurs de carte (miroir DrawResearchFullscreenCard L.33430) ---
        public static readonly Color CardNormalFill = new Color(0.035f, 0.048f, 0.062f, 0.97f);
        public static readonly Color CardNormalAccent = new Color(0.38f, 0.72f, 1f, 0.82f);
        public static readonly Color CardRunningAccent = new Color(0.70f, 0.62f, 1f, 0.94f);
        public static readonly Color CardCompletedAccent = new Color(0.42f, 0.82f, 0.48f, 0.92f);

        // --- Voile de la fenêtre (miroir L.33349) et séparateur (L.33364) ---
        public static readonly Color VeilColor = new Color(0.006f, 0.005f, 0.004f, 0.99f);
        public static readonly Color SeparatorColor = new Color(1f, 0.60f, 0.14f, 0.95f);

        // --- Préviews économiques locales (miroir des statiques monolithe L.615/619,
        //     mêmes valeurs que LivingHiveMenuHeaderData) ---
        public const float PreviewHoney = 125800f;
        public const float PreviewPollen = 98450f;

        // --- Logique du filtre (miroir EXACT de ResearchMatchesFullscreenFilter L.31791) ---
        public static bool MatchesFilter(string researchId, string filter)
        {
            if (filter == FilterAll) return true;
            string id = researchId ?? string.Empty;
            if (filter == FilterForage) return id.IndexOf("foraging", StringComparison.OrdinalIgnoreCase) >= 0;
            if (filter == FilterResources)
            {
                return id.IndexOf("pollen", StringComparison.OrdinalIgnoreCase) >= 0
                    || id.IndexOf("tempered", StringComparison.OrdinalIgnoreCase) >= 0
                    || id.IndexOf("sealed", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            if (filter == FilterDefense)
            {
                return id.IndexOf("defense", StringComparison.OrdinalIgnoreCase) >= 0
                    || id.IndexOf("reserve", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            return true;
        }

        // --- Géométrie plein écran (miroir DrawResearchFullscreen) ---

        public static float BannerHeight(bool portrait, float screenHeight)
        {
            return portrait ? Mathf.Min(150f, screenHeight * 0.20f)
                : Mathf.Clamp(screenHeight * 0.20f, 150f, 190f);
        }

        public static Rect BannerRect(bool portrait, float screenWidth, float screenHeight)
        {
            return new Rect(0f, 0f, screenWidth, BannerHeight(portrait, screenHeight));
        }

        public static int ColumnCount(bool portrait)
        {
            return portrait ? 1 : 2;
        }

        public static float CardGap(bool portrait)
        {
            return portrait ? 8f : 12f;
        }

        public static float CardHeight(bool portrait)
        {
            return portrait ? 132f : 164f;
        }

        public static float FilterItemWidth(bool portrait, float railWidth)
        {
            const float gap = 6f;
            return portrait ? 132f : (railWidth - 24f - gap * 3f) / 4f;
        }

        // Rail des filtres (miroir L.33373-33374) : y = bannerHeight + 6, hauteur 54/58.
        public static Rect FilterRailRect(bool portrait, float screenWidth, float screenHeight)
        {
            float y = BannerHeight(portrait, screenHeight) + 6f;
            return new Rect(10f, y, screenWidth - 20f, portrait ? 54f : 58f);
        }

        // Zone de contenu des cartes (miroir L.33396-33398).
        public static Rect ContentRect(bool portrait, float screenWidth, float screenHeight)
        {
            Rect rail = FilterRailRect(portrait, screenWidth, screenHeight);
            float top = rail.yMax + 8f;
            return new Rect(10f, top, screenWidth - 20f, screenHeight - top - 10f);
        }

        public static float ViewportInset(bool portrait)
        {
            return portrait ? 10f : 16f;
        }

        // Rect (coin haut-gauche) d'une carte dans l'espace de scroll du contenu
        // (miroir L.33402-33414) : colonnes, gap, hauteur fixe.
        public static Rect CardRectInside(bool portrait, int index, float viewportWidth)
        {
            int columns = ColumnCount(portrait);
            float gap = CardGap(portrait);
            float cardWidth = (viewportWidth - gap * (columns - 1)) / columns;
            int col = index % columns;
            int row = index / columns;
            return new Rect(col * (cardWidth + gap), row * (CardHeight(portrait) + gap), cardWidth, CardHeight(portrait));
        }

        // Boutons retour/fermer (miroir DrawResearchFullscreen L.33326) : retour haut-gauche,
        // fermer haut-droit.
        public static Rect BackButtonRect()
        {
            return new Rect(10f, 8f, 42f, 34f);
        }

        public static Rect CloseButtonRect(float screenWidth)
        {
            return new Rect(screenWidth - 54f, 6f, 46f, 38f);
        }
    }
}