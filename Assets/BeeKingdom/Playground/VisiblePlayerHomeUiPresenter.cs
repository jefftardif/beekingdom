using BeeKingdom.Colony;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public static class VisiblePlayerHomeUiPresenter
    {
        private static GUIStyle titleStyle;
        private static GUIStyle headerStyle;
        private static GUIStyle chipStyle;
        private static GUIStyle bodyStyle;
        private static GUIStyle mutedStyle;

        public static VisiblePlayerHomeUiBootstrap BootstrapContract => new VisiblePlayerHomeUiBootstrap(
            "SandboxPlayground",
            "Sandbox Playground",
            new[]
            {
                new VisibleHomeUiElement("BeeKingdomTitle", "Identite du jeu", "Bee Kingdom", true),
                new VisibleHomeUiElement("HiveNamePreview", "Identite de ruche", "Ruche Prime", true),
                new VisibleHomeUiElement("MainResource", "Ressource principale", "Miel", true),
                new VisibleHomeUiElement("HiveEntry", "Acces ruche", "Ruche", true),
                new VisibleHomeUiElement("WorldEntry", "Acces monde", "Monde", true),
                new VisibleHomeUiElement("PreviewStatusMessage", "Statut local", "Preview locale", true),
                new VisibleHomeUiElement("BackCloseAffordance", "Retour non technique", "Retour accueil", true)
            });

        public static ResourceAndProgressionReadout ResourceReadout => new ResourceAndProgressionReadout(
            new[]
            {
                new VisibleResourceValue("Honey", "1 240", "Preview locale"),
                new VisibleResourceValue("Wax", "420", "Preview locale"),
                new VisibleResourceValue("Pollen", "315", "Preview locale"),
                new VisibleResourceValue("Population", "86 abeilles", "Preview locale")
            },
            new VisibleProgressionValue("Progression de ruche", "Niveau 3 - apercu", "Lisibilite joueur seulement"),
            "Valeurs de preview locale, non officielles et non serveur.");

        public static MobileHomeHudShell HudShell => new MobileHomeHudShell(
            new HomeHudHeader("Bee Kingdom", "Ruche Prime", "Preview locale - aucun serveur connecte"),
            new[]
            {
                new HomeHudResourceChip("Honey", "Miel", "1 240", "honey-chip"),
                new HomeHudResourceChip("Wax", "Cire", "420", "wax-chip"),
                new HomeHudResourceChip("Pollen", "Pollen", "315", "pollen-chip")
            },
            new[]
            {
                new HomeHudNavEntry("Hive", "Ruche", "nav-hive", string.Empty),
                new HomeHudNavEntry("World", "Monde", "nav-world", "Carte live non connectee"),
                new HomeHudNavEntry("Alliance", "Alliance", "nav-alliance", "Social live non connecte"),
                new HomeHudNavEntry("Messages", "Messages", "nav-messages", "Messagerie preview seulement"),
                new HomeHudNavEntry("Army", "Armee", "nav-army", "Combat verrouille"),
                new HomeHudNavEntry("Research", "Recherche", "nav-research", "Bonus officiels non actifs")
            },
            new HomeHudPreviewStatus("Accueil joueur visible en Play Mode", true));

        public static HiveActionPreviewPanel ActionPreview => new HiveActionPreviewPanel(
            "Ameliorer la salle de stockage",
            "Augmente la capacite visible de miel et de cire dans la ruche.",
            new[]
            {
                new ActionPreviewRequirement("Wax", "Cire requise: 500", false),
                new ActionPreviewRequirement("Workers", "Ouvrieres disponibles: preview", true),
                new ActionPreviewRequirement("Server", "Validation serveur future requise", false)
            },
            "Action en preview: aucune depense, aucun ordre officiel.");

        public static ArmyDefenseAccessPreview ArmyPreview => new ArmyDefenseAccessPreview(
            "Armee et defense",
            "Gardes en observation autour de l'entree.",
            "Combat et pertes verrouilles jusqu'a autorite serveur.",
            new[]
            {
                new ArmyPreviewSignal("Guards", "Gardes: posture defensive", false),
                new ArmyPreviewSignal("HiveDefense", "Defense de ruche: calme", false),
                new ArmyPreviewSignal("FutureTraining", "Entrainement futur: non actif", false),
                new ArmyPreviewSignal("CombatLocked", "Combat verrouille", true)
            });

        public static AllianceSocialPreviewNotebook SocialPreview => new AllianceSocialPreviewNotebook(
            "Carnet social",
            "Aucune alliance connectee",
            new[]
            {
                new SocialNotebookRow("Invite", "Invitation alliance future", "Preview seulement"),
                new SocialNotebookRow("Help", "Demande d'aide locale", "Non envoyee"),
                new SocialNotebookRow("SystemMessage", "Message systeme apercu", "Non persistant"),
                new SocialNotebookRow("Etiquette", "Moderation et respect", "Information locale"),
                new SocialNotebookRow("Trust", "Confiance sociale", "Non connectee")
            },
            "Rester clair, utile et respectueux. Aucun chat live n'est actif.");

        public static void Draw(float fps, bool compact)
        {
            EnsureStyles();
            float width = Mathf.Min(Screen.width - 24f, compact ? 430f : 520f);
            Rect panel = new Rect(Screen.width - width - 12f, 12f, width, Mathf.Min(Screen.height - 24f, 740f));
            GUI.Box(panel, string.Empty);
            GUILayout.BeginArea(new Rect(panel.x + 14f, panel.y + 12f, panel.width - 28f, panel.height - 24f));
            DrawHeader(fps);
            DrawResources();
            DrawNavigation();
            DrawAction();
            DrawDefenseAndSocial();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Preview locale - rien n'est sauvegarde, envoye ou declare officiel.", mutedStyle);
            if (GUILayout.Button("Retour accueil", GUILayout.Height(34f)))
            {
                // Intentionally local: the preview button only gives a visible affordance in Play Mode.
            }
            GUILayout.EndArea();
        }

        private static void DrawHeader(float fps)
        {
            GUILayout.Label(HudShell.Header.Title, titleStyle);
            GUILayout.Label(HudShell.Header.HiveName + "  |  " + HudShell.Header.StatusText, headerStyle);
            GUILayout.Label("Play Mode visible  |  FPS " + fps.ToString("0"), mutedStyle);
            GUILayout.Space(8f);
        }

        private static void DrawResources()
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < HudShell.ResourceChips.Count; i++)
            {
                HomeHudResourceChip chip = HudShell.ResourceChips[i];
                GUILayout.Label(chip.Label + "\n" + chip.DisplayValue, chipStyle, GUILayout.Height(48f));
            }
            GUILayout.EndHorizontal();
            GUILayout.Label(ResourceReadout.Progression.Label + ": " + ResourceReadout.Progression.DisplayValue, bodyStyle);
            GUILayout.Space(8f);
        }

        private static void DrawNavigation()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Acces principaux", headerStyle);
            GUILayout.BeginHorizontal();
            for (int i = 0; i < HudShell.NavigationEntries.Count; i++)
            {
                HomeHudNavEntry entry = HudShell.NavigationEntries[i];
                GUILayout.Button(entry.Label, GUILayout.Height(34f));
                if (i == 2)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Monde, alliance, messages, armee et recherche restent en preview locale.", mutedStyle);
            GUILayout.EndVertical();
            GUILayout.Space(8f);
        }

        private static void DrawAction()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(ActionPreview.ActionTitle, headerStyle);
            GUILayout.Label(ActionPreview.PreviewBenefit, bodyStyle);
            for (int i = 0; i < ActionPreview.Requirements.Count; i++)
            {
                ActionPreviewRequirement requirement = ActionPreview.Requirements[i];
                GUILayout.Label((requirement.Satisfied ? "OK  " : "--  ") + requirement.DisplayText, bodyStyle);
            }
            GUILayout.Label(ActionPreview.LockedActionExplanation, mutedStyle);
            GUILayout.EndVertical();
            GUILayout.Space(8f);
        }

        private static void DrawDefenseAndSocial()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(ArmyPreview.EntryLabel, headerStyle);
            GUILayout.Label(ArmyPreview.DefenseStatusText, bodyStyle);
            GUILayout.Label(ArmyPreview.LockedCombatExplanation, mutedStyle);
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(SocialPreview.NotebookTitle, headerStyle);
            GUILayout.Label(SocialPreview.TrustStatusText, bodyStyle);
            GUILayout.Label("Aide locale et messages: preview.", mutedStyle);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private static void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.84f, 0.22f) }, wordWrap = true };
            headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, normal = { textColor = Color.white }, wordWrap = true };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, normal = { textColor = Color.white }, wordWrap = true };
            mutedStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = new Color(0.82f, 0.86f, 0.9f) }, wordWrap = true };
            chipStyle = new GUIStyle(GUI.skin.box) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white }, wordWrap = true };
        }
    }
}
