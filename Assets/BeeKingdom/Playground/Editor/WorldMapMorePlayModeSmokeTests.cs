using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BeeKingdom.LivingHiveMenu;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BeeKingdom.Playground.Editor
{
    /// <summary>
    /// Isolated smoke check for the actual WorldMap prefab and shared menu
    /// lifecycle. It does NOT connect to production accounts or mutate data.
    /// Batch Mode verifies state/raycast ownership, not pixel-perfect stacking.
    /// </summary>
    public sealed class WorldMapMorePlayModeSmokeTests
    {
        private readonly List<string> unexpectedErrors=new List<string>();

        private static bool IsMcpTransportCancellation(string message,string stack)
        {
            // Unity MCP connection to its own test collector is not gameplay.
            // Filter only this identifiable handshake error; preserve every
            // other log error, including networking/gameplay exceptions.
            if (message.Contains("McpManagerClientHub") &&
                message.Contains("Version handshake failed")) return true;
            return (message.Contains("OperationCanceledException")
                    || message.Contains("TaskCanceledException")
                    || message.Contains("HttpRequestException"))
                && (stack.Contains("com.IvanMurzak")
                    || stack.Contains("McpPlugin")
                    || stack.Contains("Unity.MCP"));
        }

        private void CaptureUnexpectedLogs(string message,string stack,LogType type)
        {
            if((type==LogType.Error||type==LogType.Assert||
                type==LogType.Exception)
                && !IsMcpTransportCancellation(message,stack))
                unexpectedErrors.Add(message);
        }

        [TearDown]
        public void RestoreLogAssertions()
        {
            Application.logMessageReceived -= CaptureUnexpectedLogs;
            LogAssert.ignoreFailingMessages=false;
        }

        private static string MenuDiagnostic(LivingHiveMenuCanvas m)
        {
            return "activeId=" + m.SharedPanelId
                + ";More=" + m.PanelShown("More")
                + ";Bag=" + m.PanelShown("Bag")
                + ";surface=" + m.State.SurfaceMode
                + ";time=" + Time.unscaledTime.ToString("F3")
                + ";frame=" + Time.frameCount;
        }

        private static T ReadInternal<T>(WorldMapSharedPanelForeground subject, string name)
        {
            var property = typeof(WorldMapSharedPanelForeground).GetProperty(
                name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, "Missing foreground probe " + name);
            return (T)property.GetValue(subject);
        }

        private static T ReadMenuField<T>(LivingHiveMenuCanvas subject, string name)
        {
            var field = typeof(LivingHiveMenuCanvas).GetField(
                name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing shared-menu field " + name);
            return (T)field.GetValue(subject);
        }

        private static string PanelAnimDiagnostic(LivingHiveMenuCanvas m,string id)
        {
            var animations=ReadMenuField<System.Collections.IDictionary>(m,"panelAnimStates");
            object anim=animations[id];
            if(anim==null)return "anim=missing";
            Type t=anim.GetType();
            bool closing=(bool)t.GetField("Closing").GetValue(anim);
            float started=(float)t.GetField("StartedAt").GetValue(anim);
            CanvasGroup group=(CanvasGroup)t.GetField("Group").GetValue(anim);
            return "animClosing="+closing+";animAge="+(Time.unscaledTime-started).ToString("F3")+
                   ";animAlpha="+group.alpha.ToString("F3")+
                   ";menuEnabled="+m.isActiveAndEnabled;
        }

        [UnityTest]
        public IEnumerator WorldMapMoreAndBagOpenAndCloseWithoutLosingMenuState()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return new EnterPlayMode();
            unexpectedErrors.Clear();
            Application.logMessageReceived += CaptureUnexpectedLogs;

            var loading=SceneManager.LoadSceneAsync(
                "WorldMapMmoFullscreenFoundation",LoadSceneMode.Single);
            Assert.That(loading,Is.Not.Null);
            yield return loading;
            for(int i=0;i<4;i++)yield return null;

            var map=UnityEngine.Object.FindAnyObjectByType<WorldMapMmoFullscreenFoundationBootstrap>();
            Assert.That(map,Is.Not.Null,"Official map bootstrap must exist");
            var overlay=map.GetComponent<WorldMapSharedPanelForeground>();
            Assert.That(overlay,Is.Not.Null,"Foreground Plus renderer is required");
            var menu=map.GetComponentInChildren<LivingHiveMenuCanvas>(true);
            Assert.That(menu,Is.Not.Null,"Shared menu must be attached to map");

            try
            {
                menu.CloseSharedPanel();
                yield return null;
                Assert.That(menu.SharedPanelOpen,Is.False,
                    "Bag-close diagnostic: panelId="+menu.SharedPanelId+
                    ", BagVisible="+menu.PanelShown("Bag")+
                    ", MoreVisible="+menu.PanelShown("More")+
                    ", Time.unscaledTime="+Time.unscaledTime.ToString("F3"));

                menu.ToggleSharedPanel("More");
                yield return null;
                Assert.That(menu.SharedPanelOpen,Is.True);
                Assert.That(menu.SharedPanelId,Is.EqualTo("More"));
                Assert.That(menu.PanelShown("More"),Is.True);
                Assert.That(ReadInternal<bool>(overlay, "IsReadyForProof"),Is.True);
                Assert.That(ReadInternal<string>(overlay, "CurrentPanelForProof"),Is.EqualTo("More"));

                menu.CloseSharedPanel();
                Assert.That(menu.SharedPanelId,Is.Empty,
                    "Closing must clear the logical menu before fade-out.");
                yield return new WaitForSecondsRealtime(.2f);
                Assert.That(menu.SharedPanelOpen,Is.False,
                    "Visual close should finish within the 0.12-second fade.");
                Assert.That(menu.PanelShown("More"),Is.False);

                menu.ToggleSharedPanel("Bag");
                yield return null;
                Assert.That(menu.SharedPanelOpen,Is.True);
                Assert.That(menu.SharedPanelId,Is.EqualTo("Bag"));
                Assert.That(ReadInternal<string>(overlay, "CurrentPanelForProof"),Is.EqualTo("Bag"));

                menu.CloseSharedPanel();
                Assert.That(menu.SharedPanelId,Is.Empty);
                yield return new WaitForSecondsRealtime(.2f);
                Assert.That(menu.SharedPanelOpen,Is.False,
                    "Panel must close after fade; " + MenuDiagnostic(menu));
                Assert.That(menu.PanelShown("Bag"),Is.False);
                // No invisible popup may keep the map or Chat input blocked.
                var canvas=ReadMenuField<Canvas>(menu,"canvas");
                var raycaster=ReadMenuField<UnityEngine.UI.GraphicRaycaster>(
                    menu,"raycaster");
                var ownership=ReadMenuField<CanvasGroup>(menu,"inputOwnershipGroup");
                Assert.That(canvas.enabled,Is.True,"HUD canvas must return");
                Assert.That(raycaster.enabled,Is.True,"Chat/menu input must return");
                Assert.That(ownership.blocksRaycasts,Is.True,
                    "A dismissed popup cannot retain pointer ownership");
                // Repeated toggle / close regression gate: catches timing races
                // when switching between shared More and Bag panels.
                for (int cycle = 0; cycle < 12; cycle++)
                {
                    string target = cycle % 2 == 0 ? "More" : "Bag";
                    menu.ToggleSharedPanel(target);
                    yield return null;
                    Assert.That(menu.SharedPanelId, Is.EqualTo(target),
                        "Unexpected menu state at cycle "+cycle);
                    menu.CloseSharedPanel();
                    Assert.That(menu.SharedPanelId, Is.Empty);
                    yield return new WaitForSecondsRealtime(.22f);
                    Assert.That(menu.SharedPanelOpen, Is.False,
                        "Panel failed to close in cycle "+cycle+" ("+target+"): BagVisible="+
                        menu.PanelShown("Bag")+", MoreVisible="+menu.PanelShown("More")+";"+MenuDiagnostic(menu)+";"+PanelAnimDiagnostic(menu,target));
                }
                Assert.That(unexpectedErrors,Is.Empty,
                    "Unexpected gameplay errors during More/Bag lifecycle.");
            }
            finally
            {
                if(menu!=null)menu.CloseSharedPanel();
                Application.logMessageReceived -= CaptureUnexpectedLogs;
                // TearDown restores strict logging after ExitPlayMode. MCP
                // transport cancellation during scene shutdown is not a
                // gameplay failure and must not abort the test itself.
            }
            yield return new ExitPlayMode();
        }
    }
}
