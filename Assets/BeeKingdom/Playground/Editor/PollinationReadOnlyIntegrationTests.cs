using System;
using System.IO;
using BeeKingdom.Networking;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class PollinationReadOnlyIntegrationTests
    {
        private static string Source(string file) =>
            File.ReadAllText(Path.Combine(Application.dataPath,
                "BeeKingdom", file));

        [Test]
        public void OfficialBuildExposesAuthenticatedReadOnlyPollination()
        {
            const string asset = "Assets/BeeKingdom/Playground/Resources/BeeKingdom/MobileAccountSessionRuntime.asset";
            var configuration = AssetDatabase.LoadAssetAtPath<MobileAccountSessionRuntimeConfiguration>(asset);
            Assert.That(configuration, Is.Not.Null);
            Assert.That(configuration.OfficialAccountsEnabled, Is.True);
            Assert.That(configuration.OfficialGameplayEnabled, Is.True);
            Assert.That(configuration.PollinationFeatureEnabled, Is.True);

            string binder = Source("Playground/MobileAccountSessionRuntimeBootstrap.Pollination.cs");
            StringAssert.Contains("mutationsEnabled: false", binder);
            StringAssert.Contains("authenticatedAuthority", binder);
            StringAssert.Contains("ClosePollinationForSignedOutSession()", binder);
        }

        [Test]
        public void ThreeServerMutationsRequireExplicitWritePermission()
        {
            string api = Source("Networking/PollinationClient.cs");
            foreach (string signature in new[] {
                "public async Task<RemoteCivilCohortResult> StartCohortAsync(",
                "public async Task<RemoteCivilExpeditionResult> StartExpeditionAsync(",
                "public async Task<RemoteFloralProjectCreateResult> CreateProjectAsync("
            })
            {
                int index = api.IndexOf(signature, StringComparison.Ordinal);
                Assert.That(index, Is.GreaterThanOrEqualTo(0), signature);
                int next = api.IndexOf("RequireMutationsEnabled();", index, StringComparison.Ordinal);
                Assert.That(next - index, Is.InRange(0, 450), signature);
            }
            StringAssert.Contains("pollination.read_only", api);

            string model = Source("Playground/PollinationPresentation.cs");
            Assert.That(model.Split(new[] { "MutationsAllowed && State ==" },
                StringSplitOptions.None).Length - 1, Is.EqualTo(3));
        }

        [Test]
        public void WorldMapKeepsPollinationModalAboveMapAndAwayFromMapClicks()
        {
            string world = Source("Playground/WorldMapMmoFullscreenFoundationBootstrap.cs");
            StringAssert.Contains("PollinationWorldMapOverlay.IsModalOpen", world);
            StringAssert.Contains("PollinationWorldMapOverlay.IsEntryHotspot", world);
            string overlay = Source("Playground/PollinationWorldMapOverlay.cs");
            StringAssert.Contains("WorldMapHudLayout.CurrentButton(3)", overlay);
            StringAssert.Contains("RuntimeInitializeOnLoadMethod", overlay);
        }
    }
}
