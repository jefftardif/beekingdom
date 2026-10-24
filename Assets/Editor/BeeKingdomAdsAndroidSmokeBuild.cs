using System;
using System.IO;
using BeeKingdom.Monetization;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BeeKingdomAdsAndroidSmokeBuild
{
    public static void Build()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new InvalidOperationException("BK_ADS_SMOKE_REQUIRES_ANDROID_TARGET");

        string dependencyXml = Path.Combine(Application.dataPath, "LevelPlay", "Editor",
            "ISAdMobAdapterDependencies.xml");
        if (!File.Exists(dependencyXml) ||
            !File.ReadAllText(dependencyXml).Contains("com.unity3d.ads-mediation:admob-adapter:5.19.0"))
            throw new InvalidOperationException("BK_ADS_GOOGLE_ADAPTER_MISSING");

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string entryScene = "Assets/Scenes/LevelPlayAndroidSmokeTemporary.unity";
        string apkPath = Path.Combine(projectRoot, "Builds", "Android",
            "BeeKingdom-LevelPlay-Smoke.apk");

        Directory.CreateDirectory(Path.GetDirectoryName(apkPath));
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("BeeKingdom.RewardedSmoke");
        root.AddComponent<BeeKingdomLevelPlayRewardedBridge>();
        EditorSceneManager.SaveScene(scene, entryScene);
        AssetDatabase.Refresh();

        // Test build only, not for Play Store submission.
        PlayerSettings.Android.useCustomKeystore = false;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.Mono2x);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

        try
        {
            Debug.Log("BK_ADS_SMOKE_BEGIN package=9.5.1 adapter=5.34.0.0");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { entryScene },
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };
            BuildReport result = BuildPipeline.BuildPlayer(options);
            if (result.summary.result != BuildResult.Succeeded || !File.Exists(apkPath))
                throw new InvalidOperationException("BK_ADS_ANDROID_BUILD_FAILED=" + result.summary.result);

            string evidence = "RESULT=PASS" + Environment.NewLine +
                "APK=" + apkPath + Environment.NewLine +
                "BYTES=" + new FileInfo(apkPath).Length + Environment.NewLine +
                "LEVELPLAY_APP_KEY=" + BeeKingdomLevelPlayRewardedBridge.LevelPlayAppKey +
                Environment.NewLine;
            File.WriteAllText(Path.Combine(projectRoot, "Builds", "Android", "BK-Ads-Smoke-Result.txt"),
                evidence);
            Debug.Log("BK_ADS_ANDROID_BUILD_PASS " + apkPath);
        }
        finally
        {
            AssetDatabase.DeleteAsset(entryScene);
        }
    }
}
