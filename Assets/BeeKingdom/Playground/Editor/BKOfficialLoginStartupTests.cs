using System;
using System.IO;
using System.Linq;
using BeeKingdom.Networking;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class BKOfficialLoginStartupTests
    {
        private static string Root => Directory.GetParent(Application.dataPath).FullName;
        private static string PlaygroundSource(string name) =>
            File.ReadAllText(Path.Combine(Application.dataPath,
                "BeeKingdom", "Playground", name));

        [Test]
        public void BuildAlwaysStartsWithOfficialLoginBeforeHiveAndWorldMap()
        {
            var enabled = EditorBuildSettings.scenes.Where(s => s.enabled)
                .Select(s => s.path).ToArray();
            Assert.That(enabled.Length, Is.GreaterThanOrEqualTo(3));
            Assert.That(enabled[0], Is.EqualTo(SplashDevelopmentSceneConfig.LoginScenePath));
            Assert.That(SplashDevelopmentSceneConfig.LoginScenePath,
                Is.Not.EqualTo(SplashDevelopmentSceneConfig.HiveMapScenePath));
            Assert.That(enabled, Does.Contain(SplashDevelopmentSceneConfig.HiveMapScenePath));
            Assert.That(enabled, Does.Contain(SplashDevelopmentSceneConfig.WorldMapScenePath));
        }

        [Test]
        public void EditorDefaultPlayRouteIsTheRealLoginNotTheOpenPreviewMap()
        {
            Assert.That(PlaygroundPlayModeStartScene.OfficialLoginStartSceneForProof,
                Is.EqualTo(SplashDevelopmentSceneConfig.LoginScenePath));
            string code = File.ReadAllText(Path.Combine(Application.dataPath,
                "BeeKingdom", "Playground", "Editor",
                "PlaygroundPlayModeStartScene.cs"));
            StringAssert.Contains("SessionState.GetBool(ExplicitDevelopmentStartKey, false)", code);
            StringAssert.Contains("ConfigurePlayModeStartScene(MainDemoScenePath);", code);
            StringAssert.Contains("OnPlayModeStateChanged", code);
            if (!SessionState.GetBool("BeeKingdom.Editor.ExplicitPreviewOnPlay", false))
                Assert.That(AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene),
                    Is.EqualTo(SplashDevelopmentSceneConfig.LoginScenePath));
        }

        [TestCase(MobileAccountSessionState.Authenticated, true, true)]
        [TestCase(MobileAccountSessionState.Authenticated, false, false)]
        [TestCase(MobileAccountSessionState.Offline, true, false)]
        [TestCase(MobileAccountSessionState.Authenticating, false, false)]
        [TestCase(MobileAccountSessionState.NotConfigured, false, false)]
        public void OnlyAuthenticatedServerAuthorityMayEnterHive(
            MobileAccountSessionState state, bool serverAuthority, bool expected)
        {
            Assert.That(HiveViewProductUiPresenter.
                OfficialLoginGrantsHiveEntryForProof(state, serverAuthority),
                Is.EqualTo(expected));
        }

        [Test]
        public void EmailAndGoogleCompletedLoginsAutoRouteThroughSyncedHiveEntry()
        {
            string code=PlaygroundSource("HiveViewProductUiPresenter.cs");
            StringAssert.Contains("splashAuthGateState = SplashAuthGateState.Login;",code);
            StringAssert.Contains(
                @"CompleteOfficialLoginIntoHive(""official_google_login"")",code);
            StringAssert.Contains(
                @"CompleteOfficialLoginIntoHive(""official_email_login"")",code);
            int start = code.IndexOf(
                "private static void CompleteOfficialLoginIntoHive(string route)",
                StringComparison.Ordinal);
            int end = code.IndexOf(
                "private static void PumpMobileAccountSessionOperations()",start,
                StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThan(0));
            Assert.That(end, Is.GreaterThan(start));
            string gate=code.Substring(start,end-start);
            StringAssert.Contains("OfficialLoginGrantsHiveEntryForProof",gate);
            StringAssert.Contains("EnterHiveFromSplash(route);",gate);
            StringAssert.Contains(
                "MobileAccountSessionRuntimeBootstrap.TryConfigureGameplayForActiveSession();",gate);
            // The already-tested synchronized entry method is preserved:
            StringAssert.Contains("DeferHiveEntryUntilSynchronized(route)",code);
            StringAssert.Contains("newAccountNeedsDisplayName = true;",code);
        }
    }
}