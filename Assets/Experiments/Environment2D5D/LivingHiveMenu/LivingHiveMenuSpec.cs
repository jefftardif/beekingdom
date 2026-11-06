using System;
using UnityEngine;

namespace BeeKingdom.LivingHiveMenu
{
    // SPEC AUTONOME du menu inférieur LivingHive réimplanté en uGUI dans la scène
    // Environment2D5D_SpatialV3.
    //
    // Cette spec est volontairement un COPY PLAT-DATA (aucune dépendance au monolithe
    // HiveViewProductUiPresenter) : elle miroite les entrées du rail, le dictionnaire
    // d'icônes (NavIconId) et la géométrie exacte des rectangles, sans référencer le
    // fichier de 43 840 lignes. Toutes les valeurs sont celles produites par le runtime
    // LivingHive (DrawBottomRail / DrawPortraitBottomRail) pour rester fidèles.
    public sealed class LivingHiveMenuEntry
    {
        public LivingHiveMenuEntry(string id, string label, string iconKey)
        {
            ItemId = id ?? string.Empty;
            Label = label ?? string.Empty;
            IconKey = iconKey ?? string.Empty;
        }

        public string ItemId { get; }
        public string Label { get; }
        public string IconKey { get; }
    }

    public sealed class LivingHiveMenuSpec
    {
        // Nouvelle architecture à 5 boutons (identique pour portrait et paysage).
        // Ordre : CARTE, ACTIVITÉS, COMMUNICATION, SAC, PLUS.
        public static readonly LivingHiveMenuEntry[] LandscapeEntries =
        {
            new LivingHiveMenuEntry("SurfaceSwitch", "Carte", "world"),
            new LivingHiveMenuEntry("Activities", "Activites", "quests"),
            new LivingHiveMenuEntry("Communication", "Communication", "messages"),
            new LivingHiveMenuEntry("Bag", "Sac", "inventory"),
            new LivingHiveMenuEntry("More", "Plus", "more")
        };

        // Miroir identique en portrait : 5 boutons, même ordre.
        public static readonly LivingHiveMenuEntry[] PortraitEntries =
        {
            new LivingHiveMenuEntry("SurfaceSwitch", "Carte", "world"),
            new LivingHiveMenuEntry("Activities", "Activites", "quests"),
            new LivingHiveMenuEntry("Communication", "Communication", "messages"),
            new LivingHiveMenuEntry("Bag", "Sac", "inventory"),
            new LivingHiveMenuEntry("More", "Plus", "more")
        };

        // Entrées du "Plus" (MoreMenuPanel) : ordre et libellés conformes au monolithe.
        public static readonly string[] MoreMenuEntries =
        {
            "Armée",
            "Parametres",
            "Aide",
            "Support"
        };

        // Identifiants de menus ouverts par le rail (état actif).
        public const string SurfaceSwitchId = "SurfaceSwitch";
        public const string ActivitiesId = "Activities";
        public const string CommunicationId = "Communication";
        public const string BagId = "Bag";
        public const string MoreId = "More";
        public const string SettingsId = "Settings";

        public static bool IsSurfaceSwitch(string itemId)
        {
            return string.Equals(itemId, SurfaceSwitchId, StringComparison.Ordinal);
        }

        public static bool IsActivities(string itemId)
        {
            return string.Equals(itemId, ActivitiesId, StringComparison.Ordinal);
        }

        public static bool IsCommunication(string itemId)
        {
            return string.Equals(itemId, CommunicationId, StringComparison.Ordinal);
        }

        public static bool IsBag(string itemId)
        {
            return string.Equals(itemId, BagId, StringComparison.Ordinal);
        }

        public static bool IsMore(string itemId)
        {
            return string.Equals(itemId, MoreId, StringComparison.Ordinal);
        }

        public static bool IsSettings(string itemId)
        {
            return string.Equals(itemId, SettingsId, StringComparison.Ordinal);
        }

        // Miroir de NavIconId du monolithe : id logique -> clé d'icône.
        public static string NavIconId(string id)
        {
            if (string.IsNullOrEmpty(id)) return "preview";
            switch (id)
            {
                case "hive": return "hive-nav";
                case "zones": return "zones";
                case "resources": return "resources";
                case "detail": return "detail";
                case "world": return "world";
                case "quests": return "quests";
                case "inventory": return "inventory";
                case "inbox": return "inbox";
                case "alliance": return "alliance";
                case "more": return "more";
                case "queen": return "queen";
                case "messages": return "messages";
                case "preview": return "preview";
                default: return "preview";
            }
        }

        // Icone de fallback pour une entrée de rail connaissant sa clé graphique.
        public static string ResolveRailIconKey(string iconId)
        {
            return NavIconId(iconId);
        }

        // --- Géométrie ForProof : miroir exact des rects du monolithe. ---

        // Miroir de MobileBottomRailItemRectsForProof (portrait 5 entrées).
        public static Rect[] MobileBottomRailItemRectsForProof(float width,float height) => ItemRectsForSafeArea(width,height,new Rect(0,0,width,height));
        public static Rect[] LandscapeBottomRailItemRectsForProof(float width,float height) => ItemRectsForSafeArea(width,height,new Rect(0,0,width,height));
        public static Rect RailRectForProof(bool portrait,float width,float height) => RailRectForSafeArea(width,height,new Rect(0,0,width,height));
        public static Rect RailRectForSafeArea(float width,float height,Rect deviceSafe)
        {
            Rect safe=deviceSafe.width>0f&&deviceSafe.height>0f?new Rect(deviceSafe.x,height-deviceSafe.yMax,deviceSafe.width,deviceSafe.height):new Rect(0,0,width,height);
            float w=Mathf.Min(760f,Mathf.Max(1f,safe.width-16f));
            float h=Mathf.Min(118f,Mathf.Max(1f,safe.height-16f));
            return new Rect(safe.center.x-w*.5f,safe.yMax-h-8f,w,h);
        }
        public static Rect[] ItemRectsForSafeArea(float width,float height,Rect deviceSafe)
        {
            Rect rail=RailRectForSafeArea(width,height,deviceSafe);
            const int count=5; const float gap=6f;
            float w=Mathf.Max(1f,(rail.width-16f-gap*(count-1))/count);
            float h=Mathf.Min(82f,Mathf.Max(1f,rail.height-16f));
            Rect[] result=new Rect[count];
            for(int i=0;i<count;i++)result[i]=new Rect(rail.x+8f+i*(w+gap),rail.yMax-h-8f,w,h);
            return result;
        }

        // Miroir de MobileHudRectForProof (bandeau haut compact, utilisé comme butée).
        public static Rect MobileHudRectForProof(float screenWidth, float screenHeight)
        {
            return new Rect(8f, 8f, screenWidth - 16f, 94f);
        }

        public static bool IsPortrait(float screenWidth, float screenHeight)
        {
            return screenHeight > screenWidth;
        }

        public static LivingHiveMenuEntry[] Entries(bool portrait)
        {
            return portrait ? PortraitEntries : LandscapeEntries;
        }

        public static Rect[] ItemRects(bool portrait, float screenWidth, float screenHeight)
        {
            return portrait
                ? MobileBottomRailItemRectsForProof(screenWidth, screenHeight)
                : LandscapeBottomRailItemRectsForProof(screenWidth, screenHeight);
        }
    }
}