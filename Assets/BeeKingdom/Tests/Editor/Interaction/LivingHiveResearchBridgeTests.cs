using System;
using System.Linq;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.Core.Integration;
using BeeKingdom.LivingHiveMenu;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Tests.Editor.Interaction
{
    // M016E-CL: locks the assembly-safe boundary between BeeKingdom.LivingHiveMenu and the
    // official/authenticated Research routing owned by the default Playground assembly.
    // LivingHiveMenu must never reference HiveViewProductUiPresenter or
    // MobileAccountSessionRuntimeBootstrap directly - LivingHiveResearchBridge
    // (BeeKingdom.Core.Integration) is the only legal path, mirroring
    // LivingHiveActivitiesBridge/LivingHiveSettingsBridge.
    public class LivingHiveResearchBridgeTests
    {
        [TearDown]
        public void ResetBridge()
        {
            // Leave the bridge with no handlers between tests, same as its real state before
            // any Playground bootstrap has run (e.g. in EditMode, or before HiveMapResearchBootstrap.Start()).
            LivingHiveResearchBridge.SetHandlers(null, null, null);
        }

        [Test]
        public void UnconfiguredBridgeBehavesHonestly()
        {
            // No SetHandlers call yet (or reset to null) - must report false/false and never throw,
            // rather than assuming a Playground bootstrap has already wired it.
            Assert.That(LivingHiveResearchBridge.IsOfficialOpen, Is.False);
            Assert.That(LivingHiveResearchBridge.IsOfficialAvailable, Is.False);
            Assert.DoesNotThrow(() => LivingHiveResearchBridge.OpenOfficialOverlay());
        }

        [Test]
        public void SetHandlersWiresAllThreeDelegatesIndependently()
        {
            bool open = false;
            bool available = true;
            int openCallCount = 0;

            LivingHiveResearchBridge.SetHandlers(() => open, () => available, () => openCallCount++);

            Assert.That(LivingHiveResearchBridge.IsOfficialOpen, Is.False);
            Assert.That(LivingHiveResearchBridge.IsOfficialAvailable, Is.True);

            open = true;
            Assert.That(LivingHiveResearchBridge.IsOfficialOpen, Is.True, "IsOfficialOpen must re-query the delegate, not cache the first read.");

            LivingHiveResearchBridge.OpenOfficialOverlay();
            Assert.That(openCallCount, Is.EqualTo(1));
        }

        [Test]
        public void HostAlwaysUsesLocalPreviewEvenWhenBridgeReportsAvailable()
        {
            // M016E-CL: the official overlay reproducibly freezes the Unity Editor main thread
            // on open (traced to an Editor-internal stall, likely SentinelOne intercepting
            // Editor file I/O - not a bug in this code), so the host routes to the
            // local-preview window unconditionally until a fix is confirmed.
            var windowGo = new GameObject("ResearchWindowTest");
            try
            {
                var window = windowGo.AddComponent<LivingHiveResearchWindow>();
                window.Build();
                var host = new LivingHiveResearchHost(window);
                host.Register();
                var selection = new BuildingSelectionService();
                host.Attach(selection);

                int officialOpenCalls = 0;
                LivingHiveResearchBridge.SetHandlers(() => false, () => true, () => officialOpenCalls++);

                BuildingDefinition research = BuildingCatalog.GetByBuildingType(BuildingTypes.Research);
                selection.NotifyClicked(research);

                Assert.That(officialOpenCalls, Is.EqualTo(0), "The official overlay must never be opened while it is disabled.");
                Assert.That(window.IsOpen, Is.True, "Must always fall back to the local-preview window.");
            }
            finally
            {
                BuildingWindowRouter.Host = null;
                UnityEngine.Object.DestroyImmediate(windowGo);
            }
        }

        [Test]
        public void HostFallsBackToLocalPreviewWindowWhenNoOfficialSessionIsAvailable()
        {
            var windowGo = new GameObject("ResearchWindowTest");
            try
            {
                var window = windowGo.AddComponent<LivingHiveResearchWindow>();
                window.Build();
                var host = new LivingHiveResearchHost(window);
                host.Register();
                var selection = new BuildingSelectionService();
                host.Attach(selection);

                int officialOpenCalls = 0;
                LivingHiveResearchBridge.SetHandlers(() => false, () => false, () => officialOpenCalls++);

                BuildingDefinition research = BuildingCatalog.GetByBuildingType(BuildingTypes.Research);
                selection.NotifyClicked(research);

                Assert.That(officialOpenCalls, Is.EqualTo(0), "No official session available - must never call the official open handler.");
                Assert.That(window.IsOpen, Is.True, "Must fall back to the existing M011 modal-safe local-preview fullscreen window.");
            }
            finally
            {
                BuildingWindowRouter.Host = null;
                UnityEngine.Object.DestroyImmediate(windowGo);
            }
        }

        [Test]
        public void HostAlwaysUsesLocalPreviewEvenWhenBridgeReportsOfficialAlreadyOpen()
        {
            var windowGo = new GameObject("ResearchWindowTest");
            try
            {
                var window = windowGo.AddComponent<LivingHiveResearchWindow>();
                window.Build();
                var host = new LivingHiveResearchHost(window);
                host.Register();
                var selection = new BuildingSelectionService();
                host.Attach(selection);

                int officialOpenCalls = 0;
                LivingHiveResearchBridge.SetHandlers(() => true, () => true, () => officialOpenCalls++);

                BuildingDefinition research = BuildingCatalog.GetByBuildingType(BuildingTypes.Research);
                selection.NotifyClicked(research);

                Assert.That(officialOpenCalls, Is.EqualTo(0));
                Assert.That(window.IsOpen, Is.True);
            }
            finally
            {
                BuildingWindowRouter.Host = null;
                UnityEngine.Object.DestroyImmediate(windowGo);
            }
        }

        [Test]
        public void LivingHiveMenuAssemblyNeverReferencesTheDefaultPlaygroundAssembly()
        {
            // The concrete regression this mission fixes: OC's broken change added
            // "using BeeKingdom.Playground;" to LivingHiveResearchHost.cs, which cannot compile
            // because Unity does not allow a custom .asmdef assembly to reference the implicit
            // default assembly (Assembly-CSharp). Assert it structurally so a future edit that
            // reintroduces a direct reference fails a test before it fails Unity's compiler.
            System.Reflection.Assembly menuAssembly = typeof(LivingHiveResearchHost).Assembly;
            string[] referencedNames = menuAssembly.GetReferencedAssemblies().Select(a => a.Name).ToArray();

            Assert.That(referencedNames, Has.None.Contains("Assembly-CSharp"),
                "BeeKingdom.LivingHiveMenu must never reference Assembly-CSharp/Playground directly - use LivingHiveResearchBridge (or another BeeKingdom.Core.Integration bridge) instead.");
        }
    }
}
