using System;
using System.Linq;
using BeeKingdom.Networking;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeeKingdom.Playground.Editor
{
    public sealed class PollinationPremiumWindowEditorTests
    {
        private sealed class FakeController : IPollinationPanelController
        {
            public PollinationScreenModel Model {get;} = new PollinationScreenModel();
            public bool IsConfigured {get;set;} = true;
            public bool IsBusy {get;set;}
            public int RefreshCalls {get; private set;}
            public void Refresh() => RefreshCalls++;
            public void SelectSite(string siteId) {}
            public void AdjustWorkers(long delta) {}
            public void ConfigureAlliance(Guid id,bool mayManage) {}
            public void LaunchSelectedExpedition() {}
            public void StartCivilCohort() {}
            public void CreateSelectedAllianceProject() {}
        }

        private static Button FindButton(GameObject root,params string[] words)
        {
            return root.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(button =>
                {
                    string text = button.GetComponentInChildren<TextMeshProUGUI>(true)?.text;
                    return !string.IsNullOrEmpty(text) &&
                        words.Any(x => text.IndexOf(x,StringComparison.OrdinalIgnoreCase)>=0);
                });
        }

        [Test]
        public void PollinationReusesActivitiesV4CanvasAndHasOneTouchScrollbar()
        {
            var host = new GameObject("Pollination Royal QA");
            try
            {
                var controller = new FakeController();
                var window = host.AddComponent<PollinationPremiumWindow>();
                window.Show(controller,()=>{});
                Assert.That(window.IsOpen,Is.True);
                Assert.That(window.CanvasForProof,Is.Not.Null);
                // Root canvases own their sorting order; overrideSorting is
                // only meaningful for nested canvases in Unity EditMode.
                Assert.That(window.CanvasForProof.isRootCanvas ||
                    window.CanvasForProof.overrideSorting,Is.True);
                Assert.That(window.CanvasForProof.sortingOrder,
                    Is.GreaterThan(420),"Pollination must cover existing Activities.");
                Assert.That(window.HasRaycastBlockerForProof,Is.True);
                Assert.That(host.GetComponentsInChildren<GraphicRaycaster>(true).Length,
                    Is.EqualTo(1),"Only one raycaster on the modal.");
                Assert.That(host.GetComponentsInChildren<ScrollRect>(true).Length,
                    Is.EqualTo(1),"No nested scroll view.");
                Assert.That(window.ScrollForProof.verticalScrollbar,Is.Not.Null);
                Assert.That(window.ScrollForProof.horizontal,Is.False);
                Assert.That(host.GetComponentsInChildren<RectMask2D>(true).Length,
                    Is.EqualTo(1),"Content is clipped under royal header.");
                Assert.That(host.GetComponentsInChildren<TextMeshProUGUI>(true)
                    .Any(label=>label.text=="POLLINISATION" ||
                        label.text=="POLLINATION"),Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        [Test]
        public void TabsKeepSameScrollRectAndCloseDisablesInputBlocker()
        {
            var host = new GameObject("Pollination Royal Tabs QA");
            try
            {
                bool closed = false;
                var window = host.AddComponent<PollinationPremiumWindow>();
                var controller = new FakeController();
                window.Show(controller,()=>closed=true);
                ScrollRect original=window.ScrollForProof;
                Button swarms=FindButton(host,"Essaims","Swarms");
                Assert.That(swarms,Is.Not.Null);
                swarms.onClick.Invoke();
                Assert.That(window.ActiveTabForProof,Is.EqualTo(1));
                Assert.That(window.ScrollForProof,Is.SameAs(original));
                Button alliance=FindButton(host,"Alliance");
                Assert.That(alliance,Is.Not.Null);
                alliance.onClick.Invoke();
                Assert.That(window.ActiveTabForProof,Is.EqualTo(2));
                Assert.That(window.ScrollForProof,Is.SameAs(original));
                Button refresh=FindButton(host,"Actualiser","Refresh");
                Assert.That(refresh,Is.Not.Null);
                refresh.onClick.Invoke();
                Assert.That(controller.RefreshCalls,Is.EqualTo(1));
                Button back=FindButton(host,"Retour","Back");
                Assert.That(back,Is.Not.Null);
                back.onClick.Invoke();
                Assert.That(closed,Is.True);
                Assert.That(window.IsOpen,Is.False);
                Assert.That(window.CanvasForProof.gameObject.activeSelf,Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        [Test]
        public void PollinationOnlyShowsJoinPromptAfterAuthoritativeNoAlliance()
        {
            var host = new GameObject("Pollination Membership QA");
            try
            {
                var window = host.AddComponent<PollinationPremiumWindow>();
                var fake = new FakeController();
                typeof(PollinationScreenModel).GetProperty(nameof(PollinationScreenModel.State)).GetSetMethod(true).Invoke(fake.Model, new object[] { PollinationScreenState.Ready });
                window.Show(fake, () => { });
                FindButton(host, "Alliance").onClick.Invoke();
                string text = string.Join(" ", host.GetComponentsInChildren<TextMeshProUGUI>(true)
                    .Select(label => label.text));
                StringAssert.DoesNotContain("Rejoignez une alliance", text);
                StringAssert.DoesNotContain("Join an alliance", text);
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        [Test]
        public void AllianceMembershipProjectionNeverTreatsLoadingOrErrorAsNoAlliance()
        {
            Guid id = Guid.NewGuid();
            var entity = new RemoteAllianceEntity
            {
                AllianceId = id, Name = "QA Collective", Tag = "QA",
                LeaderPlayerId = Guid.NewGuid(),
                JoinMode = RemoteAllianceJoinMode.Open
            };
            var member = new RemoteAllianceMembership
            {
                AllianceId = id, PlayerId = entity.LeaderPlayerId,
                Role = RemoteAllianceRole.Leader
            };
            AllianceCenterScreenModel confirmed = AllianceCenterPresentation.Ready(
                entity, member, Array.Empty<RemoteAllianceMemberSummary>(), null, null);
            Assert.That(PollinationWorldMapOverlay.ResolveAllianceMembershipForProof(
                confirmed, out Guid actual, out bool canManage), Is.True);
            Assert.That(actual, Is.EqualTo(id));
            Assert.That(canManage, Is.True);
            Assert.That(PollinationWorldMapOverlay.ResolveAllianceMembershipForProof(
                AllianceCenterPresentation.Loading(confirmed), out _, out _),
                Is.False);
            Assert.That(PollinationWorldMapOverlay.ResolveAllianceMembershipForProof(
                AllianceCenterPresentation.Error(confirmed, "timeout"), out _, out _),
                Is.False);
            Assert.That(PollinationWorldMapOverlay.ResolveAllianceMembershipForProof(
                AllianceCenterPresentation.NoAlliance(null, null),
                out Guid empty, out bool mayManage), Is.True);
            Assert.That(empty, Is.EqualTo(Guid.Empty));
            Assert.That(mayManage, Is.False);
        }

        [Test]
        public void ActivitiesV4SafeAreaGeometryIsSharedWithoutChangingExistingWindow()
        {
            foreach(Vector2 size in new[]
                {
                    new Vector2(390f,844f),new Vector2(1920f,1080f),
                    new Vector2(844f,390f)
                })
            {
                Rect expected=HiveMapPremiumBuildingWindow.HeaderRectForProof(
                    size.x,size.y);
                Rect actual=PollinationPremiumWindow.HeaderForProof(size.x,size.y);
                Assert.That(actual,Is.EqualTo(expected));
                Assert.That(PollinationPremiumWindow.ViewportForProof(size.x,size.y),
                    Is.EqualTo(HiveMapPremiumBuildingWindow.ScrollViewportForProof(
                        size.x,size.y)));
            }
        }

        [Test]
        public void UnconfiguredControllerCannotDisplayOrCreateRoyalCanvas()
        {
            var host=new GameObject("Pollination Royal Off QA");
            try
            {
                var fake=new FakeController {IsConfigured=false};
                var window=host.AddComponent<PollinationPremiumWindow>();
                window.Show(fake,()=>{});
                Assert.That(window.IsOpen,Is.False);
                Assert.That(window.CanvasForProof,Is.Null);
                Assert.That(window.HasRaycastBlockerForProof,Is.False);
            }
            finally {UnityEngine.Object.DestroyImmediate(host);}
        }
    }
}
