using System;
using System.Linq;
using BeeKingdom.Localization;
using BeeKingdom.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    /// <summary>
    /// P5 opt-in overlay on the EXISTING world map. It does not modify the
    /// terrain, scene assets, tile provider, local flowers or military units.
    /// Floral site coordinates are not available in the current SQL catalogue,
    /// so no fictitious map markers are drawn. Reads only official server DTOs.
    /// </summary>
    public sealed class PollinationWorldMapOverlay : MonoBehaviour
    {
        public static bool IsModalOpen { get; private set; }
        private static PollinationWorldMapOverlay current;

        public static bool TryOpenForSite(string siteId)
        {
            if (current == null || string.IsNullOrEmpty(siteId) ||
                !MobileAccountSessionRuntimeBootstrap.PollinationFeatureEnabledForWorldMap)
                return false;
            current.OpenPanel(siteId);
            return true;
        }
        public static Rect EntryButtonRect() =>
            new Rect(Mathf.Max(12f, Screen.width * 0.5f - 108f), 10f, 216f, 36f);

        public static bool IsEntryHotspot(Vector2 guiPoint) =>
            !IsModalOpen &&
            MobileAccountSessionRuntimeBootstrap.PollinationFeatureEnabledForWorldMap &&
            EntryButtonRect().Contains(guiPoint);

        private Texture2D solid;
        private bool opened;
        private bool firstReadRequested;
        private IPollinationPanelController boundController;
        private string requestedSiteId = string.Empty;
        private int tab;
        private Vector2 scroll;
        private string selectedSiteId = string.Empty;

        private static readonly Color Background = new Color(0.025f,0.026f,0.025f,0.985f);
        private static readonly Color Gold = new Color(0.91f,0.69f,0.30f,1f);
        private static readonly Color GoldMuted = new Color(0.41f,0.29f,0.13f,1f);
        private static readonly Color Parchment = new Color(0.96f,0.90f,0.75f,1f);
        private static readonly Color Slate = new Color(0.14f,0.15f,0.14f,1f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartOnInitialScene() => CreateIfMap(SceneManager.GetActiveScene());

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) =>
            CreateIfMap(scene);

        private static void CreateIfMap(Scene scene)
        {
            if (!Application.isPlaying || !scene.IsValid() || !scene.isLoaded ||
                !scene.name.StartsWith("WorldMap", StringComparison.Ordinal) ||
                FindAnyObjectByType<PollinationWorldMapOverlay>() != null)
                return;
            var go = new GameObject("BeeKingdom Pollination Opt-In Overlay");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<PollinationWorldMapOverlay>();
        }

        private void OpenPanel(string siteId)
        {
            opened = true;
            IsModalOpen = true;
            tab = 0;
            scroll = Vector2.zero;
            requestedSiteId = siteId ?? string.Empty;
            boundController = MobileAccountSessionRuntimeBootstrap.PollinationControllerForWorldMap;
            firstReadRequested = true;
            MobileAccountSessionRuntimeBootstrap.AllianceCenterControllerForHiveMap
                .RefreshQuietly();
            MobileAccountSessionRuntimeBootstrap.PollinationControllerForWorldMap.Refresh();
        }

        private void Awake()
        {
            current = this;
            solid = new Texture2D(1,1,TextureFormat.RGBA32,false);
            solid.SetPixel(0,0,Color.white);
            solid.Apply();
        }

        private void OnDestroy()
        {
            opened = false;
            IsModalOpen = false;
            if (ReferenceEquals(current, this)) current = null;
            if (solid != null) Destroy(solid);
        }

        private void Update()
        {
            IPollinationPanelController controller =
                MobileAccountSessionRuntimeBootstrap.PollinationControllerForWorldMap;
            if (!ReferenceEquals(boundController, controller))
            {
                boundController = controller;
                firstReadRequested = false;
                requestedSiteId = string.Empty;
                if (opened) Close();
            }
            if (MobileAccountSessionRuntimeBootstrap.PollinationFeatureEnabledForWorldMap
                && controller.IsConfigured && !firstReadRequested)
            {
                firstReadRequested = true;
                controller.Refresh();
            }
            if (!string.IsNullOrEmpty(requestedSiteId)
                && controller.Model?.Sites != null
                && controller.Model.Sites.Any(site => site.SiteId == requestedSiteId)
                && !controller.IsBusy)
            {
                controller.SelectSite(requestedSiteId);
                requestedSiteId = string.Empty;
            }
            if (!opened) return;
            if (!MobileAccountSessionRuntimeBootstrap.PollinationFeatureEnabledForWorldMap ||
                Input.GetKeyDown(KeyCode.Escape))
                Close();
        }

        private void Close()
        {
            opened = false;
            IsModalOpen = false;
            requestedSiteId = string.Empty;
        }

        private static string T(string fr, string en) =>
            string.Equals(BeeLocalization.CurrentLocale,"en-US",
                StringComparison.OrdinalIgnoreCase) ? en : fr;

        private void RectFill(Rect r, Color c)
        {
            if (solid == null) return;
            Color previous = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r,solid);
            GUI.color = previous;
        }

        private void Border(Rect r, Color c, float size = 1f)
        {
            RectFill(new Rect(r.x,r.y,r.width,size),c);
            RectFill(new Rect(r.x,r.yMax-size,r.width,size),c);
            RectFill(new Rect(r.x,r.y,size,r.height),c);
            RectFill(new Rect(r.xMax-size,r.y,size,r.height),c);
        }

        private GUIStyle TextStyle(int size, Color color, FontStyle weight,
            TextAnchor align = TextAnchor.MiddleLeft)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = size, normal = {textColor = color},
                fontStyle = weight, alignment = align, wordWrap = true
            };
        }

        private bool Button(Rect rect,string text,bool enabled=true,bool prominent=false)
        {
            bool oldEnabled = GUI.enabled;
            Color bg = GUI.backgroundColor, fg = GUI.contentColor;
            GUI.enabled = oldEnabled && enabled;
            GUI.backgroundColor = prominent ? Gold : new Color(0.47f,0.39f,0.24f,1f);
            GUI.contentColor = prominent ? Color.black : Parchment;
            bool clicked = GUI.Button(rect,text);
            GUI.enabled = oldEnabled;
            GUI.backgroundColor = bg;
            GUI.contentColor = fg;
            return clicked;
        }

        private void Line(Rect rect,string text,int size=14, bool bold=false)
        {
            GUI.Label(rect,text,TextStyle(size,Parchment,
                bold?FontStyle.Bold:FontStyle.Normal));
        }

        private void Bar(Rect rect,long value,long max,Color color)
        {
            RectFill(rect,new Color(0.24f,0.23f,0.19f,1f));
            float t = max > 0 ? Mathf.Clamp01((float)((double)value/max)) : 0f;
            RectFill(new Rect(rect.x,rect.y,rect.width*t,rect.height),color);
            Border(rect,new Color(0.60f,0.49f,0.28f,0.48f));
        }

        private void OnGUI()
        {
            if (!MobileAccountSessionRuntimeBootstrap.PollinationFeatureEnabledForWorldMap)
            {
                if (opened) Close();
                return;
            }
            GUI.depth = -2000;
            if (!opened)
            {
                Rect entry = EntryButtonRect();
                if (Button(entry,T("POLLINISATION","POLLINATION"),
                    true,true))
                {
                    OpenPanel(string.Empty);
                }
                return;
            }

            IPollinationPanelController controller =
                MobileAccountSessionRuntimeBootstrap.PollinationControllerForWorldMap;
            if (!controller.IsConfigured)
            {
                Close();
                return;
            }

            AllianceCenterScreenModel alliance =
                MobileAccountSessionRuntimeBootstrap.AllianceCenterControllerForHiveMap.Model;
            Guid allianceId = alliance?.Overview?.AllianceId ?? Guid.Empty;
            bool canManage = alliance?.Overview?.IsOfficerOrLeader == true;
            controller.ConfigureAlliance(allianceId,canManage);
            PollinationScreenModel model = controller.Model;

            // Full viewport glass layer, with input captured before base map
            // input handlers. The existing map's Update input is also blocked.
            RectFill(new Rect(0,0,Screen.width,Screen.height),
                new Color(0.006f,0.008f,0.012f,0.90f));
            float margin = Screen.width < 650f ? 8f : 22f;
            Rect panel = new Rect(margin,margin,
                Screen.width-2*margin,Screen.height-2*margin);
            RectFill(panel,Background);
            Border(panel,Gold,2f);

            float x=panel.x+Mathf.Max(14f,panel.width*0.028f);
            float usable=panel.width-2*(x-panel.x);
            GUI.Label(new Rect(x,panel.y+16f,usable-68f,40f),
                T("POLLINISATION","POLLINATION"),
                TextStyle(Mathf.Clamp(Screen.width/45,19,31),Gold,FontStyle.Bold));
            if (Button(new Rect(panel.xMax-54f,panel.y+16f,36f,34f),"×")) Close();
            GUI.Label(new Rect(x,panel.y+54f,usable,26f),
                T("L’écosystème de votre royaume","The ecology of your kingdom"),
                TextStyle(14,Parchment,FontStyle.Normal));
            RectFill(new Rect(x,panel.y+89f,usable,1f),GoldMuted);

            float buttonGap=6f;
            float buttonWidth=(usable-2*buttonGap)/3f;
            string[] labels={
                T("CHAMPS","FIELDS"),
                T("ESSAIMS","SWARMS"),
                T("ALLIANCE","ALLIANCE")
            };
            for(int i=0;i<3;i++)
            {
                if (Button(new Rect(x+i*(buttonWidth+buttonGap),
                    panel.y+104f,buttonWidth,38f),labels[i],true,tab==i))
                {
                    tab=i;
                    scroll=Vector2.zero;
                }
            }

            Rect area=new Rect(x,panel.y+156f,usable,
                Mathf.Max(120f,panel.height-173f));
            RectFill(area,new Color(0.075f,0.073f,0.065f,0.70f));
            Border(area,new Color(0.35f,0.29f,0.19f,0.60f));
            GUILayout.BeginArea(new Rect(area.x+10f,area.y+10f,
                area.width-20f,area.height-20f));
            scroll=GUILayout.BeginScrollView(scroll,GUIStyle.none,
                GUI.skin.verticalScrollbar);
            if (model.State == PollinationScreenState.NotConfigured ||
                model.State == PollinationScreenState.Unavailable ||
                (model.State == PollinationScreenState.Loading &&
                 model.Population == null))
            {
                GUILayout.Space(18f);
                GUILayout.Label(T("La pollinisation est en préparation.",
                    "Pollination is being prepared."),
                    TextStyle(17,Parchment,FontStyle.Normal));
            }
            else if (tab==0) DrawSites(controller,model);
            else if (tab==1) DrawSwarm(controller,model);
            else DrawAlliance(controller,model);
            GUILayout.Space(32f);
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // Do not allow clicking HUD, player hive or resource nodes through
            // the modal when the pointer falls on the glass surrounding it.
            Event e=Event.current;
            if (e != null && (e.type==EventType.MouseDown ||
                e.type==EventType.MouseUp ||
                e.type==EventType.ScrollWheel) &&
                !panel.Contains(e.mousePosition)) e.Use();
        }

        private void DrawSites(IPollinationPanelController controller,
            PollinationScreenModel model)
        {
            Section(T("Champs floraux partagés","Shared floral fields"));
            if (model.Sites==null || model.Sites.Count==0)
            {
                Item(T("Aucun site ouvert pour le moment.",
                    "No fields available yet."));
                return;
            }
            foreach(RemoteFloralSite site in model.Sites)
            {
                bool selected = site.SiteId==model.SelectedSiteId;
                if (GUILayout.Button(
                    (selected?"◆  ":"◇  ")+site.LocalizedName(BeeLocalization.CurrentLocale)
                    + "   •   "+site.PollenRemaining+" / "+site.PollenCapacity,
                    GUILayout.Height(39f)))
                {
                    controller.SelectSite(site.SiteId);
                    selectedSiteId=site.SiteId;
                }
            }
            RemoteFloralSite selectedSite=model.SelectedSite;
            if (selectedSite==null)return;
            GUILayout.Space(14f);
            Section(selectedSite.LocalizedName(BeeLocalization.CurrentLocale));
            Item(T("Réserve de pollen","Pollen reserves")+" : "+
                selectedSite.PollenRemaining+" / "+selectedSite.PollenCapacity);
            DrawMeter(selectedSite.PollenRemaining,selectedSite.PollenCapacity,
                new Color(0.92f,0.65f,0.26f));
            Item(T("Fertilité","Fertility")+" : "+selectedSite.FertilityBp/100f+" %");
            DrawMeter(selectedSite.FertilityBp,10000,new Color(0.36f,0.75f,0.46f));
            Item(T("Biodiversité","Biodiversity")+" : "+
                selectedSite.BiodiversityBp/100f+" %");
            DrawMeter(selectedSite.BiodiversityBp,10000,new Color(0.39f,0.69f,0.82f));
            Item(T("Contributeurs distincts","Distinct contributors")+" : "+
                selectedSite.DistinctContributors);
            Item(T("Points de pollinisation","Pollination points")+" : "+
                selectedSite.TotalPollinationPoints);
            GUILayout.Space(14f);
            if(GUILayout.Button(T("PRÉPARER UN ESSAIM","PREPARE A SWARM"),
                GUILayout.Height(38f))) {tab=1;scroll=Vector2.zero;}
        }

        private void DrawSwarm(IPollinationPanelController controller,
            PollinationScreenModel model)
        {
            Section(T("Population civile","Civilian population"));
            var pop=model.Population;
            var exp=model.Expedition;
            if (pop==null || exp==null)
            {
                Item(T("Données en préparation.","Preparing data."));return;
            }
            Item(T("Butineuses disponibles","Available foragers")+" : "+pop.AvailableWorkers);
            Item(T("Butineuses en mission","Assigned foragers")+" : "+pop.AssignedWorkers);
            Item(T("Capacité civile","Civilian capacity")+" : "+pop.Capacity);
            if(pop.Active!=null)
                Item(T("Nouvelle cohorte attendue","Cohort expected")+" : "+
                    pop.Active.ReadyAtUtc.ToLocalTime().ToString("g"));
            if(pop.CanStart)
            {
                if (GUILayout.Button(T("FORMER UNE COHORTE","START A COHORT"),
                    GUILayout.Height(34f))) controller.StartCivilCohort();
            }

            GUILayout.Space(20f);
            Section(T("Expédition civile","Civilian expedition"));
            if (exp.Active!=null)
            {
                Item(T("Destination","Destination")+" : "+exp.Active.SiteId);
                Item(T("Butineuses engagées","Foragers assigned")+" : "+
                    exp.Active.Workers);
                Item(T("Phase officielle","Official phase")+" : "+
                    Phase(exp.Status));
                Item(T("Retour prévu","Expected return")+" : "+
                    exp.Active.ReturnsAtUtc.ToLocalTime().ToString("g"));
                if(exp.LastCompletion!=null)
                    Item(T("Dernière récolte","Previous collection")+" : "+
                        exp.LastCompletion.PollenCredited);
                return;
            }

            RemoteFloralSite site=model.SelectedSite;
            if(site==null)
            {
                Item(T("Choisissez d’abord un champ.",
                    "Choose a field first."));return;
            }
            Item(T("Destination","Destination")+" : "+
                site.LocalizedName(BeeLocalization.CurrentLocale));
            Item(T("Essaim civil","Civilian swarm")+" : "+model.DraftWorkers);
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("−",GUILayout.Width(52f),GUILayout.Height(38f)))
                controller.AdjustWorkers(-1);
            if(GUILayout.Button("+",GUILayout.Width(52f),GUILayout.Height(38f)))
                controller.AdjustWorkers(1);
            if(GUILayout.Button("+10",GUILayout.Width(62f),GUILayout.Height(38f)))
                controller.AdjustWorkers(10);
            GUILayout.EndHorizontal();
            GUILayout.Space(7f);
            Item(T("Mode : équilibré (Alpha)","Mode: balanced (Alpha)"));
            Item(T("La récolte est confirmée au retour par le serveur.",
                "The server confirms the harvest after the return."));
            bool previous=GUI.enabled;
            GUI.enabled=previous&&model.CanLaunch&&!controller.IsBusy;
            if(GUILayout.Button(T("ENVOYER L’ESSAIM CIVIL","SEND CIVILIAN SWARM"),
                GUILayout.Height(44f))) controller.LaunchSelectedExpedition();
            GUI.enabled=previous;
            if(exp.LastCompletion!=null)
            {
                GUILayout.Space(19f);
                Section(T("Dernier rapport","Last report"));
                Item(T("Pollen réellement transporté","Pollen actually delivered")
                    +" : "+exp.LastCompletion.PollenCredited);
                Item(T("Points écologiques","Ecological points")
                    +" : "+exp.LastCompletion.PollinationPoints);
            }
        }

        private void DrawAlliance(IPollinationPanelController controller,
            PollinationScreenModel model)
        {
            Section(T("Projets floraux d’alliance","Alliance floral projects"));
            if(model.AllianceId==Guid.Empty)
            {
                Item(T("Rejoignez une alliance pour participer aux projets collectifs.",
                    "Join an alliance to contribute to collective projects."));
                return;
            }
            if(model.Projects==null || model.Projects.Count==0)
                Item(T("Aucun projet actif.","No active project."));
            else foreach(var wrapper in model.Projects)
            {
                RemoteFloralAllianceProject project=wrapper.Project;
                if(project==null)continue;
                GUILayout.Space(6f);
                Section(project.SiteId);
                Item(T("Progression","Progress")+" : "+project.Points+
                    " / "+project.GoalPoints);
                DrawMeter(project.Points,project.GoalPoints,Gold);
                Item(T("Membres qualifiés","Qualified members")+" : "+
                    project.QualifiedMembers+" / "+project.RequiredDistinctMembers);
                Item(wrapper.IsCompleted ?
                    T("Projet terminé","Project completed") :
                    T("Objectif collectif en cours","Collective objective in progress"));
            }
            if(model.CanCreateProject&&!controller.IsBusy)
            {
                GUILayout.Space(10f);
                if(GUILayout.Button(T("OUVRIR UN PROJET FLORAL",
                    "CREATE A FLORAL PROJECT"),GUILayout.Height(41f)))
                    controller.CreateSelectedAllianceProject();
            }
        }

        private static string Phase(string phase)
        {
            switch(phase)
            {
                case "outbound": return T("En route","Outbound");
                case "pollinating": return T("Pollinisation","Pollinating");
                case "returning": return T("Retour","Returning");
                case "awaiting_settlement": return T("Retour arrivé","Arrived");
                default: return T("Disponible","Available");
            }
        }

        private void Section(string title)
        {
            GUILayout.Space(8f);
            GUILayout.Label(title,
                TextStyle(18,Gold,FontStyle.Bold),GUILayout.Height(29f));
        }
        private void Item(string label)
        {
            GUILayout.Label(label,
                TextStyle(14,Parchment,FontStyle.Normal),
                GUILayout.MinHeight(23f));
        }
        private void DrawMeter(long value,long max,Color color)
        {
            Rect line=GUILayoutUtility.GetRect(10f,10f,GUILayout.ExpandWidth(true));
            Bar(line,value,max,color);
            GUILayout.Space(7f);
        }
    }
}
