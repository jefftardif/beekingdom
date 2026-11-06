#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using BeeKingdom.Playground;
using UnityEditor;
using UnityEngine;
namespace BeeKingdom.EditorOccupationAudit
{
    public static class MapOccupationReadOnlyExporter
    {
        public const string Destination=@"C:\Users\Utilisateur\Documents\Codex\2026-10-25\task\occupancy-repair\releve-unity";
        [MenuItem("Bee Kingdom/Diagnostics/Export loaded occupation read-only")]
        public static void Export()
        {
            if(!EditorApplication.isPlaying || !EditorApplication.isPaused)
                throw new InvalidOperationException("Export requires existing Play mode paused by the operator.");
            var maps=UnityEngine.Object.FindObjectsByType<WorldMapMmoFullscreenFoundationBootstrap>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);
            if(maps.Length!=1)throw new InvalidOperationException("Expected exactly one active map bootstrap.");
            var controllerField=typeof(MobileAccountSessionRuntimeBootstrap).GetField("pollinationPanelController",BindingFlags.Static|BindingFlags.NonPublic);
            object controller=controllerField?.GetValue(null);
            OccupationRead snapshot=MapOccupationReadOnlySnapshot.Read(maps[0],controller);
            string json=JsonUtility.ToJson(snapshot,true);
            Directory.CreateDirectory(Destination);
            string path=Path.Combine(Destination,"occupation-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")+".json");
            using(var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
            using(var writer=new StreamWriter(file))writer.Write(json);
            Debug.Log("Read-only occupation snapshot saved: "+path);
        }
    }
}
#endif
