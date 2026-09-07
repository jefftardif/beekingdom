using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BeeKingdom.EditorTools
{
    // M056-CL : build Windows autonome et transportable.
    //
    // POURQUOI CE POST-TRAITEMENT EXISTE
    // ----------------------------------
    // Le rendu des batiments de HiveMap est materialise AU RUNTIME par
    // BuildingRuntimeViewBootstrap / BuildingInteractionBootstrap / HiveMapAmbientBeesBootstrap.
    // Ces trois systemes lisent leurs donnees avec :
    //
    //     Path.Combine(Application.dataPath, <chemin relatif sans le prefixe "Assets/">)
    //
    // Dans l'editeur, Application.dataPath == <repo>/Assets : les fichiers existent, tout
    // marche, et c'est pour ca que le bug est invisible en Play Mode.
    // Dans un player standalone, Application.dataPath == <build>/BeeKingdom_Data, et ni le
    // sidecar de placement ni les artworks PNG n'y sont copies (ils ne sont ni dans
    // Resources, ni dans StreamingAssets). Resultat sans ce post-traitement : File.Exists
    // echoue, MaterializeRuntimeVisualBuildings retourne 0, et la build demarre sur une
    // HiveMap VIDE - aucun batiment, aucune zone cliquable.
    //
    // Ce post-traitement recopie donc ces fichiers dans le player en respectant EXACTEMENT
    // la meme arborescence relative, ce qui rend les chemins existants valides tels quels.
    // Aucun code runtime n'est modifie : le comportement editeur reste strictement
    // identique, et le risque de regression sur ce que le CEO teste tous les jours est nul.
    //
    // DETTE TECHNIQUE ASSUMEE (a traiter avant le portage mobile)
    // -----------------------------------------------------------
    // Sur Android/iOS, Application.dataPath pointe dans l'APK/le bundle et n'est pas
    // lisible avec System.IO : cette approche NE FONCTIONNERA PAS sur mobile. La vraie
    // correction, hors perimetre M056, est de migrer ces assets vers StreamingAssets ou
    // Resources et d'adapter les trois points de lecture. Voir le rapport M056.
    public static class WindowsInternalBuildTool
    {
        public const string Version = "0.1.0-alpha-internal";

        private const string OutputDirectory = "Builds/Windows/BeeKingdom-Alpha-Internal";
        private const string ExecutableName = "BeeKingdom.exe";
        private const string StampResourcePath = "Assets/BeeKingdom/Playground/Resources/BeeKingdom/BuildStamp.txt";

        // Scene de demarrage OBLIGATOIRE : la ruche officielle. LivingHive est retiree.
        private const string HiveMapScene = "Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_HiveMap_Test.unity";
        private const string LivingHiveScene = "Assets/Scenes/LivingHive.unity";

        // Fichiers lus au runtime via Application.dataPath : doivent suivre dans le player.
        private static readonly string[] PayloadDirectories =
        {
            "Assets/Experiments/Environment2D5D/Config",
            "Assets/BeeKingdom/Art/Buildings"
        };

        // Planifie la build sur le prochain tick de l'editeur, pour que l'appel MCP
        // retourne IMMEDIATEMENT. Sans ca, l'appel bloque pendant toute la build, le
        // transport MCP considere la requete perdue et la REJOUE : on se retrouve avec une
        // dizaine de builds enchainees qui se suppriment mutuellement le dossier de sortie.
        public static void ScheduleBuild(string commitHash)
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    Debug.Log("[M056]\n" + Build(commitHash));
                }
                catch (Exception e)
                {
                    Debug.LogError("[M056] BUILD EXCEPTION : " + e);
                }
            };
        }

        // Garde anti-rejeu. Une build depasse largement le delai d'attente du transport MCP :
        // celui-ci considere alors la requete perdue et la REJOUE (jusqu'a 10 fois). Comme la
        // build monopolise le thread principal, les rejeux s'executent en file APRES coup et
        // relancent chacun une build complete qui commence par supprimer le dossier de sortie.
        // Cette garde fait qu'un rejeu proche dans le temps ne fait rien du tout.
        private static DateTime _lastBuildStartedUtc = DateTime.MinValue;
        private static readonly TimeSpan ReplayWindow = TimeSpan.FromMinutes(20);

        public static string Build(string commitHash)
        {
            if (DateTime.UtcNow - _lastBuildStartedUtc < ReplayWindow)
                return "[M056] Rejeu ignore : une build a deja ete lancee il y a moins de 20 minutes.";
            _lastBuildStartedUtc = DateTime.UtcNow;

            var log = new List<string>();

            string[] scenes = ResolveScenes(log);
            WriteBuildStamp(commitHash, log);

            PlayerSettings.bundleVersion = Version;

            string outputDir = Path.GetFullPath(OutputDirectory);
            if (Directory.Exists(outputDir))
            {
                // Build FRAICHE : on ne recycle jamais des morceaux d'une build precedente.
                try
                {
                    Directory.Delete(outputDir, true);
                    log.Add("Ancien dossier de build supprime (build fraiche).");
                }
                catch (Exception e)
                {
                    // Typiquement : le jeu de la build precedente tourne encore et verrouille
                    // BeeKingdom.exe. On relache la garde anti-rejeu pour permettre un retry.
                    _lastBuildStartedUtc = DateTime.MinValue;
                    throw new IOException(
                        "Impossible de supprimer le dossier de sortie (executable verrouille ? " +
                        "fermer BeeKingdom.exe avant de rebuilder). " + e.Message, e);
                }
            }
            Directory.CreateDirectory(outputDir);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outputDir, ExecutableName),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            log.Add("Resultat : " + summary.result);
            log.Add("Duree : " + summary.totalTime);
            log.Add("Taille totale : " + summary.totalSize + " octets");
            log.Add("Erreurs : " + summary.totalErrors + " / Avertissements : " + summary.totalWarnings);

            if (summary.result != BuildResult.Succeeded)
            {
                return string.Join("\n", log);
            }

            CopyRuntimePayload(outputDir, log);
            return string.Join("\n", log);
        }

        private static string[] ResolveScenes(List<string> log)
        {
            var scenes = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;
                if (string.Equals(scene.path, LivingHiveScene, StringComparison.OrdinalIgnoreCase))
                {
                    // Garde-fou dur : LivingHive ne doit JAMAIS entrer dans une build.
                    log.Add("REFUS : LivingHive etait activee dans les Build Settings, exclue de la build.");
                    continue;
                }
                scenes.Add(scene.path);
            }

            if (scenes.Count == 0 || !string.Equals(scenes[0], HiveMapScene, StringComparison.OrdinalIgnoreCase))
            {
                // La scene de demarrage doit etre la ruche officielle, index 0.
                scenes.Remove(HiveMapScene);
                scenes.Insert(0, HiveMapScene);
                log.Add("Scene de demarrage forcee sur Environment2D5D_HiveMap_Test.");
            }

            log.Add("Scene de demarrage (index 0) : " + scenes[0]);
            log.Add("Scenes incluses : " + scenes.Count);
            return scenes.ToArray();
        }

        private static void WriteBuildStamp(string commitHash, List<string> log)
        {
            string stamp = "BeeKingdom Alpha Internal " + Version +
                           " | commit " + (string.IsNullOrEmpty(commitHash) ? "unknown" : commitHash) +
                           " | " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(StampResourcePath)));
            File.WriteAllText(Path.GetFullPath(StampResourcePath), stamp);
            AssetDatabase.ImportAsset(StampResourcePath, ImportAssetOptions.ForceUpdate);
            log.Add("Build stamp : " + stamp);
        }

        private static void CopyRuntimePayload(string outputDir, List<string> log)
        {
            string dataDir = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(ExecutableName) + "_Data");
            if (!Directory.Exists(dataDir))
            {
                log.Add("ERREUR : dossier _Data introuvable, payload runtime NON copie.");
                return;
            }

            int copied = 0;
            foreach (string sourceRelative in PayloadDirectories)
            {
                string sourceDir = Path.GetFullPath(sourceRelative);
                if (!Directory.Exists(sourceDir))
                {
                    log.Add("ATTENTION : source introuvable " + sourceRelative);
                    continue;
                }

                // On reproduit le chemin relatif APRES suppression du prefixe "Assets/",
                // exactement comme le fait le code runtime.
                string relative = sourceRelative.Substring("Assets/".Length).Replace('/', Path.DirectorySeparatorChar);
                string destinationDir = Path.Combine(dataDir, relative);
                Directory.CreateDirectory(destinationDir);

                foreach (string file in Directory.GetFiles(sourceDir))
                {
                    // Les .meta sont purement editeur : inutiles (et indesirables) dans le player.
                    if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                    File.Copy(file, Path.Combine(destinationDir, Path.GetFileName(file)), true);
                    copied++;
                }
                log.Add("Payload copie : " + sourceRelative + " -> BeeKingdom_Data/" + relative);
            }
            log.Add("Fichiers de payload copies : " + copied);
        }
    }
}
