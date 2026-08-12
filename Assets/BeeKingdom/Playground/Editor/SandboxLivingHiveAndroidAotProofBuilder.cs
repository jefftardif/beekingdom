using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public static class SandboxLivingHiveAndroidAotProofBuilder
    {
        private const string ScenePath = "Assets/Scenes/LivingHive.unity";
        private const string OutputDirectory = "Artifacts/AndroidAotProof";
        private const string ApkPath = OutputDirectory + "/BeeKingdom-LivingHive-IL2CPP-arm64.apk";
        private const string ManifestPath = OutputDirectory + "/BeeKingdom-LivingHive-IL2CPP-arm64.md";
        private const string RuntimeConfigurationPath = "Assets/Resources/BeeKingdom/MobileAccountSessionRuntime.asset";

        [MenuItem("Bee Kingdom/Playground/QA/Build LivingHive Android IL2CPP Proof")]
        public static void BuildAndExit()
        {
            ScriptingImplementation previousBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android);
            AndroidArchitecture previousArchitectures = PlayerSettings.Android.targetArchitectures;
            bool previousBuildAppBundle = EditorUserBuildSettings.buildAppBundle;
            Exception failure = null;
            int exitCode = 1;
            try
            {
                if (!File.Exists(ScenePath)) throw new FileNotFoundException("LivingHive scene is missing.", ScenePath);
                if (File.Exists(RuntimeConfigurationPath))
                    throw new InvalidOperationException("The local AOT proof must not embed a runtime account configuration asset.");

                Directory.CreateDirectory(OutputDirectory);
                DeleteIfExists(ApkPath);
                DeleteIfExists(ManifestPath);
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                EditorUserBuildSettings.buildAppBundle = false;

                var options = new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = ApkPath,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.Development | BuildOptions.CompressWithLz4
                };
                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;
                if (summary.result != BuildResult.Succeeded || summary.totalErrors != 0 || !File.Exists(ApkPath))
                    throw new InvalidOperationException(
                        "Android IL2CPP proof failed: " + summary.result + ", errors=" + summary.totalErrors.ToString(CultureInfo.InvariantCulture));

                FileInfo apk = new FileInfo(ApkPath);
                string manifest = BuildManifest(apk, summary, previousBackend, previousArchitectures, previousBuildAppBundle);
                File.WriteAllText(ManifestPath, manifest, new UTF8Encoding(false));
                Debug.Log("LivingHive Android IL2CPP proof built: " + apk.FullName);
                exitCode = 0;
            }
            catch (Exception exception)
            {
                failure = exception;
                Debug.LogError("LivingHive Android IL2CPP proof failed: " + exception);
            }
            finally
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, previousBackend);
                PlayerSettings.Android.targetArchitectures = previousArchitectures;
                EditorUserBuildSettings.buildAppBundle = previousBuildAppBundle;
                AssetDatabase.SaveAssets();
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(exitCode);
                return;
            }

            if (failure != null) throw new InvalidOperationException("Android IL2CPP proof failed.", failure);
        }

        private static string BuildManifest(
            FileInfo apk,
            BuildSummary summary,
            ScriptingImplementation previousBackend,
            AndroidArchitecture previousArchitectures,
            bool previousBuildAppBundle)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Bee Kingdom LivingHive — preuve Android IL2CPP locale");
            builder.AppendLine();
            builder.AppendLine("- Scene unique: `" + ScenePath + "`");
            builder.AppendLine("- Cible: `Android`");
            builder.AppendLine("- Backend de preuve: `IL2CPP`");
            builder.AppendLine("- Architecture de preuve: `ARM64`");
            builder.AppendLine("- Format: `APK Development`, signature debug locale");
            builder.AppendLine("- Asset de configuration compte/serveur embarque: `false`");
            builder.AppendLine("- Compte, secret, HiveId ou flag Production embarque: `false`");
            builder.AppendLine("- Deploiement ou installation appareil: `false`");
            builder.AppendLine("- Resultat Unity: `" + summary.result + "`");
            builder.AppendLine("- Erreurs: `" + summary.totalErrors.ToString(CultureInfo.InvariantCulture) + "`");
            builder.AppendLine("- Avertissements: `" + summary.totalWarnings.ToString(CultureInfo.InvariantCulture) + "`");
            builder.AppendLine("- Taille rapportee: `" + summary.totalSize.ToString(CultureInfo.InvariantCulture) + "` octets");
            builder.AppendLine("- Duree: `" + summary.totalTime.ToString() + "`");
            builder.AppendLine("- APK: `" + ApkPath + "`");
            builder.AppendLine("- Taille APK: `" + apk.Length.ToString(CultureInfo.InvariantCulture) + "` octets");
            builder.AppendLine("- SHA-256 APK: `" + Hash(apk.FullName) + "`");
            builder.AppendLine("- Backend Android anterieur restaure: `" + previousBackend + "`");
            builder.AppendLine("- Architectures Android anterieures restaurees: `" + previousArchitectures + "`");
            builder.AppendLine("- Preference App Bundle anterieure restauree: `" + previousBuildAppBundle.ToString().ToLowerInvariant() + "`");
            builder.AppendLine();
            builder.AppendLine("Cette preuve valide compilation, conversion IL2CPP, stripping/linker et packaging local seulement.");
            builder.AppendLine("AndroidKeyStore, TLS, reprise, corruption et comportement reseau exigent encore un appareil physique et staging.");
            return builder.ToString();
        }

        private static string Hash(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                byte[] hash = sha.ComputeHash(stream);
                var builder = new StringBuilder(hash.Length * 2);
                foreach (byte value in hash) builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
