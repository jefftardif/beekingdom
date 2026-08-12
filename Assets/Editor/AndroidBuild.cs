using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AndroidBuild
{
    private const string BuildDirectory = "Builds/Android";
    private const string BuildPath = BuildDirectory + "/BeeKingdom.apk";
    private const string RequiredEntryScene = "Assets/Scenes/SandboxPlayground.unity";
    private const string BuildInfoPath = "Assets/Resources/InternalBuildInfo.txt";
    private const string OrientationPolicy = "ARCH-157 official layouts - tablet landscape and phone portrait";

    [MenuItem("Bee Kingdom/Build Android APK")]
    public static void BuildAndroidApk()
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
        ConfigureInternalTestBuildBackend();
        ConfigureArch157LandscapePolicy();

        try
        {
            string[] scenes = ResolveBuildScenes();
            WriteInternalBuildInfo(scenes);
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = BuildPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                if (!File.Exists(BuildPath)) throw new FileNotFoundException("Android build succeeded but APK was not found.", BuildPath);
                Debug.Log($"Bee Kingdom Android APK written: {Path.GetFullPath(BuildPath)} ({summary.totalSize} bytes).");
                if (!Application.isBatchMode) EditorUtility.RevealInFinder(BuildPath);
                return;
            }

            throw new System.InvalidOperationException($"Bee Kingdom Android APK build failed: {summary.result}");
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
            Debug.Log("Restored Android project build settings after internal test APK build.");
        }
    }

    private static string[] ResolveBuildScenes()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToArray();

        if (scenes.Length == 0)
        {
            scenes = new[] { RequiredEntryScene };
        }

        if (scenes[0] != RequiredEntryScene)
        {
            throw new System.InvalidOperationException(
                $"Android internal build must start from the current SandboxPlayground runtime scene. First scene was '{scenes[0]}'.");
        }

        Debug.Log("Bee Kingdom Android build scenes: " + string.Join(", ", scenes));
        return scenes;
    }

    private static void WriteInternalBuildInfo(string[] scenes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(BuildInfoPath));
        string content =
            "Bee Kingdom APK tablette de test / build interne\n" +
            "Build UTC: " + System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") + "\n" +
            "Entry scene: " + RequiredEntryScene + "\n" +
            "Contains current SandboxPlayground runtime: true\n" +
            "ARCH-157 orientation policy: " + OrientationPolicy + "\n" +
            "Official tablet layout: landscape\n" +
            "Official phone layout: portrait\n" +
            "Default orientation for APK: AutoRotation\n" +
            "Allowed portrait in APK: true\n" +
            "Allowed landscape in APK: true\n" +
            "Server first: official gameplay requires server; offline is consultation/demo only\n" +
            "Scenes:\n- " + string.Join("\n- ", scenes) + "\n";

        File.WriteAllText(BuildInfoPath, content);
        AssetDatabase.ImportAsset(BuildInfoPath);
        Debug.Log("Updated internal build info asset for APK: " + BuildInfoPath);
    }

    private static void ConfigureEmbeddedAndroidTools()
    {
        string androidPlayerPath = Path.Combine(
            EditorApplication.applicationContentsPath,
            "PlaybackEngines",
            "AndroidPlayer"
        );

        SetAndroidToolProperty("useEmbeddedJdk", true);
        SetAndroidToolProperty("useEmbeddedSdk", true);
        SetAndroidToolProperty("useEmbeddedNdk", true);
        SetAndroidToolProperty("useEmbeddedGradle", true);

        SetAndroidToolProperty("jdkRootPath", Path.Combine(androidPlayerPath, "OpenJDK"));
        SetAndroidToolProperty("sdkRootPath", Path.Combine(androidPlayerPath, "SDK"));
        SetAndroidToolProperty("ndkRootPath", Path.Combine(androidPlayerPath, "NDK"));
        SetAndroidToolProperty("gradlePath", Path.Combine(androidPlayerPath, "Tools", "gradle"));

        Debug.Log("Configured embedded Android build tools for this Unity installation.");
    }

    private static void ConfigureInternalTestBuildBackend()
    {
        try
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.Mono2x);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            PlayerSettings.stripEngineCode = false;
            Debug.Log("Configured Android internal test build backend: Mono, ARMv7/ARM64, no engine stripping.");
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning("Unable to switch Android internal test build backend; keeping project defaults. " + exception.Message);
        }
    }

    private static void ConfigureArch157LandscapePolicy()
    {
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        Debug.Log("Configured Android orientation policy for internal APK: " + OrientationPolicy);
    }

    private static void SetAndroidToolProperty(string propertyName, object value)
    {
        System.Type settingsType = System.AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(assembly => assembly.GetType("UnityEditor.Android.AndroidExternalToolsSettings"))
            .FirstOrDefault(type => type != null);

        if (settingsType == null)
        {
            Debug.LogWarning("AndroidExternalToolsSettings type was not found.");
            return;
        }

        PropertyInfo property = settingsType.GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
        );

        if (property != null && property.CanWrite)
        {
            property.SetValue(null, value);
            Debug.Log($"Set Android external tool setting {propertyName}.");
        }
        else
        {
            Debug.LogWarning($"Android external tool setting {propertyName} was not found or is read-only.");
        }
    }
}
