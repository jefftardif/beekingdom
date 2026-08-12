using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed class WorldMapLocalLabRuntime
    {
        private const string SaveFileName = "world_map_local_lab_two_hives_v1.json";
        private const float HiveMinWorldPadding = 80f;
        private const float CollectionToDuration = 1.55f;
        private const float CollectionWorkDuration = 0.75f;
        private const float CollectionBackDuration = 1.55f;
        private const float CombatToDuration = 1.75f;
        private const float CombatImpactDuration = 0.55f;
        private const float CombatBackDuration = 1.75f;
        private const string PremiumHiveResourceRoot = "WorldMapRuntimeEntitiesWave1";

        private readonly List<LabTelemetryEntry> telemetry = new List<LabTelemetryEntry>();
        private readonly Dictionary<string, Texture2D> premiumHiveCache = new Dictionary<string, Texture2D>();
        private readonly string[] classLabels = { "Neutral", "RoyalGuard", "Striker", "Nurturer", "Scout", "Alchemist" };
        private readonly string[] scenarioLabels = { "Collecte R3", "Duel ruches", "Raid T7" };
        private LabState state;
        private Texture2D pixel;
        private Rect worldBounds;
        private Func<string, bool> scenarioHandler;
        private Vector2 collectionNodeWorld;
        private LabActionKind actionKind = LabActionKind.None;
        private float actionTimer;
        private bool actionResultApplied;
        private string status = "Pret";
        private string selectedHiveId = "PLAYER_TEST_HIVE";
        private Vector2 scroll;
        private int hiveTab;

        public bool IsReady => state != null && state.player != null && state.enemy != null;
        public string Status => status;
        public string SavePath { get; private set; }

        public void Initialize(Rect bounds, Func<string, bool> onScenarioRequested = null)
        {
            worldBounds = bounds;
            scenarioHandler = onScenarioRequested;
            SavePath = Path.Combine(Application.persistentDataPath, SaveFileName);
            EnsurePixel();
            LoadOrReset(false);
            state.collapsed = true;
            ClampStateToWorld();
            RebuildCollectionNode();
            AddTelemetry("init", "LAB_LOCAL_READY local only, save=" + SavePath);
        }

        public void Update(float deltaTime)
        {
            if (!IsReady || actionKind == LabActionKind.None) return;
            actionTimer += Mathf.Max(0f, deltaTime);

            if (!actionResultApplied && actionKind == LabActionKind.Collection && actionTimer >= CollectionToDuration + CollectionWorkDuration + CollectionBackDuration)
            {
                ApplyCollectionResult();
            }
            else if (!actionResultApplied && actionKind == LabActionKind.Combat && actionTimer >= CombatToDuration + CombatImpactDuration + CombatBackDuration)
            {
                ApplyCombatResult();
            }
        }

        public void DrawWorld(Func<Vector2, Vector2> worldToScreen, float animatedTime)
        {
            if (!IsReady || worldToScreen == null) return;
            DrawCollectionNode(worldToScreen, animatedTime);
            DrawLabHive(state.player, worldToScreen, animatedTime, true);
            DrawLabHive(state.enemy, worldToScreen, animatedTime + 1.1f, false);
            DrawActionFlight(worldToScreen, animatedTime);
        }

        public void DrawHud()
        {
            if (!IsReady) return;
            Rect header = HeaderRect();
            DrawSolid(header, new Color(0.022f, 0.026f, 0.020f, 0.92f));
            DrawFrame(header, new Color(0.36f, 0.92f, 0.68f, 0.90f), 2f);

            if (GUI.Button(new Rect(header.x + 8f, header.y + 6f, 34f, 30f), state.collapsed ? "+" : "-", ButtonStyle()))
            {
                state.collapsed = !state.collapsed;
                Save();
            }

            GUI.Label(new Rect(header.x + 50f, header.y + 6f, header.width - 58f, 18f), "LAB LOCAL | NON OFFICIEL", LabelStyle(Color.white, 13, FontStyle.Bold, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(header.x + 50f, header.y + 25f, header.width - 58f, 16f), status + " | aucune progression officielle", LabelStyle(new Color(0.78f, 1f, 0.86f, 1f), 10, FontStyle.Normal, TextAnchor.MiddleLeft));
            if (state.collapsed) return;

            Rect panel = PanelRect();
            DrawSolid(panel, new Color(0.018f, 0.020f, 0.017f, 0.94f));
            DrawFrame(panel, new Color(0.36f, 0.92f, 0.68f, 0.82f), 2f);

            Rect view = new Rect(panel.x + 10f, panel.y + 10f, panel.width - 20f, panel.height - 20f);
            Rect content = new Rect(0f, 0f, view.width - 18f, 760f);
            scroll = GUI.BeginScrollView(view, scroll, content);

            DrawHiveTabs(content.width);
            TestHiveConfig hive = hiveTab == 0 ? state.player : state.enemy;
            float y = 40f;
            DrawHiveEditor(hive, 0f, ref y, content.width);
            y += 8f;
            DrawScenarioSelector(0f, ref y, content.width);
            y += 8f;
            DrawActionButtons(0f, ref y, content.width);
            y += 8f;
            DrawTelemetrySummary(0f, ref y, content.width);

            GUI.EndScrollView();
        }

        public bool IsPointerOverUi(Vector2 guiPoint)
        {
            return HeaderRect().Contains(guiPoint) || (!state.collapsed && PanelRect().Contains(guiPoint));
        }

        public LabProofSnapshot CurrentProofSnapshot()
        {
            if (!IsReady) return new LabProofSnapshot(false, string.Empty, string.Empty, 0, 0, false, false, false, false, string.Empty);
            return new LabProofSnapshot(
                true,
                CoordLabel(state.player.worldPosition),
                CoordLabel(state.enemy.worldPosition),
                state.player.level,
                state.enemy.level,
                state.player.health > 0,
                state.enemy.health > 0,
                state.localOnly,
                PremiumHiveTexture(state.player) != null && PremiumHiveTexture(state.enemy) != null,
                LastTelemetryShort());
        }

        public bool RunCollectionForProof()
        {
            if (!IsReady) return false;
            StartCollection();
            actionTimer = CollectionToDuration + CollectionWorkDuration + CollectionBackDuration;
            Update(0f);
            return actionResultApplied && state.player.stock.nectar >= 144;
        }

        public bool RunCombatForProof()
        {
            if (!IsReady) return false;
            StartCombat();
            actionTimer = CombatToDuration + CombatImpactDuration + CombatBackDuration;
            Update(0f);
            return actionResultApplied && state.enemy.health < 460;
        }

        public HiveVisualProofSnapshot RunHiveVisualProgressionProofForProof()
        {
            if (!IsReady) return new HiveVisualProofSnapshot(false, string.Empty, string.Empty, string.Empty, string.Empty, false, false, false, false);
            Vector2 playerStart = state.player.worldPosition;
            Vector2 enemyStart = state.enemy.worldPosition;
            bool neutralLevel4 = SetHiveVisualForProof(state.player, 4, HiveClass.Neutral) && PremiumHiveResourcePath(state.player).Contains("/H1/hive_neutral_l4");
            bool allLevel10Classes = true;
            HiveClass[] classes =
            {
                HiveClass.RoyalGuard,
                HiveClass.Striker,
                HiveClass.Nurturer,
                HiveClass.Scout,
                HiveClass.Alchemist
            };

            for (int i = 0; i < classes.Length; i++)
            {
                allLevel10Classes &= SetHiveVisualForProof(state.player, 10, classes[i]);
                allLevel10Classes &= PremiumHiveResourcePath(state.player).Contains("/H2/hive_" + HiveClassToken(classes[i]) + "_l10");
            }

            bool level35 = SetHiveVisualForProof(state.player, 35, HiveClass.Alchemist) && PremiumHiveResourcePath(state.player).Contains("/H3/hive_alchemist_l35");
            bool enemyDistinct = SetHiveVisualForProof(state.enemy, 10, HiveClass.Striker);
            state.player.faction = "PLAYER_LOCAL";
            state.enemy.faction = "ENEMY_LOCAL";
            Save();
            bool positionStable = state.player.worldPosition == playerStart && state.enemy.worldPosition == enemyStart;
            bool distinctSprites = PremiumHiveResourcePath(state.player) != PremiumHiveResourcePath(state.enemy);
            return new HiveVisualProofSnapshot(
                neutralLevel4 && allLevel10Classes && level35 && enemyDistinct && positionStable && distinctSprites,
                PremiumHiveResourcePath(state.player),
                PremiumHiveResourcePath(state.enemy),
                state.player.faction,
                state.enemy.faction,
                neutralLevel4,
                allLevel10Classes,
                level35,
                positionStable && distinctSprites);
        }

        public bool ResetForProof()
        {
            LoadOrReset(true);
            ClampStateToWorld();
            RebuildCollectionNode();
            Save();
            AddTelemetry("reset", "proof reset defaults applied");
            return IsReady && state.player.id == "PLAYER_TEST_HIVE" && state.enemy.id == "ENEMY_TEST_HIVE";
        }

        public static string[] ProofRows()
        {
            return new[]
            {
                "world_map_local_lab:true",
                "local_lab_hives:PLAYER_TEST_HIVE,ENEMY_TEST_HIVE",
                "local_lab_editable_position:true",
                "local_lab_editable_level_1_50:true",
                "local_lab_classes:Neutral,RoyalGuard,Striker,Nurturer,Scout,Alchemist",
                "local_lab_editable_units:soldiers,guards,scouts,workers",
                "local_lab_editable_health:true",
                "local_lab_resources:Nectar,Pollen,Eau,Cire,Miel,GeleeRoyale,Propolis",
                "local_lab_stock_and_capacity:true",
                "local_lab_editable_faction_and_name:true",
                "local_lab_hud_compact_collapsible:true",
                "local_lab_buttons:Apply,Reset,Test collecte,Test combat",
                "local_lab_scenarios:CollecteR3,DuelDeuxRuches,RaidT7",
                "local_lab_collection_deterministic:true",
                "local_lab_combat_deterministic:true",
                "local_lab_official_gain:false",
                "local_lab_server:false",
                "local_lab_remote:false",
                "local_lab_real_data:false",
                "local_lab_serialized_local_only:true"
            };
        }

        private void LoadOrReset(bool forceReset)
        {
            if (!forceReset && File.Exists(SavePath))
            {
                try
                {
                    state = JsonUtility.FromJson<LabState>(File.ReadAllText(SavePath));
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[WorldMap Local Lab] Save ignored: " + exception.Message);
                    state = null;
                }
            }

            if (state == null || state.player == null || state.enemy == null)
            {
                state = CreateDefaultState();
                Save();
            }

            state.localOnly = true;
            state.authorityServer = false;
            state.officialGain = false;
            state.selectedScenarioIndex = Mathf.Clamp(state.selectedScenarioIndex, 0, scenarioLabels.Length - 1);
        }

        private LabState CreateDefaultState()
        {
            Vector2 center = worldBounds.width > 1f ? worldBounds.center : new Vector2(16640f, 16640f);
            return new LabState
            {
                localOnly = true,
                authorityServer = false,
                officialGain = false,
                collapsed = false,
                selectedScenarioIndex = 0,
                player = new TestHiveConfig
                {
                    id = "PLAYER_TEST_HIVE",
                    displayName = "Ruche test joueur",
                    faction = "PLAYER_LOCAL",
                    hiveClass = HiveClass.Nurturer,
                    level = 12,
                    soldiers = 38,
                    guards = 16,
                    scouts = 12,
                    workers = 72,
                    health = 520,
                    maxHealth = 620,
                    worldPosition = center + new Vector2(-300f, 110f),
                    stock = new LabResourceSet { nectar = 120, pollen = 90, water = 70, wax = 35, honey = 42, royalJelly = 3, propolis = 12 },
                    capacity = new LabResourceSet { nectar = 280, pollen = 240, water = 220, wax = 160, honey = 180, royalJelly = 25, propolis = 90 }
                },
                enemy = new TestHiveConfig
                {
                    id = "ENEMY_TEST_HIVE",
                    displayName = "Ruche test ennemie",
                    faction = "ENEMY_LOCAL",
                    hiveClass = HiveClass.Striker,
                    level = 14,
                    soldiers = 54,
                    guards = 20,
                    scouts = 18,
                    workers = 44,
                    health = 460,
                    maxHealth = 560,
                    worldPosition = center + new Vector2(330f, -90f),
                    stock = new LabResourceSet { nectar = 85, pollen = 76, water = 54, wax = 28, honey = 31, royalJelly = 2, propolis = 16 },
                    capacity = new LabResourceSet { nectar = 230, pollen = 210, water = 190, wax = 145, honey = 160, royalJelly = 22, propolis = 82 }
                }
            };
        }

        private void ClampStateToWorld()
        {
            ClampHive(state.player);
            ClampHive(state.enemy);
        }

        private void ClampHive(TestHiveConfig hive)
        {
            hive.level = Mathf.Clamp(hive.level, 1, 50);
            hive.soldiers = Mathf.Max(0, hive.soldiers);
            hive.guards = Mathf.Max(0, hive.guards);
            hive.scouts = Mathf.Max(0, hive.scouts);
            hive.workers = Mathf.Max(0, hive.workers);
            hive.maxHealth = Mathf.Max(1, hive.maxHealth);
            hive.health = Mathf.Clamp(hive.health, 0, hive.maxHealth);
            hive.worldPosition = new Vector2(
                Mathf.Clamp(hive.worldPosition.x, worldBounds.xMin + HiveMinWorldPadding, worldBounds.xMax - HiveMinWorldPadding),
                Mathf.Clamp(hive.worldPosition.y, worldBounds.yMin + HiveMinWorldPadding, worldBounds.yMax - HiveMinWorldPadding));
            ClampStock(hive.stock, hive.capacity);
        }

        private static void ClampStock(LabResourceSet stock, LabResourceSet capacity)
        {
            capacity.nectar = Mathf.Max(0, capacity.nectar);
            capacity.pollen = Mathf.Max(0, capacity.pollen);
            capacity.water = Mathf.Max(0, capacity.water);
            capacity.wax = Mathf.Max(0, capacity.wax);
            capacity.honey = Mathf.Max(0, capacity.honey);
            capacity.royalJelly = Mathf.Max(0, capacity.royalJelly);
            capacity.propolis = Mathf.Max(0, capacity.propolis);
            stock.nectar = Mathf.Clamp(stock.nectar, 0, capacity.nectar);
            stock.pollen = Mathf.Clamp(stock.pollen, 0, capacity.pollen);
            stock.water = Mathf.Clamp(stock.water, 0, capacity.water);
            stock.wax = Mathf.Clamp(stock.wax, 0, capacity.wax);
            stock.honey = Mathf.Clamp(stock.honey, 0, capacity.honey);
            stock.royalJelly = Mathf.Clamp(stock.royalJelly, 0, capacity.royalJelly);
            stock.propolis = Mathf.Clamp(stock.propolis, 0, capacity.propolis);
        }

        private void RebuildCollectionNode()
        {
            collectionNodeWorld = (state.player.worldPosition + state.enemy.worldPosition) * 0.5f + new Vector2(0f, -180f);
            collectionNodeWorld.x = Mathf.Clamp(collectionNodeWorld.x, worldBounds.xMin + HiveMinWorldPadding, worldBounds.xMax - HiveMinWorldPadding);
            collectionNodeWorld.y = Mathf.Clamp(collectionNodeWorld.y, worldBounds.yMin + HiveMinWorldPadding, worldBounds.yMax - HiveMinWorldPadding);
        }

        private void StartCollection()
        {
            ClampStateToWorld();
            RebuildCollectionNode();
            actionKind = LabActionKind.Collection;
            actionTimer = 0f;
            actionResultApplied = false;
            status = "Collecte locale en cours";
            AddTelemetry("collection_start", "PLAYER_TEST_HIVE -> NODE_TEST nectar+24 pollen+10 eau+6 server=false official_gain=false");
        }

        private void StartCombat()
        {
            ClampStateToWorld();
            actionKind = LabActionKind.Combat;
            actionTimer = 0f;
            actionResultApplied = false;
            status = "Combat local en cours";
            AddTelemetry("combat_start", "PLAYER_TEST_HIVE -> ENEMY_TEST_HIVE deterministic server=false official_gain=false");
        }

        private void ApplyCollectionResult()
        {
            AddStock(state.player.stock, state.player.capacity, 24, 10, 6, 0, 0, 0, 0);
            actionResultApplied = true;
            status = "Collecte locale terminee +24 Nectar +10 Pollen +6 Eau";
            AddTelemetry("collection_result", "delta exact: Nectar=+24,Pollen=+10,Eau=+6, official_gain=false");
            Save();
        }

        private void ApplyCombatResult()
        {
            int attack = state.player.soldiers * 3 + state.player.scouts + state.player.level * 2;
            int defense = state.enemy.guards * 2 + state.enemy.level;
            int damage = Mathf.Clamp(attack - defense / 2, 12, 180);
            state.enemy.health = Mathf.Max(0, state.enemy.health - damage);
            int returnDamage = Mathf.Clamp(state.enemy.soldiers + state.enemy.level - state.player.guards, 0, 80);
            state.player.health = Mathf.Max(0, state.player.health - returnDamage);
            actionResultApplied = true;
            status = "Combat local termine degats " + damage.ToString(CultureInfo.InvariantCulture);
            AddTelemetry("combat_result", "enemy_hp_delta=-" + damage.ToString(CultureInfo.InvariantCulture) + ",player_hp_delta=-" + returnDamage.ToString(CultureInfo.InvariantCulture) + ", official_gain=false");
            Save();
        }

        private static void AddStock(LabResourceSet stock, LabResourceSet capacity, int nectar, int pollen, int water, int wax, int honey, int royalJelly, int propolis)
        {
            stock.nectar = Mathf.Clamp(stock.nectar + nectar, 0, capacity.nectar);
            stock.pollen = Mathf.Clamp(stock.pollen + pollen, 0, capacity.pollen);
            stock.water = Mathf.Clamp(stock.water + water, 0, capacity.water);
            stock.wax = Mathf.Clamp(stock.wax + wax, 0, capacity.wax);
            stock.honey = Mathf.Clamp(stock.honey + honey, 0, capacity.honey);
            stock.royalJelly = Mathf.Clamp(stock.royalJelly + royalJelly, 0, capacity.royalJelly);
            stock.propolis = Mathf.Clamp(stock.propolis + propolis, 0, capacity.propolis);
        }

        private void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            File.WriteAllText(SavePath, JsonUtility.ToJson(state, true));
        }

        private void AddTelemetry(string type, string detail)
        {
            telemetry.Add(new LabTelemetryEntry(type, detail, Time.realtimeSinceStartup));
            while (telemetry.Count > 8) telemetry.RemoveAt(0);
        }

        private string LastTelemetryShort()
        {
            if (telemetry.Count == 0) return "aucune telemetrie";
            LabTelemetryEntry last = telemetry[telemetry.Count - 1];
            return last.type + ": " + last.detail;
        }

        private void DrawHiveTabs(float width)
        {
            if (GUI.Button(new Rect(0f, 0f, width * 0.5f - 4f, 30f), "PLAYER_TEST_HIVE", hiveTab == 0 ? SelectedButtonStyle() : ButtonStyle())) hiveTab = 0;
            if (GUI.Button(new Rect(width * 0.5f + 4f, 0f, width * 0.5f - 4f, 30f), "ENEMY_TEST_HIVE", hiveTab == 1 ? SelectedButtonStyle() : ButtonStyle())) hiveTab = 1;
        }

        private void DrawHiveEditor(TestHiveConfig hive, float x, ref float y, float width)
        {
            GUI.Label(new Rect(x, y, width, 20f), hive.id + " | " + hive.displayName, LabelStyle(Color.white, 12, FontStyle.Bold, TextAnchor.MiddleLeft));
            y += 24f;
            hive.displayName = TextRow("Nom", hive.displayName, x, ref y, width);
            hive.faction = TextRow("Faction", hive.faction, x, ref y, width);
            DrawIntRow("Niveau", ref hive.level, 1, 50, x, ref y, width);
            DrawClassRow(hive, x, ref y, width);
            DrawVectorRow("Position", ref hive.worldPosition, x, ref y, width);
            DrawIntRow("PV", ref hive.health, 0, 999999, x, ref y, width * 0.48f);
            float oldY = y - 26f;
            DrawIntField("PV max", ref hive.maxHealth, 1, 999999, x + width * 0.52f, oldY, width * 0.48f);
            DrawIntRow("Soldats", ref hive.soldiers, 0, 999999, x, ref y, width * 0.48f);
            oldY = y - 26f;
            DrawIntField("Gardiennes", ref hive.guards, 0, 999999, x + width * 0.52f, oldY, width * 0.48f);
            DrawIntRow("Eclaireuses", ref hive.scouts, 0, 999999, x, ref y, width * 0.48f);
            oldY = y - 26f;
            DrawIntField("Ouvrieres", ref hive.workers, 0, 999999, x + width * 0.52f, oldY, width * 0.48f);
            y += 4f;
            GUI.Label(new Rect(x, y, width, 18f), "Stocks / capacites", LabelStyle(new Color(0.78f, 1f, 0.86f, 1f), 11, FontStyle.Bold, TextAnchor.MiddleLeft));
            y += 22f;
            DrawResourceRow("Nectar", ref hive.stock.nectar, ref hive.capacity.nectar, x, ref y, width);
            DrawResourceRow("Pollen", ref hive.stock.pollen, ref hive.capacity.pollen, x, ref y, width);
            DrawResourceRow("Eau", ref hive.stock.water, ref hive.capacity.water, x, ref y, width);
            DrawResourceRow("Cire", ref hive.stock.wax, ref hive.capacity.wax, x, ref y, width);
            DrawResourceRow("Miel", ref hive.stock.honey, ref hive.capacity.honey, x, ref y, width);
            DrawResourceRow("Gelee royale", ref hive.stock.royalJelly, ref hive.capacity.royalJelly, x, ref y, width);
            DrawResourceRow("Propolis", ref hive.stock.propolis, ref hive.capacity.propolis, x, ref y, width);
        }

        private void DrawScenarioSelector(float x, ref float y, float width)
        {
            GUI.Label(new Rect(x, y, width, 18f), "Scenario local", LabelStyle(new Color(0.78f, 1f, 0.86f, 1f), 11, FontStyle.Bold, TextAnchor.MiddleLeft));
            y += 22f;
            state.selectedScenarioIndex = GUI.SelectionGrid(new Rect(x, y, width, 30f), Mathf.Clamp(state.selectedScenarioIndex, 0, scenarioLabels.Length - 1), scenarioLabels, 3);
            y += 36f;
            if (GUI.Button(new Rect(x, y, width, 28f), "Appliquer scenario local", ButtonStyle()))
            {
                ApplySelectedScenarioPreset();
            }
            y += 34f;
            GUI.Label(new Rect(x, y, width, 18f), ScenarioDescription(state.selectedScenarioIndex), LabelStyle(new Color(0.82f, 0.90f, 0.86f, 1f), 9, FontStyle.Normal, TextAnchor.MiddleLeft));
            y += 22f;
        }

        public bool ApplyScenarioPresetForProof(int index)
        {
            if (!IsReady) return false;
            state.selectedScenarioIndex = Mathf.Clamp(index, 0, scenarioLabels.Length - 1);
            ApplySelectedScenarioPreset();
            return state.localOnly && !state.authorityServer && !state.officialGain;
        }

        public ScenarioLabProofSnapshot CurrentScenarioLabProofSnapshot()
        {
            if (!IsReady) return new ScenarioLabProofSnapshot(false, string.Empty, false, false, false);
            return new ScenarioLabProofSnapshot(
                true,
                ScenarioId(state.selectedScenarioIndex),
                !state.authorityServer,
                !state.officialGain,
                state.player.id == "PLAYER_TEST_HIVE" && state.enemy.id == "ENEMY_TEST_HIVE");
        }

        private void ApplySelectedScenarioPreset()
        {
            ClampStateToWorld();
            string scenarioId = ScenarioId(state.selectedScenarioIndex);
            if (scenarioId == "scenario_collect_r3")
            {
                state.player.level = Mathf.Max(state.player.level, 12);
                state.player.hiveClass = HiveClass.Nurturer;
                state.player.workers = Mathf.Max(state.player.workers, 96);
                state.player.scouts = Mathf.Max(state.player.scouts, 18);
                state.player.capacity.nectar = Mathf.Max(state.player.capacity.nectar, 360);
                state.player.capacity.pollen = Mathf.Max(state.player.capacity.pollen, 320);
                state.player.capacity.water = Mathf.Max(state.player.capacity.water, 260);
                state.player.stock.nectar = Mathf.Min(state.player.stock.nectar, state.player.capacity.nectar - 80);
                status = "Scenario Collecte R3 pret";
            }
            else if (scenarioId == "scenario_duel_two_hives")
            {
                state.player.level = 18;
                state.player.hiveClass = HiveClass.RoyalGuard;
                state.player.soldiers = 72;
                state.player.guards = 42;
                state.player.scouts = 18;
                state.player.workers = 70;
                state.player.maxHealth = 720;
                state.player.health = 720;
                state.enemy.level = 18;
                state.enemy.hiveClass = HiveClass.Striker;
                state.enemy.soldiers = 82;
                state.enemy.guards = 28;
                state.enemy.scouts = 24;
                state.enemy.workers = 48;
                state.enemy.maxHealth = 680;
                state.enemy.health = 680;
                status = "Scenario duel deux ruches pret";
            }
            else
            {
                state.player.level = 35;
                state.player.hiveClass = HiveClass.Alchemist;
                state.player.soldiers = 140;
                state.player.guards = 86;
                state.player.scouts = 70;
                state.player.workers = 180;
                state.player.maxHealth = 1200;
                state.player.health = 1200;
                state.enemy.level = 35;
                state.enemy.hiveClass = HiveClass.Striker;
                state.enemy.maxHealth = 900;
                state.enemy.health = 900;
                status = "Scenario Raid T7 pret";
            }

            state.localOnly = true;
            state.authorityServer = false;
            state.officialGain = false;
            ClampStateToWorld();
            RebuildCollectionNode();
            Save();
            AddTelemetry("scenario", scenarioId + " applied local_demo server=false official_gain=false");
            bool runtimeApplied = scenarioHandler == null || scenarioHandler.Invoke(scenarioId);
            if (!runtimeApplied) AddTelemetry("scenario_warning", scenarioId + " runtime target not visible yet");
        }

        private string ScenarioDescription(int index)
        {
            string id = ScenarioId(index);
            if (id == "scenario_collect_r3") return "Prepare collecte riche locale, quantite/epuisement/respawn demo.";
            if (id == "scenario_duel_two_hives") return "Prepare duel deterministe entre PLAYER_TEST_HIVE et ENEMY_TEST_HIVE.";
            return "Prepare raid local T7, composition forte, aucun gain officiel.";
        }

        private string ScenarioId(int index)
        {
            if (index == 1) return "scenario_duel_two_hives";
            if (index == 2) return "scenario_raid_t7";
            return "scenario_collect_r3";
        }

        private void DrawActionButtons(float x, ref float y, float width)
        {
            float buttonWidth = (width - 18f) / 4f;
            if (GUI.Button(new Rect(x, y, buttonWidth, 32f), "Apply", ButtonStyle()))
            {
                ClampStateToWorld();
                RebuildCollectionNode();
                Save();
                status = "Parametres appliques et sauvegardes localement";
                AddTelemetry("apply", "serialized local only");
            }
            if (GUI.Button(new Rect(x + buttonWidth + 6f, y, buttonWidth, 32f), "Reset", ButtonStyle()))
            {
                LoadOrReset(true);
                ClampStateToWorld();
                RebuildCollectionNode();
                Save();
                status = "Labo reinitialise";
                AddTelemetry("reset", "defaults restored");
            }
            GUI.enabled = actionKind == LabActionKind.None || actionResultApplied;
            if (GUI.Button(new Rect(x + (buttonWidth + 6f) * 2f, y, buttonWidth, 32f), "Test collecte", ButtonStyle()))
            {
                StartCollection();
            }
            if (GUI.Button(new Rect(x + (buttonWidth + 6f) * 3f, y, buttonWidth, 32f), "Test combat", ButtonStyle()))
            {
                StartCombat();
            }
            GUI.enabled = true;
            y += 40f;
        }

        private void DrawTelemetrySummary(float x, ref float y, float width)
        {
            GUI.Label(new Rect(x, y, width, 18f), "Telemetrie locale", LabelStyle(Color.white, 11, FontStyle.Bold, TextAnchor.MiddleLeft));
            y += 22f;
            int start = Mathf.Max(0, telemetry.Count - 4);
            for (int i = start; i < telemetry.Count; i++)
            {
                GUI.Label(new Rect(x, y, width, 18f), telemetry[i].type + " | " + telemetry[i].detail, LabelStyle(new Color(0.82f, 0.90f, 0.86f, 1f), 9, FontStyle.Normal, TextAnchor.MiddleLeft));
                y += 20f;
            }
            GUI.Label(new Rect(x, y, width, 36f), "Aucun serveur, aucune persistance officielle, aucun gain officiel.", LabelStyle(new Color(1f, 0.86f, 0.48f, 1f), 10, FontStyle.Normal, TextAnchor.UpperLeft));
        }

        private string TextRow(string label, string value, float x, ref float y, float width)
        {
            GUI.Label(new Rect(x, y, width * 0.32f, 20f), label, LabelStyle(Color.white, 10, FontStyle.Normal, TextAnchor.MiddleLeft));
            string next = GUI.TextField(new Rect(x + width * 0.34f, y, width * 0.66f, 22f), value ?? string.Empty);
            y += 26f;
            return next;
        }

        private void DrawClassRow(TestHiveConfig hive, float x, ref float y, float width)
        {
            GUI.Label(new Rect(x, y, width * 0.32f, 20f), "Classe", LabelStyle(Color.white, 10, FontStyle.Normal, TextAnchor.MiddleLeft));
            int selected = Mathf.Clamp((int)hive.hiveClass, 0, classLabels.Length - 1);
            selected = GUI.SelectionGrid(new Rect(x + width * 0.34f, y, width * 0.66f, 48f), selected, classLabels, 2);
            hive.hiveClass = (HiveClass)selected;
            y += 54f;
        }

        private void DrawVectorRow(string label, ref Vector2 value, float x, ref float y, float width)
        {
            GUI.Label(new Rect(x, y, width * 0.24f, 20f), label, LabelStyle(Color.white, 10, FontStyle.Normal, TextAnchor.MiddleLeft));
            DrawFloatField("X", ref value.x, x + width * 0.25f, y, width * 0.35f);
            DrawFloatField("Y", ref value.y, x + width * 0.63f, y, width * 0.35f);
            y += 26f;
        }

        private void DrawResourceRow(string label, ref int stock, ref int capacity, float x, ref float y, float width)
        {
            GUI.Label(new Rect(x, y, width * 0.30f, 20f), label, LabelStyle(Color.white, 10, FontStyle.Normal, TextAnchor.MiddleLeft));
            DrawIntField("Stock", ref stock, 0, 999999, x + width * 0.32f, y, width * 0.32f);
            DrawIntField("Cap", ref capacity, 0, 999999, x + width * 0.67f, y, width * 0.31f);
            y += 24f;
        }

        private void DrawIntRow(string label, ref int value, int min, int max, float x, ref float y, float width)
        {
            DrawIntField(label, ref value, min, max, x, y, width);
            y += 26f;
        }

        private void DrawIntField(string label, ref int value, int min, int max, float x, float y, float width)
        {
            GUI.Label(new Rect(x, y, width * 0.42f, 20f), label, LabelStyle(Color.white, 10, FontStyle.Normal, TextAnchor.MiddleLeft));
            string text = GUI.TextField(new Rect(x + width * 0.44f, y, width * 0.56f, 22f), value.ToString(CultureInfo.InvariantCulture));
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                value = Mathf.Clamp(parsed, min, max);
            }
        }

        private void DrawFloatField(string label, ref float value, float x, float y, float width)
        {
            GUI.Label(new Rect(x, y, width * 0.18f, 20f), label, LabelStyle(Color.white, 10, FontStyle.Normal, TextAnchor.MiddleLeft));
            string text = GUI.TextField(new Rect(x + width * 0.20f, y, width * 0.80f, 22f), value.ToString("0", CultureInfo.InvariantCulture));
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                value = parsed;
            }
        }

        private void DrawLabHive(TestHiveConfig hive, Func<Vector2, Vector2> worldToScreen, float animatedTime, bool player)
        {
            Vector2 p = worldToScreen(hive.worldPosition);
            if (!IsOnScreen(p, 130f)) return;
            Color core = ClassColor(hive.hiveClass, player);
            bool selected = hive.id == selectedHiveId;
            float size = 28f + hive.level * 0.45f + Mathf.Sin(animatedTime * 3f) * 1.5f;
            if (selected)
            {
                DrawCircle(p, size + 34f, new Color(0.36f, 0.92f, 0.68f, 0.90f), 34);
            }

            Texture2D premiumTexture = PremiumHiveTexture(hive);
            if (premiumTexture != null)
            {
                float spriteSize = Mathf.Clamp(72f + hive.level * 1.9f, 78f, 156f);
                Rect spriteRect = new Rect(p.x - spriteSize * 0.5f, p.y - spriteSize * 0.60f, spriteSize, spriteSize);
                DrawCircle(p, spriteSize * 0.42f, new Color(core.r, core.g, core.b, 0.16f), 32);
                Color previous = GUI.color;
                GUI.color = Color.white;
                GUI.DrawTexture(spriteRect, premiumTexture, ScaleMode.ScaleToFit, true);
                GUI.color = previous;
            }
            else
            {
                DrawCircle(p, size + 12f, new Color(core.r, core.g, core.b, 0.18f), 32);
                DrawDiamond(p, size, core, 4f);
                DrawCircle(new Vector2(p.x, p.y - size * 0.18f), size * 0.28f, new Color(1f, 0.82f, 0.22f, 0.95f), 16);
                DrawFrame(new Rect(p.x - size * 0.52f, p.y - size * 0.16f, size * 1.04f, size * 0.44f), new Color(0.08f, 0.05f, 0.02f, 0.92f), 2f);
            }

            DrawFactionOverlay(p, size, hive, player);

            Rect badge = new Rect(p.x - 70f, p.y - size - 32f, 140f, 22f);
            DrawSolid(badge, new Color(0.018f, 0.020f, 0.017f, 0.86f));
            DrawFrame(badge, FactionColor(hive, player), 1.5f);
            GUI.Label(badge, hive.id, LabelStyle(Color.white, 10, FontStyle.Bold, TextAnchor.MiddleCenter));

            Rect label = new Rect(p.x - 88f, p.y + size + 4f, 176f, 42f);
            DrawSolid(label, new Color(0.018f, 0.020f, 0.017f, 0.80f));
            GUI.Label(label, hive.displayName + "\nN" + hive.level.ToString(CultureInfo.InvariantCulture) + " " + hive.hiveClass + " PV " + hive.health.ToString(CultureInfo.InvariantCulture), LabelStyle(Color.white, 10, FontStyle.Normal, TextAnchor.MiddleCenter));
        }

        private Texture2D PremiumHiveTexture(TestHiveConfig hive)
        {
            string resourcePath = PremiumHiveResourcePath(hive);
            if (premiumHiveCache.TryGetValue(resourcePath, out Texture2D cached)) return cached;
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                texture.anisoLevel = 1;
            }

            premiumHiveCache[resourcePath] = texture;
            return texture;
        }

        private string PremiumHiveResourcePath(TestHiveConfig hive)
        {
            return ResolveHiveVisual(hive.level, hive.hiveClass);
        }

        private string ResolveHiveVisual(int level, HiveClass hiveClass)
        {
            level = Mathf.Clamp(level, 1, 50);
            if (level < 10 || hiveClass == HiveClass.Neutral)
            {
                int neutralLevel = level >= 9 ? 9 : (level >= 7 ? 7 : (level >= 4 ? 4 : 1));
                return PremiumHiveResourceRoot + "/H1/hive_neutral_l" + neutralLevel.ToString(CultureInfo.InvariantCulture);
            }

            string classToken = HiveClassToken(hiveClass);
            int tierLevel = level >= 50 ? 50 : (level >= 35 ? 35 : (level >= 20 ? 20 : 10));
            string folder = tierLevel == 10 ? "H2" : "H3";
            return PremiumHiveResourceRoot + "/" + folder + "/hive_" + classToken + "_l" + tierLevel.ToString(CultureInfo.InvariantCulture);
        }

        private bool SetHiveVisualForProof(TestHiveConfig hive, int level, HiveClass hiveClass)
        {
            hive.level = Mathf.Clamp(level, 1, 50);
            hive.hiveClass = hiveClass;
            return PremiumHiveTexture(hive) != null;
        }

        private static string HiveClassToken(HiveClass hiveClass)
        {
            if (hiveClass == HiveClass.RoyalGuard) return "royal_guard";
            if (hiveClass == HiveClass.Striker) return "striker";
            if (hiveClass == HiveClass.Nurturer) return "nurturer";
            if (hiveClass == HiveClass.Scout) return "scout";
            if (hiveClass == HiveClass.Alchemist) return "alchemist";
            return "neutral";
        }

        private void DrawCollectionNode(Func<Vector2, Vector2> worldToScreen, float animatedTime)
        {
            Vector2 p = worldToScreen(collectionNodeWorld);
            if (!IsOnScreen(p, 90f)) return;
            float pulse = 1f + Mathf.Sin(animatedTime * 4f) * 0.08f;
            DrawCircle(p, 22f * pulse, new Color(0.65f, 0.44f, 1f, 0.24f), 24);
            DrawCircle(p, 11f * pulse, new Color(0.74f, 0.46f, 1f, 0.95f), 20);
            GUI.Label(new Rect(p.x - 62f, p.y + 18f, 124f, 28f), "NODE_TEST\n+24/+10/+6", LabelStyle(Color.white, 9, FontStyle.Normal, TextAnchor.MiddleCenter));
        }

        private void DrawActionFlight(Func<Vector2, Vector2> worldToScreen, float animatedTime)
        {
            if (actionKind == LabActionKind.None) return;
            Vector2 origin = state.player.worldPosition;
            Vector2 destination = actionKind == LabActionKind.Collection ? collectionNodeWorld : state.enemy.worldPosition;
            float total = actionKind == LabActionKind.Collection ? CollectionToDuration + CollectionWorkDuration + CollectionBackDuration : CombatToDuration + CombatImpactDuration + CombatBackDuration;
            float progress = Mathf.Clamp01(actionTimer / Mathf.Max(0.01f, total));
            bool returning = actionKind == LabActionKind.Collection
                ? actionTimer > CollectionToDuration + CollectionWorkDuration
                : actionTimer > CombatToDuration + CombatImpactDuration;
            float travelProgress = returning ? 1f - Mathf.InverseLerp(total - (actionKind == LabActionKind.Collection ? CollectionBackDuration : CombatBackDuration), total, actionTimer) : Mathf.Clamp01(progress * 1.9f);

            Vector2 a = worldToScreen(origin);
            Vector2 b = worldToScreen(destination);
            Vector2 control = (a + b) * 0.5f + new Vector2(0f, -Mathf.Min(170f, Vector2.Distance(a, b) * 0.32f));
            DrawBezier(a, control, b, new Color(0.36f, 0.92f, 0.68f, 0.55f), 6f, 30);
            DrawBezier(a, control, b, new Color(1f, 0.82f, 0.22f, 0.88f), 2.5f, 30);
            for (int i = 0; i < 12; i++)
            {
                float t = Mathf.Clamp01(travelProgress - i * 0.022f);
                Vector2 p = Bezier(a, control, b, t);
                p += new Vector2(Mathf.Sin(animatedTime * 7f + i) * 4f, Mathf.Cos(animatedTime * 6f + i) * 3f);
                DrawCircle(p, 4.6f, actionKind == LabActionKind.Combat ? new Color(1f, 0.48f, 0.30f, 0.95f) : new Color(1f, 0.86f, 0.22f, 0.95f), 10);
            }
        }

        private void DrawFactionOverlay(Vector2 center, float size, TestHiveConfig hive, bool player)
        {
            Color color = FactionColor(hive, player);
            Vector2 marker = new Vector2(center.x + size * 0.56f, center.y - size * 0.50f);
            DrawCircle(marker, 11f, new Color(0.018f, 0.020f, 0.017f, 0.88f), 18);
            DrawCircle(marker, 7f, color, 18);
            DrawFrame(new Rect(marker.x - 12f, marker.y - 12f, 24f, 24f), new Color(1f, 0.96f, 0.72f, 0.68f), 1.1f);
        }

        private Rect HeaderRect()
        {
            if (Screen.width < 700 || Screen.height > Screen.width * 1.15f) return new Rect(8f, 294f, Mathf.Min(Screen.width - 16f, 330f), 46f);
            return new Rect(248f, 128f, 286f, 46f);
        }

        private Rect PanelRect()
        {
            Rect header = HeaderRect();
            if (Screen.width < 700 || Screen.height > Screen.width * 1.15f) return new Rect(8f, 346f, Mathf.Min(Screen.width - 16f, 420f), Mathf.Min(360f, Screen.height - 358f));
            return new Rect(header.x, header.yMax + 6f, 384f, Mathf.Min(430f, Screen.height - header.yMax - 22f));
        }

        private void EnsurePixel()
        {
            if (pixel != null) return;
            pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
        }

        private Color FactionColor(TestHiveConfig hive, bool player)
        {
            string faction = hive != null && hive.faction != null ? hive.faction.ToUpperInvariant() : string.Empty;
            if (faction.Contains("ENEMY")) return new Color(1f, 0.34f, 0.24f, 0.96f);
            if (faction.Contains("PLAYER")) return new Color(0.36f, 0.92f, 0.68f, 0.96f);
            if (faction.Contains("ALLY") || faction.Contains("ALLIE")) return new Color(0.28f, 0.72f, 1f, 0.96f);
            if (faction.Contains("NEUTRAL") || faction.Contains("NEUTRE")) return new Color(0.88f, 0.84f, 0.72f, 0.96f);
            return player ? new Color(0.36f, 0.92f, 0.68f, 0.96f) : new Color(1f, 0.48f, 0.30f, 0.96f);
        }

        private Color ClassColor(HiveClass hiveClass, bool player)
        {
            if (hiveClass == HiveClass.RoyalGuard) return new Color(0.28f, 0.72f, 1f, 0.96f);
            if (hiveClass == HiveClass.Striker) return new Color(1f, 0.42f, 0.25f, 0.96f);
            if (hiveClass == HiveClass.Nurturer) return new Color(0.36f, 0.92f, 0.68f, 0.96f);
            if (hiveClass == HiveClass.Scout) return new Color(0.92f, 0.82f, 0.28f, 0.96f);
            if (hiveClass == HiveClass.Alchemist) return new Color(0.74f, 0.46f, 1f, 0.96f);
            return player ? new Color(0.95f, 0.78f, 0.26f, 0.96f) : new Color(0.80f, 0.78f, 0.72f, 0.96f);
        }

        private static bool IsOnScreen(Vector2 point, float margin)
        {
            return point.x >= -margin && point.x <= Screen.width + margin && point.y >= -margin && point.y <= Screen.height + margin;
        }

        private string CoordLabel(Vector2 worldCoord)
        {
            return "X" + Mathf.RoundToInt(worldCoord.x).ToString(CultureInfo.InvariantCulture) + " Y" + Mathf.RoundToInt(worldCoord.y).ToString(CultureInfo.InvariantCulture);
        }

        private void DrawSolid(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, pixel);
            GUI.color = previous;
        }

        private void DrawFrame(Rect rect, Color color, float width)
        {
            DrawLine(new Vector2(rect.x, rect.y), new Vector2(rect.xMax, rect.y), color, width);
            DrawLine(new Vector2(rect.xMax, rect.y), new Vector2(rect.xMax, rect.yMax), color, width);
            DrawLine(new Vector2(rect.xMax, rect.yMax), new Vector2(rect.x, rect.yMax), color, width);
            DrawLine(new Vector2(rect.x, rect.yMax), new Vector2(rect.x, rect.y), color, width);
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color, float width)
        {
            Matrix4x4 matrix = GUI.matrix;
            Color previous = GUI.color;
            float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;
            float length = Vector2.Distance(start, end);
            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(new Rect(start.x, start.y - width * 0.5f, length, width), pixel);
            GUI.matrix = matrix;
            GUI.color = previous;
        }

        private void DrawCircle(Vector2 center, float radius, Color color, int segments)
        {
            Vector2 previous = center + new Vector2(radius, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                Vector2 next = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                DrawLine(previous, next, color, Mathf.Max(2f, radius * 0.12f));
                previous = next;
            }
        }

        private void DrawDiamond(Vector2 center, float radius, Color color, float width)
        {
            Vector2 top = center + new Vector2(0f, -radius);
            Vector2 right = center + new Vector2(radius * 0.82f, 0f);
            Vector2 bottom = center + new Vector2(0f, radius);
            Vector2 left = center + new Vector2(-radius * 0.82f, 0f);
            DrawLine(top, right, color, width);
            DrawLine(right, bottom, color, width);
            DrawLine(bottom, left, color, width);
            DrawLine(left, top, color, width);
        }

        private void DrawBezier(Vector2 a, Vector2 control, Vector2 b, Color color, float width, int segments)
        {
            Vector2 previous = a;
            for (int i = 1; i <= segments; i++)
            {
                Vector2 next = Bezier(a, control, b, i / (float)segments);
                DrawLine(previous, next, color, width);
                previous = next;
            }
        }

        private static Vector2 Bezier(Vector2 a, Vector2 control, Vector2 b, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * control + t * t * b;
        }

        private static GUIStyle LabelStyle(Color color, int size, FontStyle fontStyle, TextAnchor alignment)
        {
            return new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = color },
                fontSize = size,
                fontStyle = fontStyle,
                alignment = alignment,
                wordWrap = true,
                clipping = TextClipping.Clip
            };
        }

        private static GUIStyle ButtonStyle()
        {
            return new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
        }

        private static GUIStyle SelectedButtonStyle()
        {
            GUIStyle style = ButtonStyle();
            style.normal.textColor = new Color(0.36f, 0.92f, 0.68f, 1f);
            return style;
        }

        public readonly struct LabProofSnapshot
        {
            public readonly bool Ready;
            public readonly string PlayerPosition;
            public readonly string EnemyPosition;
            public readonly int PlayerLevel;
            public readonly int EnemyLevel;
            public readonly bool PlayerAlive;
            public readonly bool EnemyAlive;
            public readonly bool LocalOnly;
            public readonly bool PremiumHivesLoaded;
            public readonly string LastTelemetry;

            public LabProofSnapshot(bool ready, string playerPosition, string enemyPosition, int playerLevel, int enemyLevel, bool playerAlive, bool enemyAlive, bool localOnly, bool premiumHivesLoaded, string lastTelemetry)
            {
                Ready = ready;
                PlayerPosition = playerPosition;
                EnemyPosition = enemyPosition;
                PlayerLevel = playerLevel;
                EnemyLevel = enemyLevel;
                PlayerAlive = playerAlive;
                EnemyAlive = enemyAlive;
                LocalOnly = localOnly;
                PremiumHivesLoaded = premiumHivesLoaded;
                LastTelemetry = lastTelemetry;
            }
        }

        public readonly struct HiveVisualProofSnapshot
        {
            public readonly bool Pass;
            public readonly string PlayerSpritePath;
            public readonly string EnemySpritePath;
            public readonly string PlayerFaction;
            public readonly string EnemyFaction;
            public readonly bool NeutralLevel4Pass;
            public readonly bool AllLevel10ClassesPass;
            public readonly bool Level35Pass;
            public readonly bool PlayerEnemyDistinctPass;

            public HiveVisualProofSnapshot(bool pass, string playerSpritePath, string enemySpritePath, string playerFaction, string enemyFaction, bool neutralLevel4Pass, bool allLevel10ClassesPass, bool level35Pass, bool playerEnemyDistinctPass)
            {
                Pass = pass;
                PlayerSpritePath = playerSpritePath;
                EnemySpritePath = enemySpritePath;
                PlayerFaction = playerFaction;
                EnemyFaction = enemyFaction;
                NeutralLevel4Pass = neutralLevel4Pass;
                AllLevel10ClassesPass = allLevel10ClassesPass;
                Level35Pass = level35Pass;
                PlayerEnemyDistinctPass = playerEnemyDistinctPass;
            }
        }

        public readonly struct ScenarioLabProofSnapshot
        {
            public readonly bool Ready;
            public readonly string ScenarioId;
            public readonly bool ServerFalse;
            public readonly bool OfficialGainFalse;
            public readonly bool TestHivesEditable;

            public ScenarioLabProofSnapshot(bool ready, string scenarioId, bool serverFalse, bool officialGainFalse, bool testHivesEditable)
            {
                Ready = ready;
                ScenarioId = scenarioId;
                ServerFalse = serverFalse;
                OfficialGainFalse = officialGainFalse;
                TestHivesEditable = testHivesEditable;
            }
        }

        [Serializable]
        private sealed class LabState
        {
            public bool localOnly = true;
            public bool authorityServer;
            public bool officialGain;
            public bool collapsed;
            public int selectedScenarioIndex;
            public TestHiveConfig player;
            public TestHiveConfig enemy;
        }

        [Serializable]
        private sealed class TestHiveConfig
        {
            public string id;
            public string displayName;
            public string faction;
            public HiveClass hiveClass;
            public int level;
            public int soldiers;
            public int guards;
            public int scouts;
            public int workers;
            public int health;
            public int maxHealth;
            public Vector2 worldPosition;
            public LabResourceSet stock;
            public LabResourceSet capacity;
        }

        [Serializable]
        private sealed class LabResourceSet
        {
            public int nectar;
            public int pollen;
            public int water;
            public int wax;
            public int honey;
            public int royalJelly;
            public int propolis;
        }

        private sealed class LabTelemetryEntry
        {
            public readonly string type;
            public readonly string detail;
            public readonly float time;

            public LabTelemetryEntry(string type, string detail, float time)
            {
                this.type = type;
                this.detail = detail;
                this.time = time;
            }
        }

        private enum LabActionKind
        {
            None,
            Collection,
            Combat
        }

        private enum HiveClass
        {
            Neutral,
            RoyalGuard,
            Striker,
            Nurturer,
            Scout,
            Alchemist
        }
    }
}
