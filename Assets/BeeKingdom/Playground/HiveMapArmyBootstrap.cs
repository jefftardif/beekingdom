using System;
using System.Globalization;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.Core.Integration;
using BeeKingdom.Localization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    public sealed class HiveMapArmyBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Army Runtime";
        private const float HeaderHeight = 116f;
        private const float ContentMaxWidth = 720f;
        private const float EntryButtonWidth = 110f;
        private const float EntryButtonHeight = 36f;

        public static bool ModalOpenForExternalHost { get; private set; }

        private Vector2 scroll;
        private bool entryButtonWasPressed;
        private int selGuardians;
        private int selWingrunners;
        private int selDarters;
        private string lastSeenReservationId = "";
        private bool lastSeenHadReservation;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            if (FindFirstObjectByType<HiveMapArmyBootstrap>() != null) return;
            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, active);
            root.AddComponent<HiveMapArmyBootstrap>();
        }

        private static bool IsEnvironmentScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return false;
            return scene.name.StartsWith("Environment2D5D", StringComparison.Ordinal);
        }

        public static void InitializeForScene(Scene scene)
        {
            if (!Application.isPlaying) return;
            if (!IsEnvironmentScene(scene)) return;
            if (FindFirstObjectByType<HiveMapArmyBootstrap>() != null) return;
            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<HiveMapArmyBootstrap>();
        }

        private void Start()
        {
            LivingHiveArmyBridge.SetHandlers(() => ModalOpenForExternalHost, OpenModal);
            // M038C-CL: fallback FTUE target for whenever the Caserne's own Army button
            // (registered as a screen-rect, takes priority - see HiveViewProductUiPresenter's
            // DrawBarrackTopBar) isn't currently on screen, e.g. the player is looking at the
            // "Plus" submenu instead. Lazy Func: re-resolves the real RectTransform on every
            // lookup, so it stays correct across the submenu opening/closing.
            try { BeeKingdom.Tutorial.TutorialTargetRegistry.Instance.RegisterUi(BeeKingdom.Tutorial.FtueTutorialRegistry.TargetArmyMenu, LivingHiveArmyBridge.GetArmyRowRect); } catch {}
        }

        private void OnGUI()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;

            if (ModalOpenForExternalHost)
            {
                // M040X-CL: same fix as HiveMapProductionBootstrap/HiveMapBarrackBootstrap -
                // see Docs/AI/Missions/M040X-CL-FTUE-Overlay-Occlusion-Fix.md.
                bool clipToDialogue = BeeKingdom.Tutorial.TutorialDialoguePresenter.IsAnyDialogueVisible;
                if (clipToDialogue)
                {
                    Rect panelRect = BeeKingdom.Tutorial.TutorialDialoguePresenter.GetCurrentPanelRect();
                    GUI.BeginGroup(new Rect(0f, 0f, Screen.width, panelRect.yMin));
                }
                DrawFullscreen();
                if (clipToDialogue) GUI.EndGroup();
                return;
            }

            DrawEntryButton();
        }

        private void DrawEntryButton()
        {
            // Floating entry removed in M018 — Army now via bottom rail (LivingHiveArmyBridge).
            return;
        }

        private static void OpenModal()
        {
            ModalOpenForExternalHost = true;
            MobileAccountSessionRuntimeBootstrap.TryConfigureGameplayForActiveSession();
            RefreshAllControllers();
            try { BeeKingdom.Tutorial.TutorialGameplayNotifier.NotifyWindowOpened("army"); } catch {}
        }

        private void SyncSelectionFromModel()
        {
            var m = MobileAccountSessionRuntimeBootstrap.SquadReservationControllerForHiveMap?.Model;
            bool hasReservation = m != null && m.HasReservation;
            string curId = m?.ReservationId ?? "";

            if (hasReservation)
            {
                selGuardians = (int)m.ReservedGuardians;
                selWingrunners = (int)m.ReservedWingrunners;
                selDarters = (int)m.ReservedDarters;
            }
            else if (lastSeenHadReservation && !hasReservation)
            {
                // Authoritative release just completed — clear editable selection
                selGuardians = 0;
                selWingrunners = 0;
                selDarters = 0;
            }
            else if (m != null)
            {
                // No reservation and no release transition — preserve editing selection
                // (do not clear 4/0/0 while player is composing)
            }

            lastSeenHadReservation = hasReservation;
            lastSeenReservationId = curId;
        }

        private static void RefreshAllControllers()
        {
            try { MobileAccountSessionRuntimeBootstrap.DoctrineRecruitmentControllerForHiveMap?.Refresh(); } catch { }
            try { MobileAccountSessionRuntimeBootstrap.SquadReservationControllerForHiveMap?.Refresh(); } catch { }
            try { MobileAccountSessionRuntimeBootstrap.PerimeterSortieControllerForHiveMap?.Refresh(); } catch { }
            try { MobileAccountSessionRuntimeBootstrap.CombatPatrolControllerForHiveMap?.Refresh(); } catch { }
        }

        private void DrawFullscreen()
        {
            DrawFullscreenBackground();
            DrawHeader();
            if (!ModalOpenForExternalHost) return;

            float contentWidth = Mathf.Min(ContentMaxWidth, Screen.width - 28f);
            Rect content = new Rect((Screen.width - contentWidth) * 0.5f, HeaderHeight + 14f, contentWidth, Screen.height - HeaderHeight - 28f);
            GUILayout.BeginArea(content);
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Width(content.width), GUILayout.Height(content.height));

            DrawTroopsSection();
            GUILayout.Space(10f);
            DrawSquadSection();
            GUILayout.Space(10f);
            DrawDoctrineSection();
            GUILayout.Space(10f);
            DrawSortieSection();
            GUILayout.Space(10f);
            DrawPatrolSection();

            GUILayout.Space(12f);
            GUILayout.Label(Text("L'Armée prépare les opérations depuis la Ruche. Les combats se déroulent sur la Carte du Monde.", "The Army prepares operations from the Hive. Combat occurs on the World Map."), new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 11 });
            if (GUILayout.Button(Text("Ouvrir la Carte du Monde", "Open World Map"), GUILayout.Height(38f)))
            {
                if (SplashDevelopmentSceneConfig.IsSceneEnabledInBuildSettings(SplashDevelopmentSceneConfig.WorldMapScenePath))
                    SplashDevelopmentSceneConfig.TryOpenScene(SplashDevelopmentSceneConfig.WorldMapScenePath, out _);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawFullscreenBackground()
        {
            Rect full = new Rect(0f, 0f, Screen.width, Screen.height);
            Color prev = GUI.color;
            GUI.color = new Color(0.006f, 0.005f, 0.004f, 0.99f);
            GUI.DrawTexture(full, Texture2D.blackTexture, ScaleMode.StretchToFill, false);
            GUI.color = prev;
        }

        private void DrawHeader()
        {
            Rect banner = new Rect(0f, 0f, Screen.width, HeaderHeight);
            Color prev = GUI.color;
            GUI.color = new Color(0.13f, 0.085f, 0.024f, 0.98f);
            GUI.DrawTexture(banner, Texture2D.whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = new Color(0f, 0f, 0f, 0.38f);
            GUI.DrawTexture(banner, Texture2D.blackTexture, ScaleMode.StretchToFill, false);
            GUI.color = prev;

            if (HiveViewProductUiPresenter.DrawPremiumBackButtonForExternalHost(new Rect(4f, 2f, 48f, 46f)))
            {
                CloseArmyModal();
                return;
            }
            if (GUI.Button(new Rect(Screen.width - 48f, 2f, 48f, 46f), "×", new GUIStyle(GUI.skin.button) { fontSize = 22 })) CloseArmyModal();

            GUI.Label(new Rect(68f, 12f, Screen.width - 220f, 30f), BeeLocalization.Text("ui.army.fullscreen_title", Text("ARMÉE", "ARMY")), new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold });
            GUI.Label(new Rect(70f, 42f, Screen.width - 220f, 22f), Text("Gestion et préparation des forces", "Force management and preparation"), new GUIStyle(GUI.skin.label) { fontSize = 13 });
            if (GUI.Button(new Rect(Screen.width - 112f, 14f, 96f, 34f), Text("Rafraîchir", "Refresh"))) RefreshAllControllers();
            GUI.color = new Color(1f, 0.60f, 0.14f, 0.95f);
            GUI.DrawTexture(new Rect(0f, HeaderHeight - 1f, Screen.width, 1f), Texture2D.whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = Color.white;
        }

        private void CloseArmyModal()
        {
            bool hasReservation = MobileAccountSessionRuntimeBootstrap.SquadReservationControllerForHiveMap?.Model?.HasReservation ?? false;
            ModalOpenForExternalHost = false;
            if (!hasReservation)
            {
                selGuardians = 0;
                selWingrunners = 0;
                selDarters = 0;
            }
        }

        private void DrawTroopsSection()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(Text("FORCES", "FORCES"), new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 });
            var ctrl = MobileAccountSessionRuntimeBootstrap.SquadReservationControllerForHiveMap;
            if (ctrl == null || !ctrl.IsConfigured)
            {
                GUILayout.Label(Text("Forces non configurées — serveur requis.", "Forces not configured — server required."), new GUIStyle(GUI.skin.label) { wordWrap = true });
            }
            else if (ctrl.IsBusy && ctrl.Model.State == HiveSquadReservationScreenState.Loading)
            {
                GUILayout.Label(Text("Synchronisation…", "Syncing…"), new GUIStyle(GUI.skin.label) { wordWrap = true });
            }
            else
            {
                var m = ctrl.Model;
                GUILayout.Label(Text("Roster réel — une seule vérité (Caserne → Armée)", "Real roster — single truth (Barrack → Army)"), new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 10 });
                DrawTroopRow("Gardiennes", "Guardians", m.RosterGuardians, m.AvailableGuardians, m.ReservedGuardians);
                DrawTroopRow("Voltigeuses", "Wingrunners", m.RosterWingrunners, m.AvailableWingrunners, m.ReservedWingrunners);
                DrawTroopRow("Lanceuses", "Darters", m.RosterDarters, m.AvailableDarters, m.ReservedDarters);
                GUILayout.Label(Text("Capacité d'escouade: ", "Squad capacity: ") + m.Capacity.ToString(CultureInfo.InvariantCulture) + Text(" (X / 16)", " (X / 16)").Replace("X", (m.ReservedGuardians + m.ReservedWingrunners + m.ReservedDarters).ToString(CultureInfo.InvariantCulture)), new GUIStyle(GUI.skin.label) { wordWrap = true });
                if (!string.IsNullOrWhiteSpace(m.ErrorCode) && m.State == HiveSquadReservationScreenState.Error)
                    GUILayout.Label(Text("Erreur: ", "Error: ") + TranslateError(m.ErrorCode), new GUIStyle(GUI.skin.label) { wordWrap = true });
            }
            GUILayout.EndVertical();
        }

        private void DrawTroopRow(string fr, string en, long total, long available, long assigned)
        {
            string name = Text(fr, en);
            GUILayout.Label(name + Text(" — Total: ", " — Total: ") + total.ToString(CultureInfo.InvariantCulture) + Text(" | Dispo: ", " | Avail: ") + available.ToString(CultureInfo.InvariantCulture) + Text(" | Assignées: ", " | Assigned: ") + assigned.ToString(CultureInfo.InvariantCulture), new GUIStyle(GUI.skin.label) { wordWrap = true });
        }

        private void DrawSquadSection()
        {
            SyncSelectionFromModel();
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(Text("ESCOUADE", "SQUAD"), new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 });
            var ctrl = MobileAccountSessionRuntimeBootstrap.SquadReservationControllerForHiveMap;
            if (ctrl == null || !ctrl.IsConfigured)
            {
                GUILayout.Label(Text("Escouade non configurée — capacité à confirmer côté serveur.", "Squad not configured — capacity pending server."), new GUIStyle(GUI.skin.label) { wordWrap = true });
            }
            else if (ctrl.IsBusy)
            {
                GUILayout.Label(Text("Synchronisation…", "Syncing…"), new GUIStyle(GUI.skin.label) { wordWrap = true });
            }
            else
            {
                var m = ctrl.Model;
                if (m.State == HiveSquadReservationScreenState.Error && !string.IsNullOrWhiteSpace(m.ErrorCode))
                    GUILayout.Label(Text("Erreur: ", "Error: ") + TranslateError(m.ErrorCode), new GUIStyle(GUI.skin.label) { wordWrap = true });
                else if (m.State == HiveSquadReservationScreenState.OfflineReadOnly)
                    GUILayout.Label(Text("Hors ligne — lecture seule.", "Offline — read only."), new GUIStyle(GUI.skin.label) { wordWrap = true });

                int capacity = m.Capacity;
                int totalSel = selGuardians + selWingrunners + selDarters;
                GUILayout.Label(Text("Capacité: ", "Capacity: ") + totalSel.ToString(CultureInfo.InvariantCulture) + " / " + capacity.ToString(CultureInfo.InvariantCulture), new GUIStyle(GUI.skin.label) { wordWrap = true });

                bool hasReservation = m.HasReservation;
                if (hasReservation)
                {
                    GUILayout.Label(Text("Escouade actuelle: ", "Current squad: ") + m.ReservedGuardians + "/" + m.ReservedWingrunners + "/" + m.ReservedDarters + Text(" (G/V/L)", " (G/W/D)"), new GUIStyle(GUI.skin.label) { wordWrap = true });
                }

                DrawSquadRow("guardians", "Gardiennes", "Guardians", m.AvailableGuardians, ref selGuardians, capacity, totalSel);
                DrawSquadRow("wingrunners", "Voltigeuses", "Wingrunners", m.AvailableWingrunners, ref selWingrunners, capacity, totalSel);
                DrawSquadRow("darters", "Lanceuses", "Darters", m.AvailableDarters, ref selDarters, capacity, totalSel);

                GUILayout.Label(Text("TOTAL ", "TOTAL ") + totalSel + " / " + capacity, new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = true });

                bool canCommit = !hasReservation && totalSel > 0 && totalSel <= capacity && selGuardians <= m.AvailableGuardians && selWingrunners <= m.AvailableWingrunners && selDarters <= m.AvailableDarters;
                bool canRelease = hasReservation;

                GUILayout.BeginHorizontal();
                GUI.enabled = canCommit && !ctrl.IsBusy;
                if (GUILayout.Button(Text("CONFIRMER L'ESCOUADE", "CONFIRM SQUAD"), GUILayout.Height(36f)))
                {
                    ctrl.Commit(selGuardians, selWingrunners, selDarters);
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUI.enabled = canRelease && !ctrl.IsBusy;
                if (GUILayout.Button(Text("LIBÉRER L'ESCOUADE", "RELEASE SQUAD"), GUILayout.Height(32f)))
                {
                    ctrl.Release();
                    selGuardians = selWingrunners = selDarters = 0;
                }
                GUI.enabled = true;
                if (GUILayout.Button(Text("Vider", "Clear"), GUILayout.Width(70f), GUILayout.Height(32f)))
                {
                    selGuardians = selWingrunners = selDarters = 0;
                }
                GUILayout.EndHorizontal();

                if (m.State == HiveSquadReservationScreenState.PendingConfirmation)
                    GUILayout.Label(Text("En attente de confirmation…", "Pending confirmation…"), new GUIStyle(GUI.skin.label) { wordWrap = true });
            }
            GUILayout.EndVertical();
        }

        private void DrawSquadRow(string familyKey, string fr, string en, long available, ref int selected, int capacity, int totalSel)
        {
            string name = Text(fr, en);
            GUILayout.BeginHorizontal();
            GUILayout.Label(name, GUILayout.Width(110f));
            bool canDec = selected > 0;
            bool canInc = selected < available && totalSel < capacity;
            GUI.enabled = canDec;
            if (GUILayout.Button("-", GUILayout.Width(32f), GUILayout.Height(28f))) selected = Math.Max(0, selected - 1);
            GUI.enabled = true;
            GUILayout.Label(selected.ToString(CultureInfo.InvariantCulture), GUILayout.Width(30f));
            GUI.enabled = canInc;
            if (GUILayout.Button("+", GUILayout.Width(32f), GUILayout.Height(28f)))
            {
                selected = Math.Min((int)available, selected + 1);
                // M038-CL: real, local-only interaction (adjusts squad selection, no server call) - does not
                // depend on CombatSquadReservation being enabled, so it stays usable even while Confirm Squad is off.
                try { BeeKingdom.Tutorial.TutorialGameplayNotifier.NotifyArmyInteracted(familyKey); } catch {}
            }
            GUI.enabled = true;
            if (GUILayout.Button("MAX", GUILayout.Width(48f), GUILayout.Height(28f)))
            {
                int maxByAvailable = (int)available;
                int maxByCapacity = capacity - (totalSel - selected);
                selected = Math.Min(maxByAvailable, Math.Max(0, maxByCapacity));
            }
            GUILayout.Label(Text(" dispo ", " avail ") + available, new GUIStyle(GUI.skin.label) { fontSize = 10 }, GUILayout.Width(90f));
            GUILayout.EndHorizontal();
        }

        private static string TranslateError(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "";
            switch (code)
            {
                case "server_unavailable": return TextStatic("Service temporairement indisponible", "Service temporarily unavailable");
                case "not_configured": return TextStatic("Fonction non disponible", "Feature unavailable");
                case "precondition_failed": return TextStatic("Préparation requise", "Preparation required");
                case "over_reserved": return TextStatic("Trop de troupes demandées", "Too many troops requested");
                case "squad_in_use": return TextStatic("Escouade déjà en mission", "Squad already on mission");
                case "revision_conflict": return TextStatic("Conflit — rafraîchissez", "Conflict — refresh");
                case "protected_storage_unavailable": return TextStatic("Stockage protégé indisponible", "Protected storage unavailable");
                case "network_unavailable": return TextStatic("Réseau indisponible", "Network unavailable");
                default: return code;
            }
        }

        private static string TextStatic(string fr, string en)
        {
            return string.Equals(BeeLocalization.CurrentLocale, "en-US", StringComparison.OrdinalIgnoreCase) ? en : fr;
        }

        private void DrawDoctrineSection()
        {
            // Doctrine is distinct from Barrack training but currently not player-facing as a separate Army action.
            // To avoid confusing duplicate recruitment, we classify it as informational and keep Barrack as the training owner.
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(Text("DOCTRINE", "DOCTRINE"), new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 });
            GUILayout.Label(Text("Recrutement doctrinal — voir Caserne. L'Armée utilise les troupes formées.", "Doctrinal recruitment — see Barrack. Army uses trained roster."), new GUIStyle(GUI.skin.label) { wordWrap = true });
            var ctrl = MobileAccountSessionRuntimeBootstrap.DoctrineRecruitmentControllerForHiveMap;
            if (ctrl != null && ctrl.IsConfigured && !ctrl.IsBusy)
                GUILayout.Label(Text("Doctrine prête — gérée via Caserne.", "Doctrine ready — managed via Barrack."), new GUIStyle(GUI.skin.label) { wordWrap = true });
            if (GUILayout.Button(Text("Ouvrir Caserne", "Open Barrack"))) HiveViewProductUiPresenter.OpenBarrackOverlayForExternalHost();
            GUILayout.EndVertical();
        }

        private void DrawSortieSection()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(Text("OPÉRATIONS", "OPERATIONS"), new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 });
            var ctrl = MobileAccountSessionRuntimeBootstrap.PerimeterSortieControllerForHiveMap;
            if (ctrl == null || !ctrl.IsConfigured)
            {
                GUILayout.Label(Text("Sortie périmètre non configurée — fonctionnalité en préparation.", "Perimeter sortie not configured — feature in preparation."), new GUIStyle(GUI.skin.label) { wordWrap = true });
            }
            else if (ctrl.IsBusy)
            {
                GUILayout.Label(Text("Synchronisation…", "Syncing…"), new GUIStyle(GUI.skin.label) { wordWrap = true });
            }
            else
            {
                var m = ctrl.Model;
                if (m.State == HivePerimeterSortieScreenState.Error)
                {
                    GUILayout.Label(Text("Sortie indisponible — ", "Sortie unavailable — ") + TranslateError(m.ErrorCode), new GUIStyle(GUI.skin.label) { wordWrap = true });
                    GUILayout.Label(Text("Vérifiez la réserve d'escouade ou réessayez plus tard.", "Check squad reservation or try again later."), new GUIStyle(GUI.skin.label) { wordWrap = true });
                }
                else if (m.State == HivePerimeterSortieScreenState.NeedsReservation)
                {
                    GUILayout.Label(Text("Préparation : Réserve d'escouade requise.", "Preparation: Squad reservation required."), new GUIStyle(GUI.skin.label) { wordWrap = true });
                }
                else
                {
                    string playerState = m.State == HivePerimeterSortieScreenState.ReadyToLaunch ? Text("Prête au départ", "Ready to deploy") : m.State.ToString();
                    GUILayout.Label(Text("Disponibilité: ", "Availability: ") + playerState, new GUIStyle(GUI.skin.label) { wordWrap = true });
                }
            }
            GUILayout.EndVertical();
        }

        private void DrawPatrolSection()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(Text("PATROUILLE", "PATROL"), new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 });
            var ctrl = MobileAccountSessionRuntimeBootstrap.CombatPatrolControllerForHiveMap;
            if (ctrl == null || !ctrl.IsConfigured)
            {
                GUILayout.Label(Text("Patrouille non configurée — préparation disponible, combat sur la Carte.", "Patrol not configured — preparation available, combat on World Map."), new GUIStyle(GUI.skin.label) { wordWrap = true });
            }
            else if (ctrl.IsBusy)
            {
                GUILayout.Label(Text("Synchronisation…", "Syncing…"), new GUIStyle(GUI.skin.label) { wordWrap = true });
            }
            else
            {
                var m = ctrl.Model;
                if (m.State == CombatPatrolScreenState.Error && !string.IsNullOrWhiteSpace(m.ErrorCode))
                {
                    GUILayout.Label(Text("Indisponible — ", "Unavailable — ") + TranslateError(m.ErrorCode), new GUIStyle(GUI.skin.label) { wordWrap = true });
                }
                else
                {
                    string playerState = m.State == CombatPatrolScreenState.ReadyToLaunch ? Text("Prête au départ", "Ready to deploy") : m.State.ToString();
                    GUILayout.Label(Text("Disponibilité: ", "Availability: ") + playerState, new GUIStyle(GUI.skin.label) { wordWrap = true });
                }
            }
            GUILayout.EndVertical();
        }

        private static string Text(string fr, string en)
        {
            return string.Equals(BeeLocalization.CurrentLocale, "en-US", StringComparison.OrdinalIgnoreCase) ? en : fr;
        }
    }
}
