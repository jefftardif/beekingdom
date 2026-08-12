using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public static class SandboxBee1023VisualContourImportReadyExport
    {
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-079_BEE1001_1020_Source/VisualContourImport";
        private const string ManifestPath = OutputDirectory + "/BEE-1023_VisualContourImportReady_Manifest.md";
        private const string ReportPath = "C:/projets/beekingdom/prompts_codex/rapports/BuilderA_BEE1023_VISUAL_CONTOUR_IMPORT_READY_Report.md";

        [MenuItem("Bee Kingdom/Playground/Export BEE-1023 Visual Contour Import Ready")]
        public static void ExportForBatch()
        {
            try
            {
                Directory.CreateDirectory(OutputDirectory);
                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? OutputDirectory);
                File.WriteAllText(ManifestPath, BuildManifest(), Encoding.UTF8);
                File.WriteAllText(ReportPath, BuildReport(), Encoding.UTF8);
                Debug.Log("BEE-1023 visual contour import readiness exported.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("BEE-1023 visual contour import readiness export failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        public static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# BEE-1023 Visual Contour Import Ready Manifest");
            builder.AppendLine();
            builder.AppendLine("## Import Status");
            builder.AppendLine();
            foreach (string row in HiveViewProductUiPresenter.VisualContourImportStatusForProof()) builder.AppendLine("- " + row);
            builder.AppendLine();
            builder.AppendLine("## Runtime Fallback");
            builder.AppendLine();
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof("wax_workshop");
            foreach (string row in HiveViewProductUiPresenter.PixelPerfectContourRuntimeForProof()) builder.AppendLine("- " + row);
            builder.AppendLine();
            builder.AppendLine("## Required UI-B Zone IDs");
            builder.AppendLine();
            foreach (string zoneId in HiveVisualContourImportRuntime.RequiredZoneIds()) builder.AppendLine("- `" + zoneId + "`");
            builder.AppendLine();
            builder.AppendLine("READY_FOR_UI_VISUAL_CONTOUR_IMPORT = YES");
            return builder.ToString();
        }

        private static string BuildReport()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Builder-A BEE-1023 Visual Contour Import Ready Report");
            builder.AppendLine();
            builder.AppendLine("## Status");
            builder.AppendLine();
            builder.AppendLine("* Completed");
            builder.AppendLine();
            builder.AppendLine("## Resume");
            builder.AppendLine();
            builder.AppendLine("Arret de la strategie de contours artistiques devines dans le code. Le runtime est maintenant prepare pour consommer des contours visibles externes fournis par UI-B sous forme de JSON normalise, idealement converti depuis des paths SVG dessines visuellement dans Inkscape au-dessus de l'image de ruche. Tant que ce fichier n'existe pas, Unity conserve seulement les hitboxes tactiles invisibles et ne dessine pas les anciens contours codes comme contours finaux.");
            builder.AppendLine();
            builder.AppendLine("## Fichiers crees");
            builder.AppendLine();
            builder.AppendLine("* `Assets/BeeKingdom/Playground/HiveVisualContourImportRuntime.cs`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/HiveVisualContourImportRuntime.cs.meta`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Data/HiveVisualContours_IMPORT_CONTRACT.md`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Data/HiveVisualContours_IMPORT_CONTRACT.md.meta`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee1023VisualContourImportReadyTests.cs`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee1023VisualContourImportReadyTests.cs.meta`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee1023VisualContourImportReadyExport.cs`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee1023VisualContourImportReadyExport.cs.meta`");
            builder.AppendLine("* `" + ManifestPath + "`");
            builder.AppendLine();
            builder.AppendLine("## Fichiers modifies");
            builder.AppendLine();
            builder.AppendLine("* `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee1010PixelPerfectContourRuntimeTests.cs`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee1022OrganicWaxBoundaryContourTests.cs`");
            builder.AppendLine();
            builder.AppendLine("## Decisions d'architecture");
            builder.AppendLine();
            builder.AppendLine("* Source visible finale obligatoire: `Assets/BeeKingdom/Playground/Resources/BeeKingdom/HiveVisualContours.json`.");
            builder.AppendLine("* Format attendu: `bee-hive-visual-contours-v1`, points normalises `0..1` dans le repere de l'image de ruche.");
            builder.AppendLine("* Les contours visibles importes sont separes des hitboxes tactiles invisibles existantes.");
            builder.AppendLine("* Fallback sans fichier UI-B: aucun faux contour visible final; les hitboxes techniques restent disponibles pour la selection.");
            builder.AppendLine();
            builder.AppendLine("## APIs publiques ajoutees");
            builder.AppendLine();
            builder.AppendLine("* `HiveVisualContourImportRuntime`");
            builder.AppendLine("* `HiveViewProductUiPresenter.VisualContourImportStatusForProof()`");
            builder.AppendLine();
            builder.AppendLine("## Changements importants");
            builder.AppendLine();
            builder.AppendLine("* `DrawReferencePolygonHalo`, hover et pulse quittent silencieusement si aucun contour externe n'est charge.");
            builder.AppendLine("* `PixelPerfectContourRuntimeForProof()` expose `visual_contour_source:none_waiting_ui_import`, `fallback_final_visual_contour:false` et `coded_guess_visual_contour_final:false` tant que UI-B n'a pas livre le fichier.");
            builder.AppendLine("* Les tests BEE-1010/BEE-1022 sont realignes sur ARCH-240: preuves techniques partielles, pas validation visuelle finale.");
            builder.AppendLine();
            builder.AppendLine("## Compatibilite");
            builder.AppendLine();
            builder.AppendLine("* Ruche uniquement, aucune carte monde, aucun BEE-881, aucun serveur officiel/live.");
            builder.AppendLine("* La selection reste fonctionnelle par hitbox invisible.");
            builder.AppendLine("* Aucun claim que les contours passent visuellement sans source UI-B.");
            builder.AppendLine();
            builder.AppendLine("## Tests");
            builder.AppendLine();
            builder.AppendLine("* `SandboxBee1023VisualContourImportReadyTests.RunAllForBatch`");
            builder.AppendLine("* `SandboxBee1010PixelPerfectContourRuntimeTests.RunAllForBatch`");
            builder.AppendLine("* `SandboxBee1022OrganicWaxBoundaryContourTests.RunAllForBatch`");
            builder.AppendLine();
            builder.AppendLine("## Compilation");
            builder.AppendLine();
            builder.AppendLine("* Compilation Unity validee par batch.");
            builder.AppendLine();
            builder.AppendLine("## Limitations");
            builder.AppendLine();
            builder.AppendLine("* Aucun contour visuel final n'est livre dans cette passe; UI-B doit produire le fichier source dessine visuellement.");
            builder.AppendLine("* Pas de parse SVG runtime direct; le contrat recommande SVG/Inkscape puis conversion en JSON normalise.");
            builder.AppendLine();
            builder.AppendLine("## Recommandations");
            builder.AppendLine();
            builder.AppendLine("* UI-B doit livrer les huit zones prioritaires avec paths nommes et points normalises.");
            builder.AppendLine("* Builder-B/Demo-A doivent recapturer en Play Mode natif apres import reel.");
            builder.AppendLine();
            builder.AppendLine("## Risques");
            builder.AppendLine();
            builder.AppendLine("* Tant que le fichier UI-B n'existe pas, la vue joueur ne montrera pas de contour visible final; c'est volontaire pour eviter une fausse validation.");
            builder.AppendLine();
            builder.AppendLine("## Ready for next brick");
            builder.AppendLine();
            builder.AppendLine("YES");
            builder.AppendLine();
            builder.AppendLine("READY_FOR_UI_VISUAL_CONTOUR_IMPORT = YES");
            return builder.ToString();
        }
    }
}
