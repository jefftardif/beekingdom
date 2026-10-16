using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.Networking;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeeKingdom.Playground.Editor
{
    public sealed class BKStudioNurseryActivitiesTests
    {
        private sealed class FakeCareController : IHiveBroodVitalityPanelController
        {
            public HiveBroodVitalityScreenModel Model { get; set; }
            public bool IsConfigured { get; set; } = true;
            public bool IsBusy { get; set; }
            public TimeSpan Elapsed { get; set; }
            public int RefreshCalls { get; private set; }
            public int CompleteCalls { get; private set; }
            public int RetryCalls { get; private set; }
            public List<string> Starts { get; } = new List<string>();
            public void Refresh() { RefreshCalls++; }
            public void Start(string type) { Starts.Add(type); }
            public void Complete() { CompleteCalls++; }
            public void Retry() { RetryCalls++; }
        }

        private static HiveBroodVitalityScreenModel CreateModel(
            HiveBroodVitalityScreenState state = HiveBroodVitalityScreenState.Ready,
            int nutrition = 62, int stability = 71, string active = null)
        {
            var now = new DateTimeOffset(2026, 10, 14, 15, 0, 0, TimeSpan.Zero);
            return HiveBroodVitalityPresentation.Project(state,
                new RemoteBroodVitalitySnapshot {
                    ServerTimeUtc = now,
                    GlobalRevision = 7,
                    Vitality = new RemoteBroodVitalityState {
                        Nutrition = nutrition,
                        Stability = stability,
                        Revision = 3,
                        UpdatedAtUtc = now,
                        ActiveOperation = active == null ? null :
                            new RemoteBroodVitalityOperation {
                                OperationId = Guid.NewGuid(),
                                Type = active,
                                StartedAtUtc = now,
                                EndsAtUtc = now.AddSeconds(12)
                            }
                    }
                }, TimeSpan.Zero, true);
        }

        private static Button FindButton(GameObject host, string french, string english)
        {
            return host.GetComponentsInChildren<Button>(true).FirstOrDefault(button => {
                var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
                return label != null && (label.text.StartsWith(french, StringComparison.Ordinal) ||
                    label.text.StartsWith(english, StringComparison.Ordinal));
            });
        }

        [Test]
        public void NurseryUsesTheSharedFullscreenActivitiesCanvasAndShowsLiveServerValues()
        {
            var host = new GameObject("Nursery Activities Test", typeof(RectTransform));
            host.SetActive(false);
            try
            {
                var fake = new FakeCareController { Model = CreateModel() };
                var window = host.AddComponent<HiveMapPremiumBuildingWindow>();
                window.BindNurseryController(fake);
                window.Show(BuildingCatalog.GetByBuildingType(BuildingTypes.Nursery),
                    "Nurserie", "Nursery", "", "", () => window.Hide(), () => { });
                Assert.That(window.NurseryActiveForProof, Is.True);
                Assert.That(window.IsOpen, Is.True);
                Assert.That(fake.RefreshCalls, Is.EqualTo(1));
                var canvas = host.GetComponentInChildren<Canvas>(true);
                Assert.That(canvas, Is.Not.Null);
                Assert.That(canvas.sortingOrder, Is.GreaterThan(240));
                var scrolling = host.GetComponentInChildren<ScrollRect>(true);
                Assert.That(scrolling, Is.Not.Null);
                Assert.That(scrolling.verticalScrollbar, Is.Not.Null);
                string text = string.Join("|", host.GetComponentsInChildren<TextMeshProUGUI>(true)
                    .Select(l => l.text).ToArray());
                Assert.That(text, Does.Contain("62 / 100"));
                Assert.That(text, Does.Contain("71 / 100"));
                Assert.That(text, Does.Not.Contain("Fonctionnalité en préparation"));
                var fills = host.GetComponentsInChildren<Image>(true)
                    .Where(i => i.type == Image.Type.Filled).ToArray();
                Assert.That(fills.Length, Is.EqualTo(3));
                Assert.That(fills.Any(i => Math.Abs(i.fillAmount - .62f) < .001f), Is.True);
                Assert.That(fills.Any(i => Math.Abs(i.fillAmount - .71f) < .001f), Is.True);
                window.Hide();
                Assert.That(window.NurseryActiveForProof, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        [Test]
        public void NourrirAndStabiliserReuseTheActualServerBackedActions()
        {
            var host = new GameObject("Nursery Mutations Test");
            host.SetActive(false);
            try
            {
                var fake = new FakeCareController { Model = CreateModel() };
                var window = host.AddComponent<HiveMapPremiumBuildingWindow>();
                window.BindNurseryController(fake);
                window.Show(BuildingCatalog.GetByBuildingType(BuildingTypes.Nursery),
                    "Nurserie", "Nursery", "", "", () => { }, () => { });
                Button feed = FindButton(host, "Nourrir", "Feed");
                Button stabilize = FindButton(host, "Stabiliser", "Stabilize");
                Assert.That(feed, Is.Not.Null);
                Assert.That(stabilize, Is.Not.Null);
                Assert.That(feed.interactable, Is.True);
                Assert.That(stabilize.interactable, Is.True);
                feed.onClick.Invoke();
                stabilize.onClick.Invoke();
                Assert.That(fake.Starts, Is.EqualTo(new[] {
                    HiveBroodVitalityClient.FeedingType,
                    HiveBroodVitalityClient.StabilizationType }));
                Assert.That(fake.CompleteCalls, Is.Zero);
                Assert.That(fake.RetryCalls, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        [Test]
        public void ActiveCareAndPendingStatesDoNotAllowDoubleSpend()
        {
            var host = new GameObject("Nursery Care Lifecycle Test");
            host.SetActive(false);
            try
            {
                var fake = new FakeCareController {
                    Model = CreateModel(HiveBroodVitalityScreenState.Ready,
                        50, 50, HiveBroodVitalityClient.FeedingType) };
                var window = host.AddComponent<HiveMapPremiumBuildingWindow>();
                window.BindNurseryController(fake);
                window.Show(BuildingCatalog.GetByBuildingType(BuildingTypes.Nursery),
                    "Nurserie", "Nursery", "", "", () => { }, () => { });
                Button feed = FindButton(host, "Nourrir", "Feed");
                Button complete = FindButton(host, "Terminer", "Complete");
                Button retry = FindButton(host, "Vérifier", "Verify");
                Assert.That(feed.interactable, Is.False);
                Assert.That(complete.gameObject.activeSelf, Is.True);
                Assert.That(complete.interactable, Is.False);
                Assert.That(retry.gameObject.activeSelf, Is.False);
                fake.Elapsed = TimeSpan.FromSeconds(20);
                var update = typeof(HiveMapPremiumBuildingWindow)
                    .GetMethod("RefreshNurseryLabels",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.That(update, Is.Not.Null);
                update.Invoke(window, null);
                Assert.That(complete.interactable, Is.True);
                complete.onClick.Invoke();
                Assert.That(fake.CompleteCalls, Is.EqualTo(1));
                Assert.That(fake.Starts.Count, Is.Zero);

                fake.Model = HiveBroodVitalityPresentation.Project(
                    HiveBroodVitalityScreenState.PendingConfirmation,
                    new RemoteBroodVitalitySnapshot {
                        ServerTimeUtc = DateTimeOffset.UtcNow,
                        Vitality = new RemoteBroodVitalityState {
                            Nutrition = 50, Stability = 50,
                            UpdatedAtUtc = DateTimeOffset.UtcNow
                        }
                    }, TimeSpan.Zero, true,
                    pendingKind: "start",
                    pendingPayload: HiveBroodVitalityClient.FeedingType);
                update.Invoke(window, null);
                Assert.That(feed.interactable, Is.False);
                Assert.That(retry.gameObject.activeSelf, Is.True);
                retry.onClick.Invoke();
                Assert.That(fake.RetryCalls, Is.EqualTo(1));
                Assert.That(fake.Starts.Count, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        [Test]
        public void OfflineAndUnconfiguredSessionsDoNotShowExecutableCare()
        {
            var host = new GameObject("Nursery Offline Test");
            host.SetActive(false);
            try
            {
                var controller = new FakeCareController {
                    Model = CreateModel(HiveBroodVitalityScreenState.OfflineReadOnly) };
                var window = host.AddComponent<HiveMapPremiumBuildingWindow>();
                window.BindNurseryController(controller);
                window.Show(BuildingCatalog.GetByBuildingType(BuildingTypes.Nursery),
                    "Nurserie", "Nursery", "", "", () => { }, () => { });
                Assert.That(FindButton(host, "Nourrir", "Feed").interactable, Is.False);
                Assert.That(FindButton(host, "Stabiliser", "Stabilize").interactable, Is.False);
                string text = string.Join("|", host.GetComponentsInChildren<TextMeshProUGUI>(true)
                    .Select(l => l.text).ToArray());
                Assert.That(text.Contains("Hors ligne") || text.Contains("Offline"), Is.True);

                window.Hide();
                controller.IsConfigured = false;
                window.BindNurseryController(controller);
                window.Show(BuildingCatalog.GetByBuildingType(BuildingTypes.Nursery),
                    "Nurserie", "Nursery", "", "", () => { }, () => { });
                Assert.That(FindButton(host, "Nourrir", "Feed").interactable, Is.False);
                Assert.That(FindButton(host, "Stabiliser", "Stabilize").interactable, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        [Test]
        public void ExistingNurseryUpgradeAndFullscreenInputGateRemainWired()
        {
            string script = File.ReadAllText(Path.Combine(Application.dataPath,
                "BeeKingdom/Playground/HiveMapNurseryBootstrap.cs"));
            Assert.That(script, Does.Contain("premiumWindow.Show("));
            Assert.That(script, Does.Contain("BindNurseryController("));
            Assert.That(script, Does.Contain("OpenConstructionOverlayForExternalHost"));
            Assert.That(script, Does.Contain("OverlayOpenForExternalHost = false;"));
            Assert.That(script, Does.Not.Contain("private void OnGUI()"));
            string gate = File.ReadAllText(Path.Combine(Application.dataPath,
                "BeeKingdom/Playground/HiveMapOverlayInputGateBootstrap.cs"));
            Assert.That(gate, Does.Contain(
                "HiveMapNurseryBootstrap.OverlayOpenForExternalHost"));
        }
    }
}
