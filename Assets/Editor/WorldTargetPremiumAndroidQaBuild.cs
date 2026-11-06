using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Internal, independent QA APK for the premium World Map target action panel.
/// Never overwrites the alpha APK or changes the live server. Debug key only.
/// The original Android PlayerSettings are restored in finally.
/// </summary>
public static class WorldTargetPremiumAndroidQaBuild
{
    public const string Output = @"C:\BeeKingdom\qa\BeeKingdom_PremiumTarget_QA_20261106.apk";

    public static void BuildQaApk()
    {
        string[] scenes=EditorBuildSettings.scenes
            .Where(scene=>scene.enabled)
            .Select(scene=>scene.path)
            .Where(path=>!string.IsNullOrWhiteSpace(path))
            .ToArray();
        if(scenes.Length<1||
            !scenes.Contains("Assets/Scenes/WorldMapMmoFullscreenFoundation.unity")||
            !scenes.Contains("Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_HiveMap_Test.unity"))
            throw new InvalidOperationException("The real hive map and canonical World Map must be in the QA APK.");

        string engine=Path.Combine(EditorApplication.applicationContentsPath,"PlaybackEngines","AndroidPlayer");
        foreach(string folder in new[]{"SDK","NDK","OpenJDK","Tools/gradle"})
            if(!Directory.Exists(Path.Combine(engine,folder)))
                throw new DirectoryNotFoundException("Embedded Android "+folder+" is missing.");

        if(File.Exists(Output))
            throw new IOException("QA APK already exists; never overwrite an earlier test build: "+Output);
        Directory.CreateDirectory(Path.GetDirectoryName(Output));

        ScriptingImplementation previousBackend=PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
        AndroidArchitecture previousArchitectures=PlayerSettings.Android.targetArchitectures;
        bool previousKeystore=PlayerSettings.Android.useCustomKeystore;
        bool previousStripping=PlayerSettings.stripEngineCode;
        string previousId=PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
        string previousProduct=PlayerSettings.productName;
        UIOrientation previousOrientation=PlayerSettings.defaultInterfaceOrientation;
        bool previousPortrait=PlayerSettings.allowedAutorotateToPortrait;
        bool previousPortraitUpsideDown=PlayerSettings.allowedAutorotateToPortraitUpsideDown;
        bool previousLeft=PlayerSettings.allowedAutorotateToLandscapeLeft;
        bool previousRight=PlayerSettings.allowedAutorotateToLandscapeRight;
        try
        {
            // Unique app ID and non-production debug signing: safe to install
            // next to the existing alpha APK without replacing user data.
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,
                "com.bkdhoneystudio.beekingdom.premiumtargetqa");
            PlayerSettings.productName="BeeKingdom Premium Target QA";
            PlayerSettings.Android.useCustomKeystore=false;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.stripEngineCode=false;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait=true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            PlayerSettings.allowedAutorotateToLandscapeLeft=true;
            PlayerSettings.allowedAutorotateToLandscapeRight=true;
            BuildPlayerOptions options=new BuildPlayerOptions
            {
                scenes=scenes,
                locationPathName=Output,
                target=BuildTarget.Android,
                options=BuildOptions.Development|BuildOptions.AllowDebugging
            };
            Debug.Log("BK_TARGET_QA_ANDROID_START scenes="+scenes.Length+" output="+Output);
            BuildReport report=BuildPipeline.BuildPlayer(options);
            if(report==null||report.summary.result!=BuildResult.Succeeded||!File.Exists(Output))
                throw new InvalidOperationException("Premium Target QA Android build did not succeed: "+
                    (report==null?"no report":report.summary.result.ToString()));
            var file=new FileInfo(Output);
            Debug.Log("BK_TARGET_QA_ANDROID_PASS bytes="+file.Length+" output="+Output);
        }
        finally
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,previousBackend);
            PlayerSettings.Android.targetArchitectures=previousArchitectures;
            PlayerSettings.Android.useCustomKeystore=previousKeystore;
            PlayerSettings.stripEngineCode=previousStripping;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,previousId);
            PlayerSettings.productName=previousProduct;
            PlayerSettings.defaultInterfaceOrientation=previousOrientation;
            PlayerSettings.allowedAutorotateToPortrait=previousPortrait;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown=previousPortraitUpsideDown;
            PlayerSettings.allowedAutorotateToLandscapeLeft=previousLeft;
            PlayerSettings.allowedAutorotateToLandscapeRight=previousRight;
            Debug.Log("BK_TARGET_QA_ANDROID_ORIGINAL_SETTINGS_RESTORED");
        }
    }
}
