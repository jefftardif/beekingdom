using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEditor;

namespace BeeKingdom.Playground.Editor
{
    /// <summary>
    /// Uses the actual canonical World Map scene, not a mock map.
    /// Verifies target lifecycle and map hit ownership without production writes.
    /// </summary>
    public sealed class WorldTargetPremiumPlayModeTests
    {
        private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        private bool initialOptionsEnabled;
        private EnterPlayModeOptions initialOptions;

        private static object Field(object obj,string name)
        {
            var info=obj.GetType().GetField(name,Flags);
            Assert.That(info,Is.Not.Null,"Missing WorldMap field "+name);
            return info.GetValue(obj);
        }
        private static object Invoke(object obj,string method,params object[] args)
        {
            var info=obj.GetType().GetMethod(method,Flags);
            Assert.That(info,Is.Not.Null,"Missing map method "+method);
            return info.Invoke(obj,args);
        }
        private static string FirstId(System.Collections.IList items)
        {
            Assert.That(items,Is.Not.Null);
            Assert.That(items.Count,Is.GreaterThan(0),
                "The world scene must contain a target to check");
            object first=items[0];
            var field=first.GetType().GetField("Id",
                BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            Assert.That(field,Is.Not.Null);
            return (string)field.GetValue(first);
        }

        private static object HitKind(string name)
        {
            Type type=typeof(WorldMapMmoFullscreenFoundationBootstrap).Assembly
                .GetType("BeeKingdom.Playground.WorldMapHitKind");
            Assert.That(type,Is.Not.Null,"Missing production hit kind");
            return Enum.Parse(type,name);
        }
        [TearDown]
        public void RestoreLogPolicy()
        {
            LogAssert.ignoreFailingMessages=false;
            EditorSettings.enterPlayModeOptions=initialOptions;
            EditorSettings.enterPlayModeOptionsEnabled=initialOptionsEnabled;
        }

        [UnityTest]
        public IEnumerator ActualWorldMapSceneOpensAndDismissesEnemyAndResourceSafely()
        {
            // Ignore unrelated Unity MCP transport handshake failures; this
            // test makes NO real player/server mutations.
            LogAssert.ignoreFailingMessages=true;
            initialOptionsEnabled=EditorSettings.enterPlayModeOptionsEnabled;
            initialOptions=EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled=true;
            EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            yield return new EnterPlayMode();
            var loading=SceneManager.LoadSceneAsync(
                "WorldMapMmoFullscreenFoundation",LoadSceneMode.Single);
            Assert.That(loading,Is.Not.Null);
            yield return loading;
            for(int i=0;i<6;i++)yield return null;

            var map=UnityEngine.Object.FindAnyObjectByType<
                WorldMapMmoFullscreenFoundationBootstrap>();
            Assert.That(map,Is.Not.Null,
                "Official World Map bootstrap must be present");
            var resources=(System.Collections.IList)Field(map,"resources");
            var enemies=(System.Collections.IList)Field(map,"bestiary");
            string resourceId=FirstId(resources);
            string enemyId=FirstId(enemies);
            var selection=(WorldTargetPanelSelection)Field(map,"premiumTarget");
            Assert.That(selection.Visible,Is.False,
                "Initial map should not obscure camera with a target modal");
            Invoke(map,"OpenPremiumTarget",HitKind("Resource"),resourceId);
            yield return null;
            Assert.That(selection.IsTarget(WorldTargetPanelMode.Resource,resourceId),
                Is.True,"Resource click opens resource-only modal");
            Assert.That(selection.IsTarget(WorldTargetPanelMode.Enemy,enemyId),
                Is.False,"Cannot show both target types");
            Assert.That((bool)Invoke(map,"IsPointerOverFixedUi",new Vector2(
                    Screen.width*.5f,Screen.height*.5f)),
                Is.True,"Modal must own map taps");

            Invoke(map,"OpenPremiumTarget",HitKind("Bestiary"),enemyId);
            yield return null;
            Assert.That(selection.IsTarget(WorldTargetPanelMode.Enemy,enemyId),
                Is.True,"Enemy click replaces previous resource modal");
            Assert.That(selection.IsTarget(WorldTargetPanelMode.Resource,resourceId),
                Is.False,"Resource modal must be dismissed on enemy click");
            Assert.That((bool)Invoke(map,"IsPointerOverFixedUi",new Vector2(12f,12f)),
                Is.True,"Clicking behind enemy modal cannot pan the map");
            Rect popup=(Rect)Invoke(map,"PremiumTargetRect",false);
            Assert.That(popup.width,Is.GreaterThan(0f));
            Assert.That(popup.height,Is.GreaterThan(0f));

            Invoke(map,"ClosePremiumTarget");
            yield return null;
            Assert.That(selection.Visible,Is.False);
            Invoke(map,"OpenPremiumTarget",HitKind("None"),"unrelated");
            Assert.That(selection.Visible,Is.False,
                "Non-actionable map selection must never open popup");
            yield return new ExitPlayMode();
        }
    }
}
