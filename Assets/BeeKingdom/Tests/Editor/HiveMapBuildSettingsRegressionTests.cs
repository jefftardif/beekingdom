using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace BeeKingdom.Tests.Editor
{
    // M043D-CL: guards the exact regression the CEO hit - LivingHive silently became build index 0
    // / enabled, and the real production HiveMap (Environment2D5D_HiveMap_Test.unity) disappeared
    // from EditorBuildSettings entirely. Known-good baseline restored from the last committed state
    // (commit ed1512e "Align build configuration with HiveMap" / Docs/AI/Missions/
    // M012-OC-HiveMap-Build-Configuration-Alignment.md). Tests scene GUID, not just display name/path
    // (a renamed-but-same-file scenario would still be caught; a different file at the same path
    // would not silently pass).
    public sealed class HiveMapBuildSettingsRegressionTests
    {
        // GUIDs from ProjectSettings/EditorBuildSettings.asset / the M012-OC baseline - stable
        // identifiers for the .unity scene asset itself, independent of its path or display name.
        private const string HiveMapGuid = "7fbab56df58e3dd498dc6b8dd19b10d7";
        private const string LivingHiveGuid = "d71c61733e1eecb4a9806ef7263fe85a";
        private const string WorldMapGuid = "1805d1342de3a89429d15c76bdf2a35a";

        [Test]
        public void ProductionHiveMapIsPresentAndEnabledInBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            EditorBuildSettingsScene hiveMap = scenes.FirstOrDefault(s => s.guid.ToString().Equals(HiveMapGuid, System.StringComparison.OrdinalIgnoreCase));

            Assert.That(hiveMap, Is.Not.Null,
                "Production HiveMap (Environment2D5D_HiveMap_Test.unity) is missing from EditorBuildSettings entirely - this is exactly the M043D regression.");
            Assert.That(hiveMap.enabled, Is.True, "Production HiveMap must be enabled in Build Settings.");
        }

        [Test]
        public void FirstEnabledBuildSceneIsNeverLivingHive()
        {
            EditorBuildSettingsScene firstEnabled = EditorBuildSettings.scenes.FirstOrDefault(s => s.enabled);

            Assert.That(firstEnabled, Is.Not.Null, "Build Settings has no enabled scenes at all.");
            Assert.That(firstEnabled.guid.ToString(), Is.Not.EqualTo(LivingHiveGuid).IgnoreCase,
                "LivingHive.unity is the first enabled Build Settings scene - it would launch as scene 0 in any build, and Editor Play Mode's automatic playModeStartScene fallback " +
                "(PlaygroundPlayModeStartScene.cs) treats the first-enabled/currently-open scene as authoritative. LivingHive is legacy/dev-only and must never be the apparent production hive.");
        }

        [Test]
        public void LivingHiveIsPresentButDisabled()
        {
            // Not deleted (still needed for QA/editor access per M012-OC), just never a normal
            // runtime scene.
            EditorBuildSettingsScene livingHive = EditorBuildSettings.scenes.FirstOrDefault(s => s.guid.ToString().Equals(LivingHiveGuid, System.StringComparison.OrdinalIgnoreCase));

            Assert.That(livingHive, Is.Not.Null, "LivingHive.unity should remain present (disabled) for QA/editor access.");
            Assert.That(livingHive.enabled, Is.False, "LivingHive.unity must be disabled in Build Settings - it is legacy/dev-only, never the normal player hive.");
        }

        [Test]
        public void CanonicalWorldMapIsPresentAndEnabled()
        {
            EditorBuildSettingsScene worldMap = EditorBuildSettings.scenes.FirstOrDefault(s => s.guid.ToString().Equals(WorldMapGuid, System.StringComparison.OrdinalIgnoreCase));

            Assert.That(worldMap, Is.Not.Null, "Canonical WorldMap (WorldMapWave6Wave5Method12288Preview.unity) is missing from Build Settings.");
            Assert.That(worldMap.enabled, Is.True, "Canonical WorldMap must be enabled in Build Settings.");
        }

        [Test]
        public void HiveMapAppearsBeforeLivingHiveInBuildOrder()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            int hiveMapIndex = System.Array.FindIndex(scenes, s => s.guid.ToString().Equals(HiveMapGuid, System.StringComparison.OrdinalIgnoreCase));
            int livingHiveIndex = System.Array.FindIndex(scenes, s => s.guid.ToString().Equals(LivingHiveGuid, System.StringComparison.OrdinalIgnoreCase));

            Assert.That(hiveMapIndex, Is.GreaterThanOrEqualTo(0), "HiveMap must be present in Build Settings.");
            if (livingHiveIndex >= 0)
                Assert.That(hiveMapIndex, Is.LessThan(livingHiveIndex),
                    "HiveMap must appear before LivingHive in the Build Settings scene order - a build's scene index 0 must always be the production hive.");
        }
    }
}
