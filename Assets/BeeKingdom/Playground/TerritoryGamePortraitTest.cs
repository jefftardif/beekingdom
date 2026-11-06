#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using BeeKingdom.Networking;
using BeeKingdom.Playground;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Edit-mode Game-view fixture. No Play, no account session, no saved test scene.
[ExecuteAlways]
public sealed class TerritoryGamePortraitTest : MonoBehaviour
{
    private const string Folder = @"C:\Users\Utilisateur\Documents\Codex\2026-10-25\task\territory-sql-tests";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static Scene fixtureScene;
    private static Scene originalScene;
    private static bool originalDirty;
    private static int generation;
    private WorldMapMmoFullscreenFoundationBootstrap renderer;
    private RemoteTerritoryOutline outline;
    private string selected = "aucune";
    private int selectedCount;
    private int frames;
    private bool oblique;
    private bool offscreen;
    private RemoteTerritoryPoint[] originalRing;

    [InitializeOnLoadMethod]
    private static void RegisterCleanup()
    {
        AssemblyReloadEvents.beforeAssemblyReload -= Close;
        AssemblyReloadEvents.beforeAssemblyReload += Close;
        EditorApplication.quitting -= Close;
        EditorApplication.quitting += Close;
    }

    [MenuItem("Tools/BeeKingdom/QA/Game portrait TEST ouvrir")]
    private static void Open()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Mode édition requis");
        Close();
        originalScene = SceneManager.GetActiveScene();
        originalDirty = originalScene.isDirty;
        try
        {
        fixtureScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(originalScene);
        var root = new GameObject("TEST territoire - aucune donnée joueur");
        SceneManager.MoveGameObjectToScene(root, fixtureScene);
        var camera = root.AddComponent<Camera>();
        camera.depth = 999;
        camera.cullingMask = 0;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.07f, .10f, .13f);
        var test = root.AddComponent<TerritoryGamePortraitTest>();
        generation++;
        test.InitializeTest();
        EditorApplication.QueuePlayerLoopUpdate();
        }
        catch { Close(); throw; }
    }

    [MenuItem("Tools/BeeKingdom/QA/Game portrait TEST fermer")]
    private static void Close()
    {
        if (!fixtureScene.IsValid()) return;
        string result = "";
        try
        {
        foreach (GameObject root in fixtureScene.GetRootGameObjects())
        {
            var test = root.GetComponent<TerritoryGamePortraitTest>();
            if (test == null) continue;
            result += DateTime.UtcNow.ToString("O") + " generation=" + generation + " frames=" + test.frames
                + " selectedCount=" + test.selectedCount + " last=" + test.selected
                + " screen=" + Screen.width + "x" + Screen.height + "\n";
            test.Cleanup();
        }
        }
        finally { EditorSceneManager.CloseScene(fixtureScene, true); }
        File.AppendAllText(Path.Combine(Folder, "game-portrait-result.txt"),
            result + "Restored=" + (SceneManager.GetActiveScene().handle == originalScene.handle
                && originalScene.isDirty == originalDirty && !EditorApplication.isPlaying) + "\n");
    }

    private void InitializeTest()
    {
        var response = new SystemTextGameJsonCodec().Deserialize<RemoteTerritoryResponse>(
            File.ReadAllText(Path.Combine(Folder, "TEST-sql-territory-response.json")));
        if (!TerritoryOutlineValidation.ValidResponse(response, response.PlayerId,
            "TEST-world", "TEST-map", "TEST-coordinates")) throw new InvalidDataException("Fixture invalide");
        outline = response.View.Outline;
        // Derived TEST geometry only; the recorded server response on disk is unchanged.
        foreach (var point in outline.Ring) { point.X = -50m + (point.X + 50m) * 10m; point.Y *= 10m; }
        originalRing = outline.Ring;
        var host = new GameObject("MMO inactif TEST");
        host.SetActive(false);
        SceneManager.MoveGameObjectToScene(host, fixtureScene);
        renderer = host.AddComponent<WorldMapMmoFullscreenFoundationBootstrap>();
        Type node = renderer.GetType().GetNestedType("WorldHiveNode", BindingFlags.NonPublic);
        ConstructorInfo ctor = node.GetConstructors()[0];
        var list = (IList)renderer.GetType().GetField("hives", Flags).GetValue(renderer);
        // Exact test IDs only; no player/colony binding and no account cache.
        foreach (var item in new[] { ("TEST-centre", new Vector2(-50, 0)), ("TEST-bord", new Vector2(-25f, 0)) })
            list.Add(ctor.Invoke(new object[] { item.Item1, item.Item1, "TEST",
                Enum.ToObject(ctor.GetParameters()[3].ParameterType, 0), item.Item2, "Fixture non production" }));
    }

    private void Cleanup()
    {
        if (renderer == null) return;
        FieldInfo field = renderer.GetType().GetField("pixel", Flags);
        var texture = field.GetValue(renderer) as Texture2D;
        if (texture != null) DestroyImmediate(texture);
        field.SetValue(renderer, null);
        renderer = null;
    }

    private void OnDestroy() { Cleanup(); }

    private Vector2 Project(Vector2 point) => (Vector2)renderer.GetType()
        .GetMethod("WorldToScreen", Flags).Invoke(renderer, new object[] { point });

    private void OnGUI()
    {
        if (renderer == null || outline == null) return;
        renderer.GetType().GetField("currentWorldCenter", Flags).SetValue(renderer, new Vector2(-50 + (offscreen ? Screen.width / 5.3f : 0), 0));
        // Native game zoom; derived TEST fixture spans fifty world units.
        renderer.GetType().GetField("currentZoom", Flags).SetValue(renderer, 2.65f);
        GUI.Label(new Rect(12, 12, Screen.width - 24, 60), "TEST hors session — génération " + generation
            + "\nGéométrie TEST ×10 ; zoom natif 2,65 ; aucun choix produit. " + Screen.width + " × " + Screen.height);
        GUI.Label(new Rect(12, 78, Screen.width - 24, 50), "Sélection réelle ChooseMapHit : " + selected + " / " + selectedCount);
        if (GUI.Button(new Rect(12, 135, 180, 32), "Oblique TEST / rectangle"))
        {
            oblique = !oblique;
            outline.Ring = oblique ? new[] { new RemoteTerritoryPoint { X=-75,Y=-25 }, new RemoteTerritoryPoint { X=-25,Y=0 }, new RemoteTerritoryPoint { X=-65,Y=25 } } : originalRing;
        }
        if (GUI.Button(new Rect(12, 175, 180, 32), "Décaler hors écran TEST")) offscreen = !offscreen;
        if (GUI.Button(new Rect(12, 215, 180, 32), "Fermer TEST")) EditorApplication.delayCall += Close;
        if (GUI.Button(new Rect(12, 255, 180, 32), "Recréer TEST")) EditorApplication.delayCall += Open;
        if (Event.current.type == EventType.MouseDown)
        {
            renderer.GetType().GetMethod("DrawOfficialAllianceTerritory", Flags).Invoke(renderer, null);
        if (Event.current.type == EventType.MouseDown)
            {
                renderer.GetType().GetMethod("ChooseMapHit", Flags).Invoke(renderer, new object[] { Event.current.mousePosition });
                selected = renderer.GetType().GetField("lastHitId", Flags).GetValue(renderer) as string ?? "aucune";
                if (selected.StartsWith("TEST-", StringComparison.Ordinal)) selectedCount++;
            }
        }
        foreach (var item in new[] { ("TEST-centre", new Vector2(-50, 0)), ("TEST-bord", new Vector2(-25f, 0)) })
        {
            Vector2 p = Project(item.Item2);
            GUI.Box(new Rect(p.x - 8, p.y - 8, 16, 16), "");
            GUI.Label(new Rect(p.x - 35, p.y + 12, 100, 24), item.Item1);
        }
        if (Event.current.type != EventType.Repaint) return;
        renderer.GetType().GetMethod("DrawAllianceTerritoryOutline", Flags).Invoke(renderer, new object[] { outline });
        frames++;
    }
}
#endif
