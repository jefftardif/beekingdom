using System;
using System.Linq;
using BeeKingdom.Localization;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public static partial class HiveViewProductUiPresenter
    {
        private static string startupEntryPendingRoute;
        private static float startupReadyShownAt = -1f;

        // Pre-login splash can prepare a restored session. A fresh login uses the same
        // screen again before EnteredHive is set, so no blank HUD is ever released.
        private static bool DrawSynchronizedStartupScreen()
        {
            bool entering = !string.IsNullOrEmpty(startupEntryPendingRoute);
            if (startupLoadingScreenDone && !entering) return false;
            if (startupLoadingScreenStartRealtime < 0f) startupLoadingScreenStartRealtime = Time.realtimeSinceStartup;
            float elapsed = Time.realtimeSinceStartup - startupLoadingScreenStartRealtime;
            bool authenticating = MobileAccountSessionRuntimeBootstrap.StartupAuthenticationInitializing;
            bool official = MobileAccountSessionRuntimeBootstrap.StartupRequiresOfficialData;
            HiveStartupSnapshot state = MobileAccountSessionRuntimeBootstrap.StartupSnapshot;
            bool missingBinding = official && state == null && !authenticating;
            bool failed = state?.Failed == true || missingBinding || (authenticating && elapsed >= 45f);
            if (entering && !official && !authenticating)
            {
                CancelStartupEntry();
                return false;
            }
            bool dataReady = !authenticating && (!official || state?.Ready == true);
            if (dataReady && startupReadyShownAt < 0f) startupReadyShownAt = Time.realtimeSinceStartup;
            float completionNotice = state != null && state.Unavailable.Length > 0 ? 1.5f : .25f;
            if (dataReady && elapsed >= StartupLoadingScreenDurationSeconds &&
                Time.realtimeSinceStartup - startupReadyShownAt >= completionNotice)
            {
                startupLoadingScreenDone = true;
                string route = startupEntryPendingRoute;
                startupEntryPendingRoute = null;
                if (!string.IsNullOrEmpty(route))
                {
                    EnterHiveFromSplash(route);
                    return true;
                }
                return false;
            }

            int oldDepth = GUI.depth; Color oldColor = GUI.color;
            Color oldContent = GUI.contentColor; Color oldBackground = GUI.backgroundColor;
            bool oldEnabled = GUI.enabled;
            try
            {
                GUI.depth = -32750; GUI.color = GUI.contentColor = GUI.backgroundColor = Color.white; GUI.enabled = true;
                if (startupLoadingScreenTexture == null)
                    startupLoadingScreenTexture = Resources.Load<Texture2D>("PremiumBeeReference/splash_loading");
                Rect full = new Rect(0, 0, Screen.width, Screen.height);
                GUI.DrawTexture(full, Texture2D.blackTexture);
                if (startupLoadingScreenTexture != null) GUI.DrawTexture(full, startupLoadingScreenTexture, ScaleMode.ScaleAndCrop, false);
                Rect safe = Screen.safeArea;
                if (safe.width <= 0 || safe.height <= 0) safe = full;
                float bottom = Screen.height - safe.yMin;
                float width = Mathf.Min(safe.width - 32f, 760f);
                float x = safe.x + (safe.width - width) * .5f;
                float y = bottom - (failed ? 222f : 178f);
                Rect card = new Rect(x, y, width, failed ? 202f : 158f);
                GUI.color = new Color(.025f,.022f,.016f,.94f); GUI.DrawTexture(card, Texture2D.whiteTexture); GUI.color = Color.white;
                string title = failed ? BeeLocalization.Text("startup.sync.failed")
                    : authenticating ? BeeLocalization.Text("startup.sync.connecting")
                    : dataReady ? BeeLocalization.Text("startup.sync.ready")
                    : BeeLocalization.Text("startup.sync.preparing");
                var titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter,
                    wordWrap = true, fontSize = width < 480 ? 19 : 23, fontStyle = FontStyle.Bold };
                titleStyle.normal.textColor = new Color(1f,.80f,.37f);
                GUI.Label(new Rect(x+12,y+10,width-24,50), title,titleStyle);
                var body = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap=true, fontSize=width<480?14:17 };
                body.normal.textColor = new Color(.94f,.91f,.82f);
                string detail = failed ? BeeLocalization.Text("startup.sync.failed_hint")
                    : state == null ? BeeLocalization.Text("startup.sync.session")
                    : dataReady ? BeeLocalization.Text("startup.sync.checks") + " " + state.Completed + " / " + state.Total
                    : BeeLocalization.Text("startup.step." + state.WaitingFor) + " — " + state.Completed + " / " + state.Total;
                GUI.Label(new Rect(x+12,y+56,width-24,42),detail,body);
                Rect track = new Rect(x+20,y+102,width-40,10);
                GUI.color = new Color(.20f,.15f,.07f,1);GUI.DrawTexture(track,Texture2D.whiteTexture);
                float progress = state?.Progress ?? (dataReady ? 1f : .02f);
                GUI.color = new Color(.96f,.72f,.23f,1);
                if(progress>0) GUI.DrawTexture(new Rect(track.x,track.y,track.width*progress,track.height),Texture2D.whiteTexture);
                GUI.color = Color.white;
                string unavailable = state == null || state.Unavailable.Length==0 ? string.Empty :
                    BeeLocalization.Text("startup.sync.unavailable")+" "+string.Join(", ",state.Unavailable.Select(id=>BeeLocalization.Text("startup.step."+id)));
                var note = new GUIStyle(body) { fontSize=width<480?12:14 };
                GUI.Label(new Rect(x+12,y+118,width-24,failed?28:34),unavailable,note);
                if (failed)
                {
                    float buttonWidth=(width-52f)*.5f;
                    if (GUI.Button(new Rect(x+20,y+150,buttonWidth,44),BeeLocalization.Text("startup.sync.retry")))
                    {
                        MobileAccountSessionRuntimeBootstrap.RetryStartupSynchronization();
                        startupLoadingScreenStartRealtime=Time.realtimeSinceStartup;
                        startupReadyShownAt=-1f;
                    }
                    if (GUI.Button(new Rect(x+32+buttonWidth,y+150,buttonWidth,44),BeeLocalization.Text("startup.sync.back")))
                        CancelStartupEntry();
                }
                if(Event.current.isMouse || Event.current.type==EventType.ScrollWheel) Event.current.Use();
            }
            finally {GUI.depth=oldDepth;GUI.color=oldColor;GUI.contentColor=oldContent;GUI.backgroundColor=oldBackground;GUI.enabled=oldEnabled;}
            return true;
        }

        private static void CancelStartupEntry()
        {
            startupEntryPendingRoute=null;
            startupLoadingScreenDone=true;
            startupReadyShownAt=-1f;
            MobileAccountSessionRuntimeBootstrap.CloseGameplayForSignedOutSession();
            splashAuthGateState=SplashAuthGateState.Login;
        }

        private static bool DeferHiveEntryUntilSynchronized(string route)
        {
            if (!MobileAccountSessionRuntimeBootstrap.StartupRequiresOfficialData) return false;
            MobileAccountSessionRuntimeBootstrap.TryConfigureGameplayForActiveSession();
            if (MobileAccountSessionRuntimeBootstrap.StartupDataReady) return false;
            startupEntryPendingRoute=route;
            startupLoadingScreenDone=false;
            startupLoadingScreenStartRealtime=Time.realtimeSinceStartup;
            startupReadyShownAt=-1f;
            return true;
        }

        private static void PublishSynchronizedHeaderBeforeEntry()
        {
            GetResourceTotalsForExternalHost(out float honey,out float wax,out float pollen,out float bees,
                out float used,out float maximum,out float royalJelly);
            BeeKingdom.LivingHiveMenu.LivingHiveMenuHeaderData.SetLiveValues(
                Mathf.RoundToInt(honey),Mathf.RoundToInt(wax),Mathf.RoundToInt(pollen),Mathf.RoundToInt(bees),
                Mathf.RoundToInt(used),Mathf.RoundToInt(maximum),Mathf.RoundToInt(royalJelly));
        }
    }
}