#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using BeeKingdom.Networking;
using UnityEditor;
using UnityEngine;

// Editor-only inspection of a recorded TEST HTTP response. Never changes a scene,
// account session, runtime settings, terrain or the live territory cache.
public sealed class TerritoryHttpTestPreview : EditorWindow
{
    private string fixturePath = "";
    private string status = "Charger la réponse HTTP TEST enregistrée. Aucun territoire de jeu n'est ajouté.";
    private RemoteTerritoryResponse response;
    private float zoom = 1f;

    [MenuItem("Tools/BeeKingdom/QA/Territoire HTTP TEST")]
    private static void Open()
    {
        var window = GetWindow<TerritoryHttpTestPreview>("Territoire HTTP TEST");
        window.minSize = new Vector2(650, 460);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("DONNÉES TEST — aperçu éditeur hors carte de jeu. Réponse HTTP enregistrée, pas un serveur actif.", MessageType.Warning);
        if (GUILayout.Button("Charger la réponse HTTP TEST du paquet du 25 octobre"))
        {
            // Exact local file selected for this QA; Documents may be redirected to OneDrive.
            fixturePath = @"C:\Users\Utilisateur\Documents\Codex\2026-10-25\task\territory-http-tests\TEST-territory-response.json";
            LoadFixture();
        }
        fixturePath = EditorGUILayout.TextField("Réponse JSON", fixturePath);
        if (GUILayout.Button("Charger et valider la réponse TEST")) LoadFixture();
        EditorGUILayout.LabelField(status, EditorStyles.wordWrappedLabel);
        if (response == null || response.View == null || response.View.Outline == null) return;
        zoom = EditorGUILayout.Slider("Zoom de l'aperçu", zoom, .5f, 2f);
        var outline = response.View.Outline;
        EditorGUILayout.LabelField("Alliance niveau " + outline.AllianceLevel + "  |  Bâtiment niveau " + outline.HeadquartersLevel
            + "  |  Révision " + outline.Revision + "  |  " + outline.Ring.Length + " sommets reçus");
        Rect canvas = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(canvas, new Color(.07f, .10f, .13f));
        if (Event.current.type != EventType.Repaint) return;
        decimal minX = outline.Ring.Min(p => p.X), maxX = outline.Ring.Max(p => p.X);
        decimal minY = outline.Ring.Min(p => p.Y), maxY = outline.Ring.Max(p => p.Y);
        float span = Mathf.Max((float)(maxX - minX), (float)(maxY - minY));
        float scale = Mathf.Min(canvas.width, canvas.height) * .55f / span * zoom;
        decimal cx = (minX + maxX) / 2m, cy = (minY + maxY) / 2m;
        Vector3 Project(RemoteTerritoryPoint p) => new Vector3(canvas.center.x + (float)(p.X - cx) * scale,
            canvas.center.y - (float)(p.Y - cy) * scale, 0);
        var points = outline.Ring.Select(Project).Concat(new[] { Project(outline.Ring[0]) }).ToArray();
        Handles.BeginGUI();
        Handles.color = Color.black;
        Handles.DrawAAPolyLine(6, points);
        Handles.color = new Color(1f, .74f, .15f);
        Handles.DrawAAPolyLine(3, points);
        Vector3 headquarters = Project(outline.HeadquartersPosition);
        Handles.color = new Color(.3f, .8f, 1f);
        Handles.DrawSolidDisc(headquarters, Vector3.forward, 5);
        Handles.EndGUI();
        GUI.Label(new Rect(headquarters.x + 10, headquarters.y - 12, 200, 28), "Siège TEST — construit", EditorStyles.whiteLabel);
        GUI.Label(new Rect(canvas.x + 12, canvas.y + 12, canvas.width - 24, 25), "Contour issu du GET HTTP · aucun terrain simulé", EditorStyles.whiteLabel);
    }

    private void LoadFixture()
    {
        response = null;
        try
        {
            var loaded = new SystemTextGameJsonCodec().Deserialize<RemoteTerritoryResponse>(File.ReadAllText(fixturePath));
            if (loaded == null || loaded.WorldId != "TEST-world" || loaded.SurfaceId != "TEST-map"
                || !TerritoryOutlineValidation.ValidResponse(loaded, loaded.PlayerId, "TEST-world", "TEST-map", "TEST-coordinates"))
                throw new InvalidDataException("Réponse hors du scope TEST ou contrat invalide.");
            response = loaded;
            status = "Réponse TEST validée dans Unity. Capturée à " + loaded.ServerTimeUtc.ToString("u")
                + ". Aucun jeton chargé ; aucune écriture de scène.";
        }
        catch (Exception error) { status = "Aperçu refusé : " + error.Message; }
        Repaint();
    }
}
#endif
