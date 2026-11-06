#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using BeeKingdom.Networking;
using BeeKingdom.Playground;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Isolated Editor test. Never activates the component or touches the account cache.
public sealed class TerritoryMmoRendererTest : EditorWindow
{
    private const string Folder = @"C:\Users\Utilisateur\Documents\Codex\2026-10-25\task\territory-sql-tests";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private Scene preview;
    private Scene original;
    private bool originalDirty;
    private WorldMapMmoFullscreenFoundationBootstrap renderer;
    private RemoteTerritoryOutline outline;
    private float zoom = 1f;
    private string status = "Non exécuté";
    private int clicks;
    private int repaints;
    private bool projectionPassed;

    [MenuItem("Tools/BeeKingdom/QA/Renderer MMO TEST")]
    private static void Open()
    {
        GetWindow<TerritoryMmoRendererTest>("Renderer MMO TEST").Show();
    }

    private void OnDisable()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        if (renderer != null)
        {
            FieldInfo pixelField = renderer.GetType().GetField("pixel", Flags);
            var pixel = pixelField.GetValue(renderer) as Texture2D;
            if (pixel != null) DestroyImmediate(pixel);
            pixelField.SetValue(renderer, null);
        }
        if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
        renderer = null;
        outline = null;
    }

    private void InitializeTest()
    {
        Cleanup();
        original = SceneManager.GetActiveScene();
        originalDirty = original.isDirty;
        var response = new SystemTextGameJsonCodec().Deserialize<RemoteTerritoryResponse>(
            File.ReadAllText(Path.Combine(Folder, "TEST-sql-territory-response.json")));
        if (response == null || !TerritoryOutlineValidation.ValidResponse(response, response.PlayerId,
            "TEST-world", "TEST-map", "TEST-coordinates")) throw new InvalidDataException("Fixture TEST invalide");
        outline = response.View.Outline;
        preview = EditorSceneManager.NewPreviewScene();
        var host = new GameObject("Renderer TEST inactif");
        host.SetActive(false);
        SceneManager.MoveGameObjectToScene(host, preview);
        renderer = host.AddComponent<WorldMapMmoFullscreenFoundationBootstrap>();
        renderer.GetType().GetField("currentWorldCenter", Flags).SetValue(renderer,
            new Vector2((float)outline.HeadquartersPosition.X, (float)outline.HeadquartersPosition.Y));
        projectionPassed = true;
        foreach (float value in new[] { .30f, 1f, 2.65f })
        {
            renderer.GetType().GetField("currentZoom", Flags).SetValue(renderer, value);
            Vector2 a = Project(Vector2.zero);
            Vector2 b = Project(new Vector2(10, 10));
            projectionPassed &= Mathf.Abs((b.x - a.x) - 10 * value) < .01f
                && Mathf.Abs((b.y - a.y) - 10 * value) < .01f;
        }
        clicks = 0;
        repaints = 0;
        status = "Projection réelle aux zooms 0,30 / 1 / 2,65 : " + (projectionPassed ? "PASS" : "FAIL");
    }

    private Vector2 Project(Vector2 point)
    {
        return (Vector2)renderer.GetType().GetMethod("WorldToScreen", Flags).Invoke(renderer, new object[] { point });
    }

    private bool SceneUnchanged()
    {
        return original.IsValid() && SceneManager.GetActiveScene().handle == original.handle
            && original.isDirty == originalDirty && !EditorApplication.isPlaying;
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("TEST isolé — dessin MMO réel, réponse SQL/HTTP enregistrée. Aucun territoire joueur. Loupe diagnostique ×10, aucune taille produit approuvée.", MessageType.Warning);
        if (GUILayout.Button("Initialiser le test isolé"))
        {
            try { InitializeTest(); }
            catch (Exception error) { Cleanup(); status = "Échec : " + error; }
        }
        EditorGUILayout.LabelField(status, EditorStyles.wordWrappedLabel);
        if (renderer == null || outline == null) return;
        zoom = EditorGUILayout.Slider("Zoom MMO", zoom, .30f, 2.65f);
        EditorGUILayout.LabelField("Repaints réels : " + repaints + " | Clics sans capture : " + clicks
            + " | Scène utilisateur préservée : " + SceneUnchanged());
        if (GUILayout.Button("Enregistrer la preuve et fermer la scène TEST"))
        {
            bool unchanged = SceneUnchanged();
            Cleanup();
            bool after = SceneUnchanged();
            File.WriteAllText(Path.Combine(Folder, "renderer-mmo-result.txt"),
                "UTC=" + DateTime.UtcNow.ToString("O") + "\nProjection=" + projectionPassed
                + "\nRepaints=" + repaints + "\nUnconsumedMouseDown=" + clicks
                + "\nSceneBeforeCleanup=" + unchanged + "\nSceneAfterCleanup=" + after
                + "\nScope=Editor preview scene; actual MMO projection and outline draw; no player session, no live HTTP, no gameplay selection proof.\n");
            status = "Preuve enregistrée ; scène TEST fermée.";
            return;
        }
        Rect canvas = GUILayoutUtility.GetRect(100, 250, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(canvas, new Color(.07f, .10f, .13f));
        renderer.GetType().GetField("currentZoom", Flags).SetValue(renderer, zoom);
        if (Event.current.type == EventType.MouseDown && canvas.Contains(Event.current.mousePosition))
        {
            // Overlay is repaint-only in production. It must not consume mouse events.
            renderer.GetType().GetMethod("DrawOfficialAllianceTerritory", Flags).Invoke(renderer, null);
            if (Event.current.type == EventType.MouseDown) clicks++;
            Repaint();
        }
        if (Event.current.type != EventType.Repaint) return;
        // Use the real camera transform, not an outer GUI scale (rotation pivots are screen-space).
        float diagnosticZoom = zoom * 10f;
        renderer.GetType().GetField("currentZoom", Flags).SetValue(renderer, diagnosticZoom);
        renderer.GetType().GetField("currentWorldCenter", Flags).SetValue(renderer,
            new Vector2((float)outline.HeadquartersPosition.X, (float)outline.HeadquartersPosition.Y)
            + (new Vector2(Screen.width * .5f, Screen.height * .5f) - canvas.center) / diagnosticZoom);
        Matrix4x4 saved = GUI.matrix;
        Color savedColor = GUI.color;
        try
        {
            renderer.GetType().GetMethod("DrawAllianceTerritoryOutline", Flags).Invoke(renderer, new object[] { outline });
            repaints++;
        }
        finally { GUI.matrix = saved; GUI.color = savedColor; }
    }
}
#endif
