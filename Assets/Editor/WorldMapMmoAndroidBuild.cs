using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WorldMapMmoAndroidBuild
{
    private const string BuildDirectory = "Builds/Android";
    private const string BuildPath = BuildDirectory + "/BeeKingdom.apk";
    private const string Step5ABuildDirectory = "Builds/Artifacts/WorldMapStep5AAndroidDevelopment";
    private const string Step5ABuildPath = Step5ABuildDirectory + "/BeeKingdom_WorldMapStep5A_Development.apk";
    private const string SandboxScene = "Assets/Scenes/SandboxPlayground.unity";
    private const string WorldMapScene = "Assets/Scenes/WorldMapMmoFullscreenFoundation.unity";
    private const string BuildInfoPath = "Assets/Resources/InternalBuildInfo.txt";

    public static void BuildWorldMapMmoAndroidApk()
    {
        ConfigureEmbeddedAndroidTools();
        Directory.CreateDirectory(BuildDirectory);
        if (File.Exists(BuildPath)) File.Delete(BuildPath);

        ScriptingImplementation originalBackend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
        AndroidArchitecture originalArchitectures = PlayerSettings.Android.targetArchitectures;
        bool originalStripEngineCode = PlayerSettings.stripEngineCode;
        UIOrientation originalDefaultOrientation = PlayerSettings.defaultInterfaceOrientation;
        bool originalPortrait = PlayerSettings.allowedAutorotateToPortrait;
        bool originalPortraitUpsideDown = PlayerSettings.allowedAutorotateToPortraitUpsideDown;
        bool originalLandscapeLeft = PlayerSettings.allowedAutorotateToLandscapeLeft;
        bool originalLandscapeRight = PlayerSettings.allowedAutorotateToLandscapeRight;

        try
        {
            if (!File.Exists(WorldMapScene)) throw new FileNotFoundException("World map scene is missing.", WorldMapScene);
            ConfigureInternalTestBuildBackend();
            ConfigureTabletAndPhoneOrientationPolicy();
            WriteInternalBuildInfo();

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { WorldMapScene },
                locationPathName = BuildPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                if (!File.Exists(BuildPath)) throw new FileNotFoundException("World map Android build succeeded but APK was not found.", BuildPath);
                Debug.Log($"Bee Kingdom world map Android APK written: {Path.GetFullPath(BuildPath)} ({summary.totalSize} bytes).");
                return;
            }

            throw new System.InvalidOperationException($"Bee Kingdom world map Android APK build failed: {summary.result}");
        }
        finally
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, originalBackend);
            PlayerSettings.Android.targetArchitectures = originalArchitectures;
            PlayerSettings.stripEngineCode = originalStripEngineCode;
            PlayerSettings.defaultInterfaceOrientation = originalDefaultOrientation;
            PlayerSettings.allowedAutorotateToPortrait = originalPortrait;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = originalPortraitUpsideDown;
            PlayerSettings.allowedAutorotateToLandscapeLeft = originalLandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeRight = originalLandscapeRight;
            Debug.Log("Restored Android project build settings after world map APK build.");
        }
    }

    public static void BuildWorldMapStep5ADevelopmentApk()
    {
        ConfigureEmbeddedAndroidTools();
        Directory.CreateDirectory(Step5ABuildDirectory);

        ScriptingImplementation originalBackend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
        AndroidArchitecture originalArchitectures = PlayerSettings.Android.targetArchitectures;
        bool originalStripEngineCode = PlayerSettings.stripEngineCode;
        UIOrientation originalDefaultOrientation = PlayerSettings.defaultInterfaceOrientation;
        bool originalPortrait = PlayerSettings.allowedAutorotateToPortrait;
        bool originalPortraitUpsideDown = PlayerSettings.allowedAutorotateToPortraitUpsideDown;
        bool originalLandscapeLeft = PlayerSettings.allowedAutorotateToLandscapeLeft;
        bool originalLandscapeRight = PlayerSettings.allowedAutorotateToLandscapeRight;

        try
        {
            if (!File.Exists(SandboxScene)) throw new FileNotFoundException("Sandbox scene is missing.", SandboxScene);
            if (!File.Exists(WorldMapScene)) throw new FileNotFoundException("World map scene is missing.", WorldMapScene);

            ConfigureStep5ADevelopmentBuildBackend();
            ConfigureTabletAndPhoneOrientationPolicy();

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { SandboxScene, WorldMapScene },
                locationPathName = Step5ABuildPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                if (!File.Exists(Step5ABuildPath))
                {
                    throw new FileNotFoundException("Step5A Android Development build succeeded but APK was not found.", Step5ABuildPath);
                }

                Debug.Log($"Bee Kingdom Step5A Android Development APK written: {Path.GetFullPath(Step5ABuildPath)} ({summary.totalSize} bytes).");
                return;
            }

            throw new System.InvalidOperationException($"Bee Kingdom Step5A Android Development APK build failed: {summary.result}");
        }
        finally
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, originalBackend);
            PlayerSettings.Android.targetArchitectures = originalArchitectures;
            PlayerSettings.stripEngineCode = originalStripEngineCode;
            PlayerSettings.defaultInterfaceOrientation = originalDefaultOrientation;
            PlayerSettings.allowedAutorotateToPortrait = originalPortrait;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = originalPortraitUpsideDown;
            PlayerSettings.allowedAutorotateToLandscapeLeft = originalLandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeRight = originalLandscapeRight;
            Debug.Log("Restored Android project build settings after Step5A Development APK build.");
        }
    }

    private static void WriteInternalBuildInfo()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(BuildInfoPath));
        string content =
            "Bee Kingdom APK tablette de test / build interne\n" +
            "Build UTC: " + System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") + "\n" +
            "Entry scene: " + WorldMapScene + "\n" +
            "Contains world map MMO fullscreen foundation: true\n" +
            "Default orientation for APK: AutoRotation\n" +
            "Allowed portrait in APK: true\n" +
            "Allowed landscape in APK: true\n" +
            "Server live claim: false\n" +
            "Official server progression: false\n" +
            "Official save/economy/army persistence: false\n" +
            "Scenes:\n- " + WorldMapScene + "\n";

        File.WriteAllText(BuildInfoPath, content);
        AssetDatabase.ImportAsset(BuildInfoPath);
        Debug.Log("Updated internal build info asset for world map APK: " + BuildInfoPath);
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

    private static void ConfigureInternalTestBuildBackend()
    {
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.Mono2x);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        PlayerSettings.stripEngineCode = false;
    }

    private static void ConfigureStep5ADevelopmentBuildBackend()
    {
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        PlayerSettings.stripEngineCode = false;
    }

    private static void ConfigureTabletAndPhoneOrientationPolicy()
    {
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
    }

    private static void SetAndroidToolProperty(string propertyName, object value)
    {
        System.Type settingsType = System.AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(assembly => assembly.GetType("UnityEditor.Android.AndroidExternalToolsSettings"))
            .FirstOrDefault(type => type != null);

        PropertyInfo property = settingsType?.GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
        );

        if (property != null && property.CanWrite) property.SetValue(null, value);
    }
}
