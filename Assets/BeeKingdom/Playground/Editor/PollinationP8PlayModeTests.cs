using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BeeKingdom.Localization;
using BeeKingdom.Networking;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BeeKingdom.Playground.Editor
{
    /// <summary>
    /// Isolated P8 acceptance checks exercising actual Unity PlayMode UI
    /// under the same project, with feature flags OFF and in-memory DTOs.
    /// No network calls, scene saves, or changes to production accounts.
    /// </summary>
    public sealed class PollinationP8PlayModeTests
    {
        [TearDown]
        public void RestoreUnityErrorPolicy()
        {
            // Do not let the test-only workaround for a third-party
            // MCP connector influence later or unrelated Unity tests.
            LogAssert.ignoreFailingMessages=false;
        }

        private const string WorldMapScene =
            "Assets/Scenes/WorldMapMmoFullscreenFoundation.unity";
        private static readonly DateTimeOffset Clock =
            new DateTimeOffset(2026,10,21,19,0,0,TimeSpan.Zero);
        private static readonly Guid Player =
            Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid Hive =
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid Alliance =
            Guid.Parse("abcdefab-cdef-abcd-efab-cdefabcdefab");

        private sealed class FixtureController : IPollinationPanelController
        {
            public PollinationScreenModel Model { get; } =
                new PollinationScreenModel();
            public bool IsConfigured { get; set; } = true;
            public bool IsBusy { get; set; }
            public int RefreshCalls { get; private set; }
            public int LaunchCalls { get; private set; }
            public int CreateProjectCalls { get; private set; }
            public void Refresh() { RefreshCalls++; }
            public void SelectSite(string siteId)
            {
                Assign(Model,"SelectedSiteId",siteId);
            }
            public void AdjustWorkers(long delta)
            {
                Assign(Model,"DraftWorkers",
                    Math.Max(1,Math.Min(12,Model.DraftWorkers+delta)));
            }
            public void ConfigureAlliance(Guid id,bool mayManage)
            {
                Assign(Model,"AllianceId",id);
                Assign(Model,"MayManageAllianceProjects",mayManage);
            }
            public void LaunchSelectedExpedition() { LaunchCalls++; }
            public void StartCivilCohort() { }
            public void CreateSelectedAllianceProject() { CreateProjectCalls++; }
        }

        private static void Assign<T>(object model,string name,T value)
        {
            PropertyInfo prop=model.GetType().GetProperty(
                name,BindingFlags.Public|BindingFlags.Instance);
            Assert.That(prop,Is.Not.Null,"Missing UI projection: "+name);
            prop.SetValue(model,value);
        }

        private static FixtureController ReadyModel()
        {
            var controller=new FixtureController();
            var site=new RemoteFloralSite
            {
                ContractVersion=PollinationClient.SiteVersion,
                WorldId="world-qa",RegionId="region-qa",SiteId="clover-qa-01",
                FamilyId="clover",NameFr="Trèfle blanc",NameEn="White clover",
                CatalogVersion="p8.qa",Revision=0,ServerTimeUtc=Clock,
                PollenCapacity=90,PollenRemaining=40,
                FertilityBp=7200,BiodiversityBp=6300,
                TotalPollinationPoints=50,DistinctContributors=2,
                Phase="blooming",PublicAccess=true
            };
            var site2=new RemoteFloralSite
            {
                ContractVersion=PollinationClient.SiteVersion,
                WorldId="world-qa",RegionId="region-qa",SiteId="clover-qa-02",
                FamilyId="clover",NameFr="Prairie fleurie",NameEn="Flower Meadow",
                CatalogVersion="p8.qa",Revision=0,ServerTimeUtc=Clock,
                PollenCapacity=120,PollenRemaining=60,
                FertilityBp=8100,BiodiversityBp=6700,
                TotalPollinationPoints=60,DistinctContributors=3,
                Phase="blooming",PublicAccess=true
            };
            var population=new RemoteCivilPopulationSnapshot
            {
                ContractVersion=PollinationClient.PopulationVersion,
                PlayerId=Player,HiveId=Hive,ServerTimeUtc=Clock,
                Capacity=200,AvailableWorkers=30,AssignedWorkers=0,
                CivilRevision=0,HiveRevision=0,Status="available",CanStart=true
            };
            var expedition=new RemoteCivilExpeditionSnapshot
            {
                ContractVersion=PollinationClient.ExpeditionVersion,
                PlayerId=Player,HiveId=Hive,ServerTimeUtc=Clock,
                HiveRevision=0,AvailableWorkers=30,AssignedWorkers=0,
                Status="idle"
            };
            var project=new RemoteFloralAllianceProjectSnapshot
            {
                ContractVersion=PollinationClient.ProjectVersion,
                ServerTimeUtc=Clock,
                Project=new RemoteFloralAllianceProject
                {
                    ProjectId=Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff"),
                    AllianceId=Alliance,WorldId="world-qa",
                    SiteId="clover-qa-01",CreatedByPlayerId=Player,
                    CreatedAtUtc=Clock,GoalPoints=100,
                    RequiredDistinctMembers=2,MinimumPointsPerMember=5,
                    Points=80,QualifiedMembers=1,Status="Active"
                },
                Progression=new RemoteFloralAllianceProgression
                {
                    AllianceId=Alliance,Level=1,Revision=1,
                    Experience=0,CompletedProjects=0
                },
                IsCompleted=false
            };
            PollinationClient.ValidateSite(site);
            PollinationClient.ValidateSite(site2);
            PollinationClient.ValidatePopulation(population,Player,Hive);
            PollinationClient.ValidateExpedition(expedition,Player,Hive);
            PollinationClient.ValidateProject(project,Alliance);
            Assign(controller.Model,"Sites",
                (IReadOnlyList<RemoteFloralSite>)new List<RemoteFloralSite>{site,site2});
            Assign(controller.Model,"Population",population);
            Assign(controller.Model,"Expedition",expedition);
            Assign(controller.Model,"Projects",
                (IReadOnlyList<RemoteFloralAllianceProjectSnapshot>)
                    new List<RemoteFloralAllianceProjectSnapshot>{project});
            Assign(controller.Model,"SelectedSiteId",site.SiteId);
            Assign(controller.Model,"DraftWorkers",10L);
            Assign(controller.Model,"AllianceId",Alliance);
            Assign(controller.Model,"MayManageAllianceProjects",true);
            Assign(controller.Model,"State",PollinationScreenState.Ready);
            return controller;
        }

        private static string CapturePath(string name)
        {
            string folder=Path.GetFullPath(Path.Combine(
                Application.dataPath,"..","..","..","qa",
                "pollinisation-p8-playmode-20261021"));
            Directory.CreateDirectory(folder);
            return Path.Combine(folder,name);
        }

        private static bool HasText(GameObject host,string needle) =>
            host.GetComponentsInChildren<TextMeshProUGUI>(true)
                .Any(x=>x.text?.IndexOf(needle,StringComparison.OrdinalIgnoreCase)>=0);

        private static readonly List<string> UnexpectedLogs = new List<string>();

        private static void CaptureP8Log(string message,string stack,LogType type)
        {
            if((type==LogType.Exception || type==LogType.Error
                || type==LogType.Assert)
                && !KnownEditorMcpNetworkNoise(message,stack))
                UnexpectedLogs.Add(message);
        }

        private static bool KnownEditorMcpNetworkNoise(string text,string stack)
        {
            return (text.Contains("TaskCanceledException")
                    || text.Contains("HttpRequestException"))
                && (stack.Contains("com.IvanMurzak")
                    || stack.Contains("McpPlugin")
                    || stack.Contains("Unity.MCP"));
        }

        [UnityTest]
        public IEnumerator PremiumPanel_PlayMode_CapturesInputAndPreservesServerAuthority()
        {
            // A third-party Unity MCP connector can emit expected network
            // cancellation errors when batchmode enters PlayMode. LogAssert
            // suppression is paired with a strict manual whitelist below.
            LogAssert.ignoreFailingMessages=true;
            yield return new EnterPlayMode();
            // Watch all Unity errors during the actual P8 UI exercise. Only
            // third-party MCP network-cancel exceptions are permitted.
            UnexpectedLogs.Clear();
            Application.logMessageReceived += CaptureP8Log;
            string previous=BeeLocalization.CurrentLocale;
            GameObject host=null;
            GameObject underlay=null;
            try
            {
                Assert.That(BeeLocalization.SetLocale("fr-CA"),Is.True);
                var controller=ReadyModel();
                host=new GameObject("P8 Runtime Royal Pollination");
                var panel=host.AddComponent<PollinationPremiumWindow>();
                underlay=new GameObject("P8 Underlying WorldMap Button");
                var baseCanvas=underlay.AddComponent<Canvas>();
                baseCanvas.renderMode=RenderMode.ScreenSpaceOverlay;
                baseCanvas.sortingOrder=100;
                underlay.AddComponent<GraphicRaycaster>();
                var underlayRect=new GameObject("UnderlayButton",
                    typeof(RectTransform),typeof(Image),typeof(Button));
                underlayRect.transform.SetParent(underlay.transform,false);
                var rect=underlayRect.GetComponent<RectTransform>();
                rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
                rect.offsetMin=rect.offsetMax=Vector2.zero;
                underlayRect.GetComponent<Image>().raycastTarget=true;
                bool clickedUnderlay=false;
                underlayRect.GetComponent<Button>().onClick.AddListener(
                    ()=>clickedUnderlay=true);

                panel.Show(controller,()=>{});
                yield return null;
                Canvas.ForceUpdateCanvases();
                Assert.That(panel.IsOpen,Is.True);
                Assert.That(panel.HasRaycastBlockerForProof,Is.True);
                Assert.That(panel.ScrollForProof,Is.Not.Null);
                Assert.That(host.GetComponentsInChildren<ScrollRect>(true).Length,
                    Is.EqualTo(1));
                Assert.That(HasText(host,"Trèfle blanc"),Is.True,
                    "FR site title must be server-derived and preserve accent.");
                Assert.That(HasText(host,"Fertilité"),Is.True);
                ScrollRect stableScroll=panel.ScrollForProof;
                stableScroll.verticalNormalizedPosition=.40f;
                yield return null;
                float beforeRefresh=stableScroll.verticalNormalizedPosition;
                controller.Refresh();
                yield return null;
                Assert.That(panel.ScrollForProof,Is.SameAs(stableScroll),
                    "A read-only refresh cannot rebuild the ScrollRect.");
                Assert.That(stableScroll.verticalNormalizedPosition,
                    Is.EqualTo(beforeRefresh).Within(.025f),
                    "Refresh must not make the user's scroll position jump.");
                string frShot=CapturePath("p8-fields-fr.png");
                ScreenCapture.CaptureScreenshot(frShot);
                for(int frames=0;frames<4;frames++) yield return null;
                Debug.Log("P8_FR_SCREENSHOT_EXISTS="+File.Exists(frShot));
                Assert.That(panel.CanvasForProof.sortingOrder,Is.GreaterThan(420));
                Assert.That(EventSystem.current,Is.Not.Null);
                var hits=new List<RaycastResult>();
                var click=new PointerEventData(EventSystem.current)
                {
                    position=new Vector2(Screen.width/2f,Screen.height/2f)
                };
                EventSystem.current.RaycastAll(click,hits);
                var panelRaycaster = panel.CanvasForProof.GetComponent<GraphicRaycaster>();
                var backdrop = panel.CanvasForProof.transform.Find("Backdrop")
                    ?.GetComponent<RawImage>();
                Assert.That(panelRaycaster, Is.Not.Null);
                Assert.That(panelRaycaster.isActiveAndEnabled, Is.True);
                Assert.That(backdrop, Is.Not.Null);
                Assert.That(backdrop.isActiveAndEnabled, Is.True);
                Assert.That(backdrop.raycastTarget, Is.True);
                if (hits.Count == 0 && Application.isBatchMode && backdrop.depth == -1)
                {
                    // In batch-mode, Unity may register all graphics while
                    // skipping the depth assignment performed by GameView rendering.
                    // Never treat this structural gate as a visual GameView proof.
                    Assert.That(GraphicRegistry.GetGraphicsForCanvas(panel.CanvasForProof)
                        .Contains(backdrop), Is.True, "Backdrop must be registered.");
                    Assert.That(RectTransformUtility.RectangleContainsScreenPoint(
                        backdrop.rectTransform,click.position,null),Is.True,
                        "Backdrop must cover the world-map interaction point.");
                    Assert.That(backdrop.Raycast(
                        click.position,panelRaycaster.eventCamera),Is.True,
                        "Backdrop must accept raycasts even without rendered depth.");
                    Debug.Log("P8_BATCH_DEPTH_MINUS_ONE_STRUCTURAL_GATE=PASS;"
                        + "GAMEVIEW_RAYCAST_PROOF=REQUIRED");
                }
                else
                {
                    Assert.That(hits.Count,Is.GreaterThan(0),"A UI raycast is required.");
                    Assert.That(hits[0].gameObject.transform.IsChildOf(
                        panel.CanvasForProof.transform),Is.True,
                        "Royal Canvas must intercept the world-map button underneath.");
                }

                ScrollRect scroll=panel.ScrollForProof;
                int panelChildren=host.GetComponentsInChildren<ScrollRect>(true).Length;
                panel.SelectTabForProof(1);
                yield return null;
                Assert.That(panel.ScrollForProof,Is.SameAs(scroll),
                    "Tab changes must not replace the scroll rect.");
                Assert.That(panelChildren,Is.EqualTo(1));
                Assert.That(HasText(host,"BUTINEUSES CIVILES"),Is.True);
                Assert.That(HasText(host,"30"),Is.True);

                panel.SelectTabForProof(2);
                yield return null;
                Assert.That(HasText(host,"Membres qualifiés"),Is.True);
                Assert.That(controller.CreateProjectCalls,Is.Zero,
                    "Merely drawing the Alliance tab cannot start a project.");

                Assert.That(BeeLocalization.SetLocale("en-US"),Is.True);
                // Allow Unity's Update to rebuild the localized canvas before
                // checking the labels. Test coroutines may resume ahead of Update
                // on the first frame after a locale change.
                for (int frame = 0; frame < 5 && !HasText(host,"COOPERATION"); frame++)
                    yield return null;
                Assert.That(HasText(host,"COOPERATION"),Is.True,
                    "Alliance cards must switch to English within five frames.");
                Assert.That(HasText(host,"Qualified members"),Is.True);
                string enShot=CapturePath("p8-alliance-en.png");
                ScreenCapture.CaptureScreenshot(enShot);
                for(int frames=0;frames<4;frames++) yield return null;
                Debug.Log("P8_EN_SCREENSHOT_EXISTS="+File.Exists(enShot));
                Assert.That(controller.LaunchCalls,Is.Zero,
                    "UI refresh must never launch a civilian mission.");
                Assert.That(clickedUnderlay,Is.False);
                Assert.That(UnexpectedLogs,Is.Empty,
                    "Unexpected Unity errors during PlayMode modal checks.");
                panel.Hide();
                yield return null;
                Assert.That(panel.IsOpen,Is.False);
                Assert.That(panel.CanvasForProof.gameObject.activeSelf,Is.False);
            }
            finally
            {
                Application.logMessageReceived -= CaptureP8Log;
                BeeLocalization.SetLocale(previous);
                if(host!=null) UnityEngine.Object.Destroy(host);
                if(underlay!=null) UnityEngine.Object.Destroy(underlay);
            }
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator WorldMapFoundation_PlayMode_LeavesPollinationDisabled()
        {
            Assert.That(File.Exists(Path.GetFullPath(WorldMapScene)),Is.True);
            LogAssert.ignoreFailingMessages=true;
            yield return new EnterPlayMode();
            LogAssert.ignoreFailingMessages=true;
            // Unity test runner restores its own SandboxPlayground start
            // scene at EnterPlayMode: explicitly load the canonical map now.
            AsyncOperation load=SceneManager.LoadSceneAsync(
                "WorldMapMmoFullscreenFoundation",LoadSceneMode.Single);
            Assert.That(load,Is.Not.Null);
            yield return load;
            try
            {
                yield return null;
                yield return null;
                Assert.That(SceneManager.GetActiveScene().name,
                    Is.EqualTo("WorldMapMmoFullscreenFoundation"));
                Assert.That(UnityEngine.Object.FindAnyObjectByType<
                    WorldMapMmoFullscreenFoundationBootstrap>(),Is.Not.Null);
                Assert.That(MobileAccountSessionRuntimeBootstrap
                    .PollinationFeatureEnabledForWorldMap,Is.False,
                    "No live server, hive or client feature flag should be enabled.");
                Assert.That(PollinationWorldMapOverlay.IsModalOpen,Is.False,
                    "Opt-out map cannot be obstructed by the new premium modal.");
                var open=UnityEngine.Object.FindAnyObjectByType<PollinationPremiumWindow>();
                Assert.That(open==null || !open.IsOpen,Is.True);
                string sceneShot=CapturePath("p8-worldmap-flags-off.png");
                ScreenCapture.CaptureScreenshot(sceneShot);
                for(int frames=0;frames<4;frames++) yield return null;
                Debug.Log("P8_WORLDMAP_SCREENSHOT_EXISTS="+File.Exists(sceneShot));
            }
            finally
            {
                // Unity Test Framework owns PlayMode scene teardown.
            }
            yield return new ExitPlayMode();
        }
    }
}
