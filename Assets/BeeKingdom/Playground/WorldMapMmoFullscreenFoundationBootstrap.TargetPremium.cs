using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BeeKingdom.Networking;
using UnityEngine;

namespace BeeKingdom.Playground
{
    /// <summary>
    /// A unified, high-density and touch-safe action window, using the actual
    /// World Map textures and existing server-owned collection/combat flows.
    /// The panel never generates a resource credit or simulates a combat win.
    /// </summary>
    public sealed partial class WorldMapMmoFullscreenFoundationBootstrap
    {
        private readonly WorldTargetPanelSelection premiumTarget = new WorldTargetPanelSelection();
        private bool premiumTargetDetails;
        private float premiumTargetOpenedAt;
        private Texture2D premiumPanelTexture, premiumCardTexture;
        private Texture2D premiumGoldTexture, premiumGoldHoverTexture, premiumSecondaryTexture;
        private GUIStyle premiumPanelStyle, premiumCardStyle, premiumGoldStyle, premiumSecondaryStyle;
        private readonly Dictionary<string, GUIStyle> premiumTextStyles = new Dictionary<string, GUIStyle>();

        private static readonly Color TargetGold = new Color(1.00f, .76f, .34f, 1f);
        private static readonly Color TargetPaper = new Color(1.00f, .96f, .86f, 1f);
        private static readonly Color TargetMuted = new Color(.73f, .78f, .75f, 1f);
        private static readonly Color TargetGreen = new Color(.48f, .92f, .69f, 1f);
        private static readonly Color TargetRed = new Color(1.00f, .52f, .42f, 1f);

        private bool PremiumTargetVisible => premiumTarget.Visible;
        private void OpenPremiumTarget(WorldMapHitKind kind, string id)
        {
            if (kind == WorldMapHitKind.Resource)
                premiumTarget.Open(WorldTargetPanelMode.Resource, id);
            else if (kind == WorldMapHitKind.Bestiary)
                premiumTarget.Open(WorldTargetPanelMode.Enemy, id);
            else
                premiumTarget.Close();
            premiumTargetDetails = false;
            premiumTargetOpenedAt = Time.unscaledTime;
        }

        private void ClosePremiumTarget() => premiumTarget.Close();

        // Used by both UI clipping and map-touch exclusion. All coordinates
        // are top-left Unity IMGUI pixels converted from the platform safe area.
        private Rect PremiumTargetRect(bool forSquad = false) =>
            WorldTargetPanelGeometry.Calculate(Screen.width,Screen.height,
                Screen.safeArea,forSquad);
        private static Rect TargetRect(Rect panel, float s, float x, float y, float width, float height) =>
            new Rect(panel.x + x * s, panel.y + y * s, width * s, height * s);

        private static Texture2D TargetSurface(Color top, Color bottom, Color outline, int radius)
        {
            const int n = 96;
            Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.name = "BK/PremiumTarget/ProceduralNineSlice";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x - (n - 1) * .5f) - (n * .5f - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y - (n - 1) * .5f) - (n * .5f - radius), 0f);
                float inside = radius - Mathf.Sqrt(dx * dx + dy * dy);
                float opacity = Mathf.Clamp01(inside + .65f);
                Color tint = Color.Lerp(bottom, top, y / (n - 1f));
                if (inside < 2.2f) tint = Color.Lerp(tint, outline, .87f);
                tint.a *= opacity;
                pixels[y * n + x] = tint;
            }
            tex.SetPixels(pixels);
            tex.Apply(false, false);
            return tex;
        }

        private static GUIStyle TargetNineSlice(Texture2D tex, int radius = 18)
        {
            var style = new GUIStyle(GUI.skin.box);
            style.normal.background = tex;
            style.border = new RectOffset(radius, radius, radius, radius);
            style.padding = new RectOffset();
            style.margin = new RectOffset();
            return style;
        }

        private void EnsureTargetSkins()
        {
            if (premiumPanelStyle != null) return;
            premiumPanelTexture = TargetSurface(
                new Color(.115f, .110f, .090f, .99f),
                new Color(.035f, .045f, .041f, .99f),
                new Color(.80f, .54f, .23f, 1f), 19);
            premiumCardTexture = TargetSurface(
                new Color(.165f, .177f, .149f, .95f),
                new Color(.078f, .094f, .086f, .98f),
                new Color(.33f, .38f, .32f, 1f), 18);
            premiumGoldTexture = TargetSurface(
                new Color(1f, .86f, .49f, 1f),
                new Color(.77f, .43f, .14f, 1f),
                new Color(1f, .95f, .73f, 1f), 17);
            premiumGoldHoverTexture = TargetSurface(
                new Color(1f, .94f, .64f, 1f),
                new Color(.90f, .58f, .20f, 1f),
                new Color(1f, 1f, .83f, 1f), 17);
            premiumSecondaryTexture = TargetSurface(
                new Color(.20f, .27f, .24f, .98f),
                new Color(.10f, .13f, .12f, .98f),
                new Color(.48f, .56f, .47f, 1f), 17);
            premiumPanelStyle = TargetNineSlice(premiumPanelTexture, 19);
            premiumCardStyle = TargetNineSlice(premiumCardTexture);
            premiumGoldStyle = TargetNineSlice(premiumGoldTexture, 17);
            premiumGoldStyle.normal.background = premiumGoldTexture;
            premiumGoldStyle.hover.background = premiumGoldHoverTexture;
            premiumGoldStyle.active.background = premiumGoldHoverTexture;
            premiumGoldStyle.normal.textColor = new Color(.15f, .10f, .035f);
            premiumGoldStyle.hover.textColor = new Color(.12f, .08f, .025f);
            premiumGoldStyle.active.textColor = new Color(.12f, .08f, .025f);
            premiumGoldStyle.fontStyle = FontStyle.Bold;
            premiumGoldStyle.alignment = TextAnchor.MiddleCenter;
            premiumSecondaryStyle = TargetNineSlice(premiumSecondaryTexture);
            premiumSecondaryStyle.normal.textColor = TargetPaper;
            premiumSecondaryStyle.hover.textColor = Color.white;
            premiumSecondaryStyle.active.textColor = TargetGold;
            premiumSecondaryStyle.alignment = TextAnchor.MiddleCenter;
            premiumSecondaryStyle.fontStyle = FontStyle.Bold;
        }

        private GUIStyle TargetText(int size, Color color, FontStyle weight = FontStyle.Normal,
            TextAnchor alignment = TextAnchor.MiddleLeft, bool wrap = false)
        {
            string key = size + "/" + (int)weight + "/" + (int)alignment + "/" +
                ColorUtility.ToHtmlStringRGBA(color) + "/" + wrap;
            if (premiumTextStyles.TryGetValue(key, out GUIStyle cached)) return cached;
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(9, size),
                fontStyle = weight,
                alignment = alignment,
                wordWrap = wrap,
                clipping = TextClipping.Clip
            };
            style.normal.textColor = color;
            premiumTextStyles[key] = style;
            return style;
        }

        private void TargetLabel(Rect p, float s, float x, float y, float w, float h,
            string text, int font, Color color, bool strong = false,
            TextAnchor alignment = TextAnchor.MiddleLeft, bool wrap = false)
        {
            GUI.Label(TargetRect(p, s, x, y, w, h), text ?? string.Empty,
                TargetText(Mathf.RoundToInt(font * s), color,
                    strong ? FontStyle.Bold : FontStyle.Normal, alignment, wrap));
        }

        private bool TargetButton(Rect p, float s, float x, float y, float w, float h,
            string label, bool main = false, bool enabled = true)
        {
            EnsureTargetSkins();
            GUIStyle style = main ? premiumGoldStyle : premiumSecondaryStyle;
            style.fontSize = Mathf.Max(10, Mathf.RoundToInt(18f * s));
            bool previous = GUI.enabled;
            GUI.enabled = previous && enabled;
            bool clicked = GUI.Button(TargetRect(p,s,x,y,w,h), label ?? string.Empty, style);
            GUI.enabled = previous;
            return clicked && enabled;
        }

        private void TargetMetric(Rect p, float s, float x, float y, float w,
            string title, string value, Color valueColor)
        {
            GUI.Box(TargetRect(p,s,x,y,w,56f),GUIContent.none,premiumCardStyle);
            TargetLabel(p,s,x+12,y+4,w-24,18,title,12,TargetMuted);
            TargetLabel(p,s,x+12,y+23,w-24,26,value,19,valueColor,true);
        }

        private static string TargetDuration(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero) return "--";
            return duration.TotalMinutes < 1
                ? Mathf.CeilToInt((float)duration.TotalSeconds) + " s"
                : Mathf.CeilToInt((float)duration.TotalMinutes) + " min";
        }

        private string TargetReason(string key)
        {
            switch (key)
            {
                case "target.unavailable": return MapText(key, "Cible indisponible");
                case "target.reason.preview_only": return MapText(key, "Apercu local : aucun gain serveur.");
                case "target.reason.no_hive": return MapText(key, "Selectionnez d'abord votre ruche.");
                case "target.reason.server_offline": return MapText(key, "Donnees serveur non disponibles.");
                case "target.reason.server_busy": return MapText(key, "Action en cours de validation...");
                case "target.reason.other_flight": return MapText(key, "Une autre recolte est deja en cours.");
                case "target.reason.placement_missing": return MapText(key, "Position officielle indisponible.");
                case "target.reason.no_bees": return MapText(key, "Aucune abeille disponible pour la recolte.");
                case "target.reason.march_limit": return MapText(key, "Limite de marches atteinte.");
                case "target.reason.not_ready": return MapText(key, "Ressource temporairement indisponible.");
                case "target.reason.prepare_army": return MapText(key, "Preparez votre escouade a la caserne.");
                case "target.reason.combat_offline": return MapText(key, "Patrouille officielle indisponible.");
                case "target.reason.combat_preview": return MapText(key, "La patrouille verifie le combat avant le depart.");
                case "target.reason.ready_to_claim": return MapText(key, "Votre vol est pret a etre recupere.");
                case "target.reason.active_flight": return MapText(key, "Votre escouade est deja en mission.");
                case "target.reason.gather_ready": return MapText(key, "Choisissez les abeilles pour cette mission.");
                default: return MapText("target.reason.pending", "A verifier par le serveur.");
            }
        }

        private void DrawPremiumTargetPanel()
        {
            if (!PremiumTargetVisible || showCompositionPanel) return;
            EnsureTargetSkins();
            Rect panel = PremiumTargetRect();
            bool compact = Screen.width < 800 ||
                Screen.height > Screen.width * 1.10f;
            float designW = compact ? 420f : 600f;
            float designH = compact ? 600f : 520f;
            float s = panel.width / designW;

            Event input = Event.current;
            if (input != null && input.type == EventType.KeyDown &&
                input.keyCode == KeyCode.Escape)
            {
                ClosePremiumTarget();
                input.Use();
                return;
            }
            if (input != null && input.type == EventType.MouseDown &&
                !panel.Contains(input.mousePosition))
            {
                ClosePremiumTarget();
                input.Use();
                return;
            }
            WorldResourceNode resource = premiumTarget.Mode == WorldTargetPanelMode.Resource
                ? ResourceById(premiumTarget.TargetId) : null;
            WorldBestiaryNode enemy = premiumTarget.Mode == WorldTargetPanelMode.Enemy
                ? BestiaryById(premiumTarget.TargetId) : null;
            WorldHiveNode hive = SelectedHive();
            bool isEnemy = premiumTarget.Mode == WorldTargetPanelMode.Enemy;
            bool exists = isEnemy ? enemy != null : resource != null;
            Vector2 world = isEnemy
                ? (enemy != null ? enemy.WorldCoord : Vector2.zero)
                : (resource != null ? resource.WorldCoord : Vector2.zero);
            float distance = hive != null && exists
                ? Vector2.Distance(hive.WorldCoord,world) : -1f;
            string distanceText = distance < 0f ? "--" :
                Mathf.RoundToInt(distance).ToString(CultureInfo.InvariantCulture) + " " +
                MapText("target.units", "u");
            Color accent = isEnemy ? TargetRed : TargetGreen;
            string title = isEnemy
                ? (enemy != null ? LocalizedBestiaryName(enemy.Label) :
                    MapText("target.unavailable","Cible indisponible"))
                : (resource != null ? ResourceLabel(resource.Kind) :
                    MapText("target.unavailable","Cible indisponible"));
            Texture2D sprite = isEnemy
                ? (enemy != null ? RuntimeEntityTexture(BestiaryTexturePath(enemy)) : null)
                : (resource != null ? RuntimeEntityTexture(ResourceTexturePath(resource)) : null);

            WorldResourceCollectionScreenModel gatherModel = isEnemy ? null :
                HiveViewProductUiPresenter.OfficialWorldResourceCollectionModelForWorldMap();
            RemoteWorldResourceNode node = !isEnemy && resource != null
                ? OfficialNodeState(resource) : null;
            bool official = !isEnemy && resource != null && IsOfficialResource(resource);
            bool activeHere = !isEnemy && gatherModel?.Active != null &&
                string.Equals(gatherModel.Active.NodeId,premiumTarget.TargetId,StringComparison.Ordinal);
            bool readyToClaim = activeHere && DateTimeOffset.UtcNow >= gatherModel.Active.EndsAtUtc;
            bool rosterAvailable = gatherModel?.AvailableRoster != null &&
                gatherModel.AvailableRoster.Values.Any(count => count > 0);
            WorldTargetActionDecision decision;
            CombatPatrolScreenModel patrolModel = isEnemy
                ? HiveViewProductUiPresenter.PeekCombatPatrolModelForWorldMap() : null;
            bool hasSquad = isEnemy &&
                (MobileAccountSessionRuntimeBootstrap.SquadReservationControllerForHiveMap
                    ?.Model?.HasReservation ?? false);
            if (isEnemy)
                decision = WorldTargetPanelPolicy.Enemy(exists,hasSquad,
                    patrolModel != null && patrolModel.State != CombatPatrolScreenState.NotConfigured,
                    patrolModel?.State == CombatPatrolScreenState.Mutating,
                    patrolModel?.HasFreeSlot ?? false);
            else
                decision = WorldTargetPanelPolicy.Resource(
                    exists,hive != null,official,
                    gatherModel != null && gatherModel.State != WorldResourceCollectionScreenState.NotConfigured,
                    gatherModel?.State == WorldResourceCollectionScreenState.Mutating,
                    readyToClaim,activeHere && !readyToClaim,
                    gatherModel?.Active != null && !activeHere,
                    node != null,!requireOfficialResourcePlacements || officialPlacementReady,
                    rosterAvailable,WorldMapMarchRegistry.CanLaunchNewMarch(),
                    official ? (node?.CanLaunch ?? false) :
                        (hive != null && resource != null &&
                         CanCollectOrLaunch(hive,resource)));

            DrawSolid(new Rect(0f,0f,Screen.width,Screen.height),
                new Color(.012f,.018f,.018f,.65f));
            GUI.Box(new Rect(panel.x+4f*s,panel.y+7f*s,panel.width,panel.height),
                GUIContent.none,premiumCardStyle);
            GUI.Box(panel,GUIContent.none,premiumPanelStyle);
            DrawSolid(TargetRect(panel,s,0,0,8,designH),accent);
            DrawSolid(TargetRect(panel,s,21,76,designW-42,1.2f),
                new Color(1f,.77f,.40f,.48f));

            TargetLabel(panel,s,22,12,designW-110,19,
                isEnemy ? MapText("target.header.enemy","CIBLE HOSTILE") :
                          MapText("target.header.resource","SITE DE RECOLTE"),
                12,accent,true);
            TargetLabel(panel,s,22,33,designW-100,37,title,25,TargetPaper,true);
            if (TargetButton(panel,s,designW-59,15,39,38,"X"))
            {
                ClosePremiumTarget();
                return;
            }

            float artW = compact ? 140f : 184f;
            GUI.Box(TargetRect(panel,s,20,90,artW,compact ? 154f : 174f),
                GUIContent.none,premiumCardStyle);
            if(sprite != null)
                GUI.DrawTexture(TargetRect(panel,s,30,99,artW-20f,compact?134f:154f),
                    sprite,ScaleMode.ScaleToFit,true);
            else
            {
                Vector2 center = TargetRect(panel,s,20,90,artW,155).center;
                DrawDiamond(center,44f*s,accent,2.8f*s);
                TargetLabel(panel,s,32,172,artW-24,26,
                    isEnemy ? "T"+(enemy?.Tier ?? 0) :
                    MapText("target.no_art","SITE"),
                    16,TargetPaper,true,TextAnchor.MiddleCenter);
            }

            float infoX = compact ? 173f : 223f;
            float infoW = designW-infoX-22f;
            TargetLabel(panel,s,infoX,92,infoW,23,
                isEnemy ? MapText("target.enemy.class","BESTIAIRE") :
                    (official ? MapText("target.resource.official","RECOLTE OFFICIELLE") :
                     MapText("target.resource.preview","SITE EN APERCU")),
                13,accent,true);
            string summary = isEnemy && enemy != null
                ? "T"+enemy.Tier.ToString(CultureInfo.InvariantCulture)+"  /  "+
                  LocalizedBestiaryRole(enemy.Role)
                : official && node != null
                    ? (node.Tier ?? MapText("target.resource.site","SITE"))+"  /  "+
                      (node.ResourceKey ?? string.Empty)
                    : resource != null ? resource.Label : "--";
            // Preserve the existing rare-sighting and daily-focus rewards
            // without inventing a loot probability or a combat outcome.
            if (isEnemy && enemy != null && IsRareSighting(enemy,out _))
                summary += "  /  " + MapText("target.bonus.rare","Rare +25 %");
            if (!isEnemy && node != null && node.IsDailyFocus)
                summary += "  /  " + MapText("target.bonus.daily","Cible du jour +50 %");
            TargetLabel(panel,s,infoX,116,infoW,43,summary,16,TargetPaper,true,
                TextAnchor.UpperLeft,true);
            string stateLine;
            if (isEnemy)
                stateLine = hasSquad ? MapText("target.squad.ready","Escouade preparee") :
                    MapText("target.squad.missing","Escouade a preparer");
            else if (!official)
                stateLine = MapText("target.resource.local","Aucun gain officiel");
            else if (activeHere)
                stateLine = readyToClaim ?
                    MapText("target.resource.claimable","Recolte terminee") :
                    MapText("target.resource.active","En cours de recolte");
            else if (node == null)
                stateLine = MapText("target.reason.server_offline","Donnees serveur indisponibles");
            else
                stateLine = node.Ready ?
                    MapText("target.resource.ready","Disponible") :
                    MapText("target.resource.cooldown","En recuperation");
            TargetLabel(panel,s,infoX,162,infoW,31,stateLine,15,
                decision.Enabled ? TargetGreen : TargetGold,true,TextAnchor.MiddleLeft,true);
            TargetLabel(panel,s,infoX,200,infoW,34,
                MapText("target.coords","POSITION") + "  " +
                (exists ? CoordLabel(world) : "--"),
                12,TargetMuted,false,TextAnchor.UpperLeft,true);

            float metricY = compact ? 258f : 274f;
            float gap = 9f;
            float metricW = (designW-40f-gap)*.5f;
            string value1, value2, value3, value4, metric1, metric2, metric3, metric4;
            Color color1 = TargetPaper, color2 = TargetPaper, color3 = TargetPaper, color4 = TargetPaper;
            if (isEnemy)
            {
                var preview = patrolModel?.Preview;
                bool validPreview = enemy != null && patrolModel?.SelectedTier == enemy.Tier &&
                    preview != null && preview.Tier == enemy.Tier;
                metric1=MapText("target.metric.threat","MENACE");
                value1=enemy == null ? "--" : "T"+enemy.Tier.ToString(CultureInfo.InvariantCulture);
                color1=TargetRed;
                metric2=MapText("target.metric.distance","DISTANCE RUCHE");
                value2=distanceText;
                metric3=MapText("target.metric.squad","ESCOUADE");
                value3=hasSquad ? MapText("target.value.ready","PRETE") :
                    MapText("target.value.to_prepare","A PREPARER");
                color3=hasSquad?TargetGreen:TargetGold;
                metric4=MapText("target.metric.power","FORCE REQUISE");
                value4=validPreview && preview.RequiredPower>0 ?
                    preview.RequiredPower.ToString("N0",CultureInfo.CurrentCulture) : "--";
            }
            else
            {
                metric1=MapText("target.metric.amount","STOCK RESTANT");
                value1=node != null && node.Remaining >= 0 ?
                    node.Remaining.ToString("N0",CultureInfo.CurrentCulture) :
                    !official && resource != null ?
                        ResourceRemaining(resource).ToString("N0",CultureInfo.CurrentCulture) : "--";
                color1=node?.Ready==true?TargetGreen:TargetGold;
                metric2=MapText("target.metric.yield","GAIN PREVU");
                value2=node != null ? node.Yield.ToString("N0",CultureInfo.CurrentCulture) : "--";
                metric3=activeHere ?
                    MapText("target.metric.remaining","TEMPS RESTANT") :
                    !official ? MapText("target.metric.demo","DUREE DEMO") :
                    MapText("target.metric.duration","DUREE");
                value3=activeHere && gatherModel?.Active != null ?
                    TargetDuration(gatherModel.Active.EndsAtUtc-DateTimeOffset.UtcNow) :
                    node != null ? TargetDuration(node.Duration) :
                    resource != null && !official ?
                        TargetDuration(TimeSpan.FromSeconds(
                            LocalCollectionFlyingDuration+
                            LocalCollectionGatheringDuration+
                            LocalCollectionReturningDuration)) : "--";
                metric4=MapText("target.metric.distance","DISTANCE RUCHE");
                value4=distanceText;
            }
            TargetMetric(panel,s,20,metricY,metricW,metric1,value1,color1);
            TargetMetric(panel,s,29+metricW,metricY,metricW,metric2,value2,color2);
            TargetMetric(panel,s,20,metricY+65,metricW,metric3,value3,color3);
            TargetMetric(panel,s,29+metricW,metricY+65,metricW,metric4,value4,color4);

            float hintY = compact ? 395f : 401f;
            float hintH = compact ? 88f : 50f;
            GUI.Box(TargetRect(panel,s,20,hintY,designW-40,hintH),
                GUIContent.none,premiumCardStyle);
            DrawSolid(TargetRect(panel,s,20,hintY,3f,hintH),accent);
            string hint = TargetReason(decision.ReasonKey);
            if (premiumTargetDetails)
            {
                if (isEnemy)
                    hint = MapText("target.details.enemy",
                        "Combat confirme par le serveur. Le butin et le resultat ne sont jamais garantis.");
                else
                    hint = official ? MapText("target.details.resource",
                        "La duree et le gain prevu viennent du serveur. L'escouade valide la mission.") :
                        MapText("target.details.preview",
                        "Cette ressource est une demonstration sans gain persistant.");
            }
            TargetLabel(panel,s,32,hintY+5,designW-62,hintH-10,
                hint,compact?15:13,TargetPaper,false,TextAnchor.MiddleLeft,true);

            float primaryY = compact ? 494f : 458f;
            float primaryH = compact ? 47f : 46f;
            string primary = PrimaryTargetActionText(decision,isEnemy);
            if (TargetButton(panel,s,20,primaryY,compact?designW-40:315,
                primaryH,primary,true,decision.Enabled))
            {
                ExecutePremiumTargetAction(decision.Action,enemy,resource,hive);
                return;
            }
            if (compact)
            {
                if (TargetButton(panel,s,20,553,184,36,
                    MapText("target.action.details","Details")))
                    premiumTargetDetails=!premiumTargetDetails;
                if (TargetButton(panel,s,215,553,designW-235,36,
                    MapText("target.action.center","Centrer"),false,exists))
                    FocusPremiumTarget(world);
            }
            else
            {
                if (TargetButton(panel,s,347,primaryY,110,primaryH,
                    MapText("target.action.details", "Details")))
                    premiumTargetDetails=!premiumTargetDetails;
                if (TargetButton(panel,s,467,primaryY,113,primaryH,
                    MapText("target.action.center","Centrer"),false,exists))
                    FocusPremiumTarget(world);
            }
        }

        private string PrimaryTargetActionText(WorldTargetActionDecision d, bool enemy)
        {
            switch (d.Action)
            {
                case WorldTargetPrimaryAction.PrepareArmy:
                    return MapText("target.action.prepare_army","Preparer l'armee");
                case WorldTargetPrimaryAction.PlanAttack:
                    return MapText("target.action.plan_attack","Planifier l'attaque");
                case WorldTargetPrimaryAction.PrepareGather:
                    return MapText("target.action.gather","Envoyer les abeilles");
                case WorldTargetPrimaryAction.PreviewGather:
                    return MapText("target.action.preview","Vol d'observation (demo)");
                case WorldTargetPrimaryAction.RecallGatherers:
                    return MapText("target.action.recall","Rappeler");
                case WorldTargetPrimaryAction.ClaimGathering:
                    return MapText("target.action.claim","Recuperer la recolte");
                default:
                    return MapText("target.action.unavailable","Indisponible");
            }
        }

        private void ExecutePremiumTargetAction(WorldTargetPrimaryAction action,
            WorldBestiaryNode enemy, WorldResourceNode resource, WorldHiveNode hive)
        {
            switch (action)
            {
                case WorldTargetPrimaryAction.PrepareArmy:
                    ClosePremiumTarget();
                    HiveViewProductUiPresenter.OpenBarrackArmyTabForExternalHost();
                    break;
                case WorldTargetPrimaryAction.PlanAttack:
                    if(enemy == null) return;
                    pendingCombatPatrolLaunchTarget = enemy.WorldCoord;
                    ClosePremiumTarget();
                    HiveViewProductUiPresenter.OpenCombatPatrolOverlayForWorldMap(enemy.Tier);
                    break;
                case WorldTargetPrimaryAction.PrepareGather:
                    if(resource != null && hive != null && IsOfficialResource(resource) &&
                       CanCollectOrLaunch(hive,resource))
                        TryCollectOrLaunch(hive,resource);
                    break;
                case WorldTargetPrimaryAction.PreviewGather:
                    if (resource != null && hive != null &&
                        !IsOfficialResource(resource) &&
                        CanCollectOrLaunch(hive,resource))
                        TryCollectOrLaunch(hive,resource);
                    break;
                case WorldTargetPrimaryAction.RecallGatherers:
                    if(resource != null && IsOfficialResource(resource))
                        HiveViewProductUiPresenter.RecallOfficialWorldResourceCollectionForWorldMap();
                    break;
                case WorldTargetPrimaryAction.ClaimGathering:
                    if(resource != null && IsOfficialResource(resource))
                    {
                        HiveViewProductUiPresenter.ClaimOfficialWorldResourceCollectionForWorldMap();
                        HiveViewProductUiPresenter.DismissWorldResourceCollectionDebriefForWorldMap();
                        HiveViewProductUiPresenter.RefreshOfficialWorldResourceCollectionForWorldMap();
                    }
                    break;
            }
        }

        private void FocusPremiumTarget(Vector2 position)
        {
            targetWorldCenter = position;
            ClosePremiumTarget();
        }

        private void DrawPremiumCompositionPanel()
        {
            WorldResourceNode resource = ResourceById(compositionTargetNodeId);
            WorldResourceCollectionScreenModel model =
                HiveViewProductUiPresenter.OfficialWorldResourceCollectionModelForWorldMap();
            if (resource == null || model == null || !IsOfficialResource(resource))
            {
                collectionVisualState=CollectionVisualState.Idle;
                showCompositionPanel=false;
                return;
            }
            EnsureTargetSkins();
            Rect panel=PremiumTargetRect(true);
            bool compact=WorldTargetPanelGeometry.Compact(Screen.width,Screen.height,Screen.safeArea);
            float w=compact?420f:600f;
            float h=compact?610f:525f;
            float s=panel.width/w;
            DrawSolid(new Rect(0,0,Screen.width,Screen.height),
                new Color(.012f,.018f,.018f,.77f));
            GUI.Box(panel,GUIContent.none,premiumPanelStyle);
            DrawSolid(TargetRect(panel,s,0,0,7,h),TargetGreen);
            TargetLabel(panel,s,22,12,w-90,21,
                MapText("target.squad.eyebrow","MISSION DE RECOLTE"),
                13,TargetGreen,true);
            TargetLabel(panel,s,22,38,w-90,38,
                MapText("target.squad.title","Composer l'escouade"),
                24,TargetPaper,true);
            if(TargetButton(panel,s,w-61,14,40,38,"X"))
            { CancelPremiumComposition();return; }
            GUI.Box(TargetRect(panel,s,20,89,w-40,68),GUIContent.none,premiumCardStyle);
            TargetLabel(panel,s,33,95,w-66,26,
                ResourceLabel(resource.Kind)+"   "+resource.Label,18,TargetPaper,true);
            TargetLabel(panel,s,33,125,w-66,22,
                MapText("target.squad.subtitle","Selectionnez les abeilles disponibles."),
                13,TargetMuted);
            float startY=compact?171f:169f;
            DrawPremiumSquadRow(panel,s,w,startY,"guardians",
                MapText("target.squad.guards","Gardiennes"),model.DraftGuardians,model.AvailableRoster);
            DrawPremiumSquadRow(panel,s,w,startY+68f,"wingrunners",
                MapText("target.squad.wings","Voltigeuses"),model.DraftWingrunners,model.AvailableRoster);
            DrawPremiumSquadRow(panel,s,w,startY+136f,"darters",
                MapText("target.squad.darts","Lanceuses"),model.DraftDarters,model.AvailableRoster);

            float totalY=compact?386f:380f;
            TargetLabel(panel,s,27,totalY,w-54,33,
                MapText("target.squad.total","Total")+" : "+
                model.DraftTotal.ToString(CultureInfo.CurrentCulture),
                18,TargetGold,true);
            IReadOnlyList<string> champs=HiveViewProductUiPresenter.PeekOwnedChampionBeeIdsForWorldMap();
            champs=champs??Array.Empty<string>();
            int choices=champs.Count+1;
            compositionChampionIndex=Mathf.Clamp(compositionChampionIndex,0,choices-1);
            string champion=MapText("target.squad.none","Aucune");
            if(compositionChampionIndex>0 && champs.Count>0)
            {
                string championId=champs[compositionChampionIndex-1];
                champion=ChampionBeeCatalog.TryResolve(championId,out ChampionBeeDefinition def)
                    ? def.FallbackName : championId;
            }
            float champY=totalY+37f;
            TargetLabel(panel,s,27,champY,130,32,
                MapText("target.squad.champion","Championne"),14,TargetMuted);
            TargetLabel(panel,s,157,champY,w-267,32,champion,14,TargetPaper,true);
            if(TargetButton(panel,s,w-101,champY,35,34,"<"))
                compositionChampionIndex=(compositionChampionIndex-1+choices)%choices;
            if(TargetButton(panel,s,w-59,champY,35,34,">"))
                compositionChampionIndex=(compositionChampionIndex+1)%choices;

            bool canLaunch=model.CanLaunch(compositionTargetNodeId) &&
                WorldMapMarchRegistry.CanLaunchNewMarch() &&
                (!requireOfficialResourcePlacements || officialPlacementReady);
            float buttonsY=compact?502f:466f;
            if(TargetButton(panel,s,22,buttonsY,w-160,48,
                MapText("target.squad.send","Envoyer l'escouade"),true,canLaunch))
                ConfirmCollectionLaunch();
            if(TargetButton(panel,s,w-127,buttonsY,104,48,
                MapText("target.action.cancel","Annuler")))
                CancelPremiumComposition();
            string note=model.ErrorCode=="max_marches_reached" ||
                !WorldMapMarchRegistry.CanLaunchNewMarch() ?
                MapText("target.reason.march_limit","Limite de marches atteinte.") :
                !canLaunch ? MapText("target.squad.select","Choisissez une escouade admissible.") :
                MapText("target.squad.confirm","Le serveur confirme les troupes et le rendement.");
            if(compact)
                TargetLabel(panel,s,24,558,w-48,35,note,12,TargetMuted,false,
                    TextAnchor.MiddleCenter,true);
        }

        private void DrawPremiumSquadRow(Rect panel, float s, float w, float y,
            string family, string name, int selected, IReadOnlyDictionary<string,long> roster)
        {
            long available=0;
            if(roster!=null)roster.TryGetValue(family,out available);
            GUI.Box(TargetRect(panel,s,20,y,w-40,60f),GUIContent.none,premiumCardStyle);
            TargetLabel(panel,s,31,y+6,w-158,24,name,15,TargetPaper,true);
            TargetLabel(panel,s,31,y+30,w-158,20,
                selected.ToString(CultureInfo.CurrentCulture)+" / "+
                available.ToString("N0",CultureInfo.CurrentCulture),12,TargetMuted);
            if(TargetButton(panel,s,w-135,y+12,45,40,"-",
                false,selected>0))
                HiveViewProductUiPresenter.AdjustOfficialWorldResourceCollectionDraftForWorldMap(family,-1);
            if(TargetButton(panel,s,w-80,y+12,45,40,"+",
                false,selected<available))
                HiveViewProductUiPresenter.AdjustOfficialWorldResourceCollectionDraftForWorldMap(family,1);
        }

        private void CancelPremiumComposition()
        {
            showCompositionPanel=false;
            collectionVisualState=CollectionVisualState.Idle;
            compositionTargetNodeId=null;
        }

        private void DisposePremiumTargetSkins()
        {
            foreach(Texture2D texture in new[]{
                premiumPanelTexture,premiumCardTexture,premiumGoldTexture,
                premiumGoldHoverTexture,premiumSecondaryTexture})
                if(texture!=null) Destroy(texture);
            premiumPanelTexture=premiumCardTexture=premiumGoldTexture=
                premiumGoldHoverTexture=premiumSecondaryTexture=null;
            premiumPanelStyle=premiumCardStyle=premiumGoldStyle=premiumSecondaryStyle=null;
            premiumTextStyles.Clear();
        }
    }
}
