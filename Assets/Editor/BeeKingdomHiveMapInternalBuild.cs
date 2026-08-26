using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Standalone Windows build of the canonical HiveMap test scene, used to verify whether a
// behaviour reported in the Unity Editor (e.g. the Research building freeze under M016E-CL)
// also reproduces in a built player, or is specific to the Editor's own internal machinery.
public static class BeeKingdomHiveMapInternalBuild
{
    public const string BuildDirectory = "Builds/Windows/HiveMap";
    public const string ExePath = BuildDirectory + "/BeeKingdom_HiveMap_Debug.exe";
    private const string EntryScene = "Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_HiveMap_Test.unity";

    [MenuItem("Bee Kingdom/Build/Build HiveMap Windows Debug EXE")]
    public static void BuildHiveMapWindowsDebugExe()
    {
        if (!File.Exists(EntryScene))
            throw new FileNotFoundException("HiveMap entry scene is missing.", EntryScene);

        Directory.CreateDirectory(BuildDirectory);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { EntryScene },
            locationPathName = ExePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development | BuildOptions.AllowDebugging
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        if (summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("BeeKingdom HiveMap Windows debug build failed: " + summary.result);

        if (!File.Exists(ExePath))
            throw new FileNotFoundException("HiveMap Windows build succeeded but executable was not found.", ExePath);

        Debug.Log("BeeKingdom HiveMap Windows debug build written: " + Path.GetFullPath(ExePath) + " (" + summary.totalSize + " bytes).");
    }
}
