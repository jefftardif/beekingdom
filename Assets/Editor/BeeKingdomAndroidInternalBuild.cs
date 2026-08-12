using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BeeKingdomAndroidInternalBuild
{
    public const string BuildDirectory = "Builds/Android/Internal";
    public const string ApkPath = BuildDirectory + "/BeeKingdom_Internal_Debug.apk";

    private static readonly string[] InternalTestScenes =
    {
        "Assets/Scenes/LivingHive.unity",
        "Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity",
        "Assets/Scenes/WorldMapMmoFullscreenFoundation.unity",
        "Assets/Scenes/WorldMapWave5Premium25x25Test.unity",
        "Assets/Scenes/SandboxPlayground.unity"
    };

    [MenuItem("Bee Kingdom/Build/Configure Android Internal Debug")]
    public static void ConfigureAndroidInternalDebug()
    {
        ValidatePrerequisites();
        ConfigureEmbeddedAndroidTools();
        ConfigurePlayerSettings();
        ConfigureBuildSettings();
        Debug.Log("BeeKingdom Android internal debug configuration is ready. Entry scene: " + InternalTestScenes[0]);
    }

    [MenuItem("Bee Kingdom/Build/Build Android Internal Debug APK")]
    public static void BuildAndroidInternalDebugApk()
    {
        ConfigureAndroidInternalDebug();
        Directory.CreateDirectory(BuildDirectory);
        if (File.Exists(ApkPath)) File.Delete(ApkPath);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = InternalTestScenes,
            locationPathName = ApkPath,
            target = BuildTarget.Android,
            options = BuildOptions.Development | BuildOptions.AllowDebugging
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        if (summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException("BeeKingdom Android internal debug APK build failed: " + summary.result);
        }

        if (!File.Exists(ApkPath))
        {
            throw new FileNotFoundException("Android build succeeded but APK was not found.", ApkPath);
        }

        Debug.Log("BeeKingdom Android internal debug APK written: " + Path.GetFullPath(ApkPath) + " (" + summary.totalSize + " bytes).");
    }

    private static void ConfigureBuildSettings()
    {
        EditorBuildSettings.scenes = InternalTestScenes
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();
    }

    private static void ConfigurePlayerSettings()
    {
        PlayerSettings.companyName = "BKD Honey Studio";
        PlayerSettings.productName = "BeeKingdom";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.bkdhoneystudio.beekingdom");
        PlayerSettings.Android.bundleVersionCode = Math.Max(1, PlayerSettings.Android.bundleVersionCode);
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        PlayerSettings.Android.useCustomKeystore = false;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.Mono2x);
        PlayerSettings.stripEngineCode = false;

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        PlayerSettings.Android.startInFullscreen = true;
        PlayerSettings.Android.renderOutsideSafeArea = true;
    }

    private static void ValidatePrerequisites()
    {
        foreach (string scene in InternalTestScenes)
        {
            if (!File.Exists(scene)) throw new FileNotFoundException("Required Android build scene is missing.", scene);
        }

        string androidPlayerPath = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer");
        RequireDirectory(androidPlayerPath, "Unity Android Build Support");
        RequireDirectory(Path.Combine(androidPlayerPath, "SDK"), "Unity embedded Android SDK");
        RequireDirectory(Path.Combine(androidPlayerPath, "NDK"), "Unity embedded Android NDK");
        RequireDirectory(Path.Combine(androidPlayerPath, "OpenJDK"), "Unity embedded OpenJDK");
        RequireDirectory(Path.Combine(androidPlayerPath, "Tools", "gradle"), "Unity embedded Gradle");
    }

    private static void RequireDirectory(string path, string label)
    {
        if (!Directory.Exists(path)) throw new DirectoryNotFoundException(label + " not found at: " + path);
    }

    private static void ConfigureEmbeddedAndroidTools()
    {
        string androidPlayerPath = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer");
        SetAndroidToolProperty("useEmbeddedJdk", true);
        SetAndroidToolProperty("useEmbeddedSdk", true);
        SetAndroidToolProperty("useEmbeddedNdk", true);
        SetAndroidToolProperty("useEmbeddedGradle", true);
        SetAndroidToolProperty("jdkRootPath", Path.Combine(androidPlayerPath, "OpenJDK"));
        SetAndroidToolProperty("sdkRootPath", Path.Combine(androidPlayerPath, "SDK"));
        SetAndroidToolProperty("ndkRootPath", Path.Combine(androidPlayerPath, "NDK"));
        SetAndroidToolProperty("gradlePath", Path.Combine(androidPlayerPath, "Tools", "gradle"));
    }

    private static void SetAndroidToolProperty(string propertyName, object value)
    {
        Type settingsType = AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(assembly => assembly.GetType("UnityEditor.Android.AndroidExternalToolsSettings"))
            .FirstOrDefault(type => type != null);

        PropertyInfo property = settingsType?.GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        if (property != null && property.CanWrite) property.SetValue(null, value);
    }
}
