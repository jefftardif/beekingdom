using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BeeKingdomWindowsInternalBuild
{
    public const string BuildDirectory = "Builds/Windows/Internal";
    public const string ExePath = BuildDirectory + "/BeeKingdom_Internal_Debug.exe";

    private static readonly string[] InternalTestScenes =
    {
        "Assets/Scenes/LivingHive.unity",
        "Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity",
        "Assets/Scenes/WorldMapMmoFullscreenFoundation.unity",
        "Assets/Scenes/WorldMapWave5Premium25x25Test.unity",
        "Assets/Scenes/SandboxPlayground.unity"
    };

    [MenuItem("Bee Kingdom/Build/Configure Windows Internal Debug")]
    public static void ConfigureWindowsInternalDebug()
    {
        ValidatePrerequisites();
        ConfigurePlayerSettings();
        ConfigureBuildSettings();
        Debug.Log("BeeKingdom Windows internal debug configuration is ready. Entry scene: " + InternalTestScenes[0]);
    }

    [MenuItem("Bee Kingdom/Build/Build Windows Internal Debug EXE")]
    public static void BuildWindowsInternalDebugExe()
    {
        ConfigureWindowsInternalDebug();
        Directory.CreateDirectory(BuildDirectory);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = InternalTestScenes,
            locationPathName = ExePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development | BuildOptions.AllowDebugging
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        if (summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException("BeeKingdom Windows internal debug build failed: " + summary.result);
        }

        if (!File.Exists(ExePath))
        {
            throw new FileNotFoundException("Windows build succeeded but executable was not found.", ExePath);
        }

        Debug.Log("BeeKingdom Windows internal debug build written: " + Path.GetFullPath(ExePath) + " (" + summary.totalSize + " bytes).");
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
    }

    private static void ValidatePrerequisites()
    {
        foreach (string scene in InternalTestScenes)
        {
            if (!File.Exists(scene)) throw new FileNotFoundException("Required Windows build scene is missing.", scene);
        }

        string windowsPlayerPath = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "WindowsStandaloneSupport");
        if (!Directory.Exists(windowsPlayerPath))
            throw new DirectoryNotFoundException("Unity Windows Build Support not found at: " + windowsPlayerPath);
    }
}
