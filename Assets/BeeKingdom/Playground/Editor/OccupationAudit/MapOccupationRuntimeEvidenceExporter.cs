#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using BeeKingdom.Playground;
using UnityEditor;
using UnityEngine;
namespace BeeKingdom.EditorOccupationAudit
{
    public static class MapOccupationRuntimeEvidenceExporter
    {
        public const string Destination=@"C:\Users\Utilisateur\Documents\Codex\2026-10-26\task-3\runtime-evidence-lot5";
        [MenuItem("Bee Kingdom/Diagnostics/Export occupation runtime evidence v2 read-only")]
        public static void Export()
        {
            if(!EditorApplication.isPlaying || !EditorApplication.isPaused)throw new InvalidOperationException("Existing Play mode must already be paused by operator.");
            var maps=UnityEngine.Object.FindObjectsByType<WorldMapMmoFullscreenFoundationBootstrap>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);
            if(maps.Length!=1)throw new InvalidOperationException("Exactly one active map required.");
            var snapshot=maps[0].CaptureOccupationRuntimeEvidence();
            // Reuse the existing allowlisted observer for loaded/cache occupants and floral anchors.
            // This reflection is limited to that known controller field, never account/session objects.
            var controllerField=typeof(MobileAccountSessionRuntimeBootstrap).GetField("pollinationPanelController",BindingFlags.Static|BindingFlags.NonPublic);
            var loaded=MapOccupationReadOnlySnapshot.Read(maps[0],controllerField?.GetValue(null));
            Directory.CreateDirectory(Destination);
            string suffix=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")+".json";
            string path=Path.Combine(Destination,"runtime-"+suffix);
            using(var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
            using(var writer=new StreamWriter(file))writer.Write(JsonUtility.ToJson(snapshot,true));
            using(var file=new FileStream(Path.Combine(Destination,"loaded-"+suffix),FileMode.CreateNew,FileAccess.Write,FileShare.Read))
            using(var writer=new StreamWriter(file))writer.Write(JsonUtility.ToJson(loaded,true));
            Debug.Log("Read-only occupation evidence saved: "+path);
        }
    }
}
#endif
