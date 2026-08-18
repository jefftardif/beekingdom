using BeeKingdom.LivingHiveMenu;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BeeKingdom.Tests.Editor.Interaction
{
    // Tests du port autonome uGUI du menu inférieur LivingHive vers Environment2D5D_SpatialV3.
    // Ils verrouillent : la spec (entrées rail, ordre, icônes, géométrie ForProof en miroir
    // du monolithe), l'état pur (navigation, surface, confort, persistance PlayerPrefs) et la
    // construction du Canvas (rail + panneaux + bascule fonctionnelle).
    public class LivingHiveMenuPortTests
    {
        // --- Spec : entrées du rail (miroir DrawBottomRail / DrawPortraitBottomRail) ---

        [Test]
        public void LandscapeRailHasExactlyTenLivingHiveEntries()
        {
            LivingHiveMenuEntry[] entries = LivingHiveMenuSpec.LandscapeEntries;
            Assert.That(entries, Is.Not.Null);
            Assert.That(entries.Length, Is.EqualTo(10));

            Assert.That(entries[0].ItemId, Is.EqualTo("SurfaceSwitch"));
            Assert.That(entries[1].ItemId, Is.EqualTo("Quests"));
            Assert.That(entries[2].ItemId, Is.EqualTo("Champions"));
            Assert.That(entries[3].ItemId, Is.EqualTo("MilestoneEvent"));
            Assert.That(entries[4].ItemId, Is.EqualTo("Bestiary"));
            Assert.That(entries[5].ItemId, Is.EqualTo("Bag"));
            Assert.That(entries[6].ItemId, Is.EqualTo("Mail"));
            Assert.That(entries[7].ItemId, Is.EqualTo("Chat"));
            Assert.That(entries[8].ItemId, Is.EqualTo("Alliance"));
            Assert.That(entries[9].ItemId, Is.EqualTo("More"));
        }

        [Test]
        public void PortraitRailHasExactlyFiveLivingHiveEntries()
        {
            LivingHiveMenuEntry[] entries = LivingHiveMenuSpec.PortraitEntries;
            Assert.That(entries, Is.Not.Null);
            Assert.That(entries.Length, Is.EqualTo(5));

            Assert.That(entries[0].ItemId, Is.EqualTo("Chat"));
            Assert.That(entries[1].ItemId, Is.EqualTo("SurfaceSwitch"));
            Assert.That(entries[2].ItemId, Is.EqualTo("Quests"));
            Assert.That(entries[3].ItemId, Is.EqualTo("Alliance"));
            Assert.That(entries[4].ItemId, Is.EqualTo("More"));
        }

        [Test]
        public void NavIconIdMatchesMonolithMapping()
        {
            Assert.That(LivingHiveMenuSpec.NavIconId("hive"), Is.EqualTo("hive-nav"));
            Assert.That(LivingHiveMenuSpec.NavIconId("world"), Is.EqualTo("world"));
            Assert.That(LivingHiveMenuSpec.NavIconId("quests"), Is.EqualTo("quests"));
            Assert.That(LivingHiveMenuSpec.NavIconId("inventory"), Is.EqualTo("inventory"));
            Assert.That(LivingHiveMenuSpec.NavIconId("inbox"), Is.EqualTo("inbox"));
            Assert.That(LivingHiveMenuSpec.NavIconId("alliance"), Is.EqualTo("alliance"));
            Assert.That(LivingHiveMenuSpec.NavIconId("more"), Is.EqualTo("more"));
            Assert.That(LivingHiveMenuSpec.NavIconId("queen"), Is.EqualTo("queen"));
            Assert.That(LivingHiveMenuSpec.NavIconId("messages"), Is.EqualTo("messages"));
            Assert.That(LivingHiveMenuSpec.NavIconId("unknown"), Is.EqualTo("preview"));
        }

        // --- Spec : géométrie ForProof (miroir exact des rects du monolithe) ---

        [Test]
        public void PortraitRailRectsMatchMonolithExactGeometry()
        {
            const float width = 390f;
            const float height = 844f;
            Rect[] rects = LivingHiveMenuSpec.MobileBottomRailItemRectsForProof(width, height);

            Assert.That(rects.Length, Is.EqualTo(5));
            Assert.That(rects[0].y, Is.EqualTo(height - 78f + 8f).Within(0.001f));
            Assert.That(rects[0].height, Is.EqualTo(70f - 16f).Within(0.001f));
            Assert.That(rects[1].x - rects[0].xMax, Is.EqualTo(8f).Within(0.001f));
            Assert.That(rects[4].xMax, Is.EqualTo(8f + (width - 16f) - 10f).Within(0.001f));
        }

        [Test]
        public void PortraitRailOneValidColumnLayout()
        {
            const float width = 390f;
            const float height = 844f;
            Rect[] portrait = LivingHiveMenuSpec.MobileBottomRailItemRectsForProof(width, height);
            Rect[] landscape = LivingHiveMenuSpec.LandscapeBottomRailItemRectsForProof(width, height);

            Assert.That(portrait.Length, Is.EqualTo(5));
            Assert.That(landscape.Length, Is.EqualTo(10));
            Assert.That(portrait[0].y, Is.EqualTo(height - 78f + 8f).Within(0.001f));
            Assert.That(landscape[0].y, Is.EqualTo(height - 76f + 7f).Within(0.001f));
        }

        // --- État : navigation ---

        [Test]
        public void ToggleEntryOpensAndClosesMenu()
        {
            var state = new LivingHiveMenuState();
            state.UsePersistentStorage = false;

            Assert.That(state.ActiveMenuId, Is.Empty);
            state.ToggleEntry("Quests");
            Assert.That(state.ActiveMenuId, Is.EqualTo("Quests"));
            Assert.That(state.IsMenuOpen("Quests"), Is.True);

            state.ToggleEntry("Quests");
            Assert.That(state.ActiveMenuId, Is.Empty);
        }

        [Test]
        public void SurfaceSwitchTogglesWorldAndClosesMenu()
        {
            var state = new LivingHiveMenuState();
            state.UsePersistentStorage = false;

            state.ToggleEntry("Quests");
            state.ToggleEntry("SurfaceSwitch");

            Assert.That(state.SurfaceMode, Is.EqualTo(LivingHiveMenuState.SurfaceBoundary.World));
            Assert.That(state.SurfaceSwitchLabelForProof, Is.EqualTo("Ruche"));
            Assert.That(state.ActiveMenuId, Is.Empty);
        }

        [Test]
        public void ChatToggleOpensChatAndClosesMenu()
        {
            var state = new LivingHiveMenuState();
            state.UsePersistentStorage = false;

            state.ToggleEntry("Quests");
            state.ToggleEntry("Chat");

            Assert.That(state.ChatOpen, Is.True);
            Assert.That(state.ActiveMenuId, Is.Empty);
        }

        [Test]
        public void MoreShowsActiveWhenSettingsOpen()
        {
            var state = new LivingHiveMenuState();
            state.UsePersistentStorage = false;

            state.ToggleEntry("More");
            Assert.That(state.IsMenuOpen("More"), Is.True);
            Assert.That(state.IsMoreActiveForProof(), Is.False);

            state.OpenSettings();
            Assert.That(state.IsMenuOpen("Settings"), Is.True);
            Assert.That(state.IsMoreActiveForProof(), Is.True,
                "Le bouton More doit rester actif quand Paramètres est ouvert.");
        }

        [Test]
        public void MoreClickWhenSettingsOpenClosesPanel()
        {
            var state = new LivingHiveMenuState();
            state.UsePersistentStorage = false;

            state.OpenSettings();
            state.ToggleEntry("More");
            Assert.That(state.ActiveMenuId, Is.Empty);
        }

        // --- État : confort mobile + persistance ---

        [Test]
        public void SettingsRoundTripPersistsThroughPlayerPrefs()
        {
            LivingHiveMenuState.ClearPersistedPrefs();
            try
            {
                var first = new LivingHiveMenuState();
                first.UsePersistentStorage = true;
                first.LoadFromPlayerPrefs();
                Assert.That(first.SoundEnabled, Is.True, "Valeur par défaut son = ON.");

                first.SetSoundEnabled(false);
                first.SetReducedMotion(true);
                first.SetPreferredLocale("en-US");
                Assert.That(first.IsCustomSettingsForProof(), Is.True);

                var second = new LivingHiveMenuState();
                second.UsePersistentStorage = true;
                second.LoadFromPlayerPrefs();

                Assert.That(second.SoundEnabled, Is.False, "Son désactivé persiste.");
                Assert.That(second.ReducedMotionEnabled, Is.True, "Mouvement réduit persiste.");
                Assert.That(second.IsFrenchForProof(), Is.False, "Locale en-US persiste.");
            }
            finally
            {
                LivingHiveMenuState.ClearPersistedPrefs();
            }
        }

        [Test]
        public void DefaultsAreAllStock()
        {
            var state = new LivingHiveMenuState();
            state.UsePersistentStorage = false;

            Assert.That(state.SurfaceMode, Is.EqualTo(LivingHiveMenuState.SurfaceBoundary.Hive));
            Assert.That(state.ChatOpen, Is.False);
            Assert.That(state.SoundEnabled, Is.True);
            Assert.That(state.MusicEnabled, Is.True);
            Assert.That(state.ReducedMotionEnabled, Is.False);
            Assert.That(state.EconomyModeEnabled, Is.False);
            Assert.That(state.IsFrenchForProof(), Is.True);
            Assert.That(state.IsCustomSettingsForProof(), Is.False);
        }

        // --- Canvas uGUI : construction + bascule fonctionnelle ---

        [Test]
        public void CanvasBuildCreatesRailAndOverlayPanels()
        {
            var root = new GameObject("MenuTest");
            try
            {
                var canvas = root.AddComponent<LivingHiveMenuCanvas>();
                canvas.Build();

                Assert.That(canvas.IsRailBuilt, Is.True);
                Assert.That(canvas.RailButtonCount, Is.EqualTo(
                    LivingHiveMenuSpec.IsPortrait(Screen.width, Screen.height) ? 5 : 10));

                Assert.That(canvas.PanelShown("Quests"), Is.False);
                Assert.That(canvas.PanelShown("Carte"), Is.False, "Overlay Carte masqué par défaut.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ClickingQuestsOpensQuestPanel()
        {
            var root = new GameObject("MenuTest");
            try
            {
                var canvas = root.AddComponent<LivingHiveMenuCanvas>();
                canvas.Build();

                Assert.That(canvas.PanelShown("Quests"), Is.False);
                canvas.SimulateEntryClick("Quests");
                Assert.That(canvas.PanelShown("Quests"), Is.True);
                Assert.That(canvas.ActiveRailItemForProof, Is.EqualTo("Quests"));

                canvas.SimulateEntryClick("Quests");
                Assert.That(canvas.PanelShown("Quests"), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SurfaceSwitchNoLongerOpensLocalOverlay()
        {
            // CARTE now performs a real scene switch to the world map (see
            // LivingHiveMenuCanvas.OpenWorldMap), not the old local "Carte" overlay/state
            // toggle this test used to verify. There is no Play context in EditMode to load
            // a scene into, so OpenWorldMap no-ops here; this asserts the click no longer
            // falls back to the local overlay either.
            var root = new GameObject("MenuTest");
            try
            {
                var canvas = root.AddComponent<LivingHiveMenuCanvas>();
                canvas.Build();

                Assert.That(canvas.PanelShown("Carte"), Is.False);
                canvas.SimulateEntryClick("SurfaceSwitch");
                Assert.That(canvas.PanelShown("Carte"), Is.False);
                Assert.That(canvas.State.SurfaceMode, Is.EqualTo(LivingHiveMenuState.SurfaceBoundary.Hive));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MoreRowSettingsOpensSettingsPanel()
        {
            var root = new GameObject("MenuTest");
            try
            {
                var canvas = root.AddComponent<LivingHiveMenuCanvas>();
                canvas.Build();

                canvas.SimulateEntryClick("More");
                Assert.That(canvas.PanelShown("More"), Is.True);

                canvas.SimulateMoreRowClick("Parametres");
                Assert.That(canvas.PanelShown("Settings"), Is.True);
                Assert.That(canvas.PanelShown("More"), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // --- Canvas corrigé : rail pleine largeur, en bas d'écran, sans scaling ---

        [Test]
        public void RailForProofMatchesMonolithFullWidthBottomBar()
        {
            Rect portrait = LivingHiveMenuSpec.RailRectForProof(true, 390f, 844f);
            Rect landscape = LivingHiveMenuSpec.RailRectForProof(false, 844f, 390f);

            Assert.That(portrait.x, Is.EqualTo(8f));
            Assert.That(portrait.width, Is.EqualTo(390f - 16f));
            Assert.That(portrait.height, Is.EqualTo(70f));
            Assert.That(portrait.yMax, Is.EqualTo(844f - 8f));

            Assert.That(landscape.x, Is.EqualTo(8f));
            Assert.That(landscape.width, Is.EqualTo(844f - 16f));
            Assert.That(landscape.height, Is.EqualTo(68f));
            Assert.That(landscape.yMax, Is.EqualTo(390f - 8f));
        }

        [Test]
        public void RailBackdropSitsFullWidthAtBottomOfCanvas()
        {
            var root = new GameObject("MenuTest");
            try
            {
                var canvas = root.AddComponent<LivingHiveMenuCanvas>();
                canvas.Build();

                Transform backdrop = null;
                foreach (RectTransform child in root.GetComponentsInChildren<RectTransform>(true))
                {
                    if (child.name == "RailBackdrop") { backdrop = child; break; }
                }
                Assert.That(backdrop, Is.Not.Null, "RailBackdrop doit exister.");

                RectTransform b = backdrop.GetComponent<RectTransform>();
                Assert.That(b.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(b.anchorMax, Is.EqualTo(Vector2.zero));
                Assert.That(b.pivot, Is.EqualTo(Vector2.zero));
                Assert.That(b.sizeDelta.x, Is.EqualTo(Screen.width - 16f).Within(0.001f));
                Assert.That(b.sizeDelta.y, Is.InRange(60f, 74f), "Hauteur du rail = 68/70 selon orientation.");

                bool portrait = LivingHiveMenuSpec.IsPortrait(Screen.width, Screen.height);
                float expectedBottom = Screen.height - (LivingHiveMenuSpec.RailRectForProof(portrait, Screen.width, Screen.height)).yMax;
                Assert.That(b.anchoredPosition.y, Is.EqualTo(expectedBottom).Within(0.001f));
                Assert.That(expectedBottom, Is.EqualTo(8f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RailHasNoCanvasScaler()
        {
            var root = new GameObject("MenuTest");
            try
            {
                var canvas = root.AddComponent<LivingHiveMenuCanvas>();
                canvas.Build();
                Assert.That(root.GetComponentInChildren<CanvasScaler>(), Is.Null,
                    "Le Canvas du port ne doit pas porter de CanvasScaler (géométrie pixel écran du monolithe).");
                Assert.That(root.GetComponentInChildren<Canvas>(), Is.Not.Null);
                Assert.That(root.GetComponentInChildren<Canvas>().renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}