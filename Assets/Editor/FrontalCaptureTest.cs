using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Experiments.Environment2D5D
{
    // DIAGNOSTIC (EDIT MODE, no play mode needed): opens the experiment scene, positions
    // the camera and the frontal quad EXACTLY as the runtime does (same formulas as
    // BuildingPerspectiveCamera.ApplyTransform and FrontalBackdrop.LateUpdate), then
    // renders the camera into a RenderTexture and saves two PNGs (default pose + moved
    // pose). Run in batch:
    //   Unity.exe -batchmode -quit -projectPath ... -executeMethod BeeKingdom.Experiments.Environment2D5D.FrontalCaptureTest.Run
    public static class FrontalCaptureTest
    {
        private const string ScenePath = "Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_SpatialV3.unity";
        private const string OutDir = "C:\\Users\\UTILIS~1\\AppData\\Local\\Temp\\opencode";

        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            if (!scene.IsValid())
            {
                Debug.LogError("[CaptureTest] scene open FAILED");
                EditorApplication.Exit(2);
                return;
            }

            var cam = Object.FindFirstObjectByType<Camera>();
            if (!cam)
            {
                Debug.LogError("[CaptureTest] camera NOT FOUND");
                EditorApplication.Exit(2);
                return;
            }
            Debug.Log("[CaptureTest] camera found: " + cam.name + " clearFlags=" + cam.clearFlags + " far=" + cam.farClipPlane);

            var quad = GameObject.Find("FrontalBackdrop");
            if (!quad)
            {
                Debug.LogError("[CaptureTest] FrontalBackdrop quad NOT FOUND");
                EditorApplication.Exit(2);
                return;
            }
            var renderer = quad.GetComponent<Renderer>();
            Debug.Log("[CaptureTest] quad renderer enabled=" + (renderer ? renderer.enabled.ToString() : "MISSING") +
                      " mat=" + (renderer && renderer.sharedMaterial ? renderer.sharedMaterial.name : "NONE") +
                      " shader=" + (renderer && renderer.sharedMaterial && renderer.sharedMaterial.shader ? renderer.sharedMaterial.shader.name : "NONE"));

            DumpBuilding();

            PosePose(cam, quad.transform, new Vector2(0f, 30f), 12f, 54f);
            SaveCapture(cam, "frontal_capture1_default.png");
            PosePose(cam, quad.transform, new Vector2(25f, 30f), 12f, 54f);
            SaveCapture(cam, "frontal_capture2_pan_right.png");
            PosePose(cam, quad.transform, new Vector2(0f, 50f), 12f, 54f);
            SaveCapture(cam, "frontal_capture3_pan_up.png");
            PosePose(cam, quad.transform, new Vector2(0f, 30f), 12f, 160f);
            SaveCapture(cam, "frontal_capture4_zoomout.png");

            // BUILDING TEST poses: camera around the building anchor at world (35, 18.003).
            Vector2 buildingAim = new Vector2(35f, 18.003f);
            PosePose(cam, quad.transform, buildingAim, 12f, 34f);
            SaveCapture(cam, "frontal_capture5_building_zoomin.png");

            quad.SetActive(false);
            SaveCapture(cam, "frontal_capture5b_no_backdrop.png");

            var buildRoot = GameObject.Find("BuildingPremium");
            Vector3 origPos = buildRoot.transform.position;
            Debug.Log("[CaptureTest] building orig pos=" + origPos + " -> temporarily moving to z=24 (in front of backdrop plane)");
            buildRoot.transform.position = new Vector3(origPos.x, origPos.y, 24f);
            SaveCapture(cam, "frontal_capture5c_building_infront.png");

            var fallbackMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            var savedMats = new System.Collections.Generic.List<Material>();
            int swapped = 0;
            foreach (var mr in buildRoot.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr.sharedMaterial && mr.sharedMaterial.shader &&
                    (mr.sharedMaterial.shader.name == "BeeKingdom/Experiments/PremiumBuilding" ||
                     mr.sharedMaterial.shader.name == "BeeKingdom/Experiments/SoftShadow"))
                {
                    savedMats.Add(mr.sharedMaterial);
                    mr.sharedMaterial = fallbackMat;
                    swapped++;
                }
            }
            Debug.Log("[CaptureTest] swapped " + swapped + " materials to URP/Unlit fallback (building at z=24)");
            SaveCapture(cam, "frontal_capture5d_building_unlit.png");
            int ri = 0;
            foreach (var mr in buildRoot.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr.sharedMaterial == fallbackMat && ri < savedMats.Count)
                {
                    mr.sharedMaterial = savedMats[ri];
                    ri++;
                }
            }
            buildRoot.transform.position = origPos;
            quad.SetActive(true);

            // Isolate the custom shader on a simple quad (no scene meshes involved).
            var shader = Shader.Find("BeeKingdom/Experiments/PremiumBuilding");
            Debug.Log("[CaptureTest] PremiumBuilding shader found=" + (shader != null) +
                      (shader ? " supported=" + shader.isSupported : ""));
            var testQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            testQuad.name = "ShaderProbeQuad";
            testQuad.transform.position = new Vector3(0f, 30f, 28f);
            testQuad.transform.localScale = new Vector3(10f, 10f, 1f);
            var probeMat = new Material(shader) { name = "ProbeMat" };
            probeMat.SetColor("_BaseColor", new Color(1f, 0f, 0f, 1f));
            testQuad.GetComponent<Renderer>().sharedMaterial = probeMat;
            PosePose(cam, quad.transform, new Vector2(0f, 30f), 12f, 34f);
            var probeR = testQuad.GetComponent<Renderer>();
            var probeMf = testQuad.GetComponent<MeshFilter>();
            Debug.Log("[CaptureTest] probe pos=" + testQuad.transform.position + " scale=" + testQuad.transform.lossyScale +
                      " renderer=" + (probeR ? ("enabled=" + probeR.enabled + " mat=" + probeR.sharedMaterial.name) : "MISSING") +
                      " mesh=" + (probeMf && probeMf.sharedMesh ? ("v" + probeMf.sharedMesh.vertexCount + " bounds=" + probeMf.sharedMesh.bounds) : "MISSING") +
                      " worldBounds=" + (probeR ? probeR.bounds.ToString() : "-"));
            SaveCapture(cam, "frontal_capture5e_shader_probe.png");
            quad.SetActive(false);
            SaveCapture(cam, "frontal_capture5e2_probe_no_backdrop.png");
            quad.SetActive(true);
            probeMat.shader = Shader.Find("Universal Render Pipeline/Unlit");
            SaveCapture(cam, "frontal_capture5e3_probe_unlit.png");
            Object.DestroyImmediate(testQuad);

            // Building pushed 6 units in FRONT of the backdrop plane, backdrop ON.
            PosePose(cam, quad.transform, buildingAim, 12f, 34f);
            buildRoot.transform.position = new Vector3(origPos.x, origPos.y, 24f);
            SaveCapture(cam, "frontal_capture5f_building_infront_withbackdrop.png");
            buildRoot.transform.position = origPos;
            PosePose(cam, quad.transform, new Vector2(buildingAim.x - 25f, buildingAim.y), 12f, 54f);
            SaveCapture(cam, "frontal_capture6_building_pan_left.png");
            PosePose(cam, quad.transform, new Vector2(buildingAim.x + 25f, buildingAim.y), 12f, 54f);
            SaveCapture(cam, "frontal_capture7_building_pan_right.png");
            PosePose(cam, quad.transform, buildingAim, 12f, 20f);
            SaveCapture(cam, "frontal_capture8_building_forward.png");
            PosePose(cam, quad.transform, buildingAim, 12f, 110f);
            SaveCapture(cam, "frontal_capture9_building_backward.png");
            PosePose(cam, quad.transform, buildingAim, 18f, 30f);
            SaveCapture(cam, "frontal_capture10_building_aligned.png");

            Debug.Log("[CaptureTest] done");
            EditorApplication.Exit(0);
        }

        private static void PosePose(Camera cam, Transform quad, Vector2 anchor, float pitchDeg, float distance)
        {
            float rad = pitchDeg * Mathf.Deg2Rad;
            Vector3 forward = new Vector3(0f, -Mathf.Sin(rad), Mathf.Cos(rad));
            Vector3 aim = new Vector3(anchor.x, anchor.y, BuildingPerspectiveCamera.BackdropZ);
            Vector3 pos = aim - forward * distance;
            cam.transform.position = pos;
            cam.transform.rotation = Quaternion.Euler(pitchDeg, 0f, 0f);
            cam.fieldOfView = 55f;

            float planeH = AnchorMarker.PlaneWidth * (1229f / 2048f);
            Vector3 pinned = new Vector3(0f, planeH * 0.5f, AnchorMarker.BackdropZ);
            quad.localPosition = cam.transform.InverseTransformPoint(pinned);
            quad.localScale = new Vector3(AnchorMarker.PlaneWidth, planeH, 1f);

            Debug.Log("[CaptureTest] pose anchor(" + anchor.x + "," + anchor.y + ") pitch " + pitchDeg + " dist " + distance +
                      " -> quad world " + quad.position + " local " + quad.localPosition + " scale " + quad.localScale +
                      " camera world " + pos + " rot " + cam.transform.eulerAngles);
        }

        private static void DumpBuilding()
        {
            var root = GameObject.Find("BuildingPremium");
            if (!root)
            {
                Debug.LogError("[CaptureTest] BuildingPremium root NOT FOUND in scene");
                return;
            }
            Debug.Log("[CaptureTest] building root pos=" + root.transform.position + " active=" + root.activeSelf);
            int rCount = 0, bad = 0;
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                rCount++;
                var mf = mr.GetComponent<MeshFilter>();
                bool okMesh = mf && mf.sharedMesh && mf.sharedMesh.vertexCount > 0 && mf.sharedMesh.subMeshCount > 0 && mf.sharedMesh.triangles.Length > 0;
                bool okMat = mr.sharedMaterial && mr.sharedMaterial.shader;
                bool okBounds = okMesh && mf.sharedMesh.bounds.size.sqrMagnitude > 0.001f;
                if (!okMesh || !okMat || !okBounds) bad++;
                var msize = okMesh ? mf.sharedMesh.bounds.size.ToString("F2") : "-";
                Debug.Log("[CaptureTest]   renderer " + mr.gameObject.name +
                          " mesh=" + (okMesh ? ("v" + mf.sharedMesh.vertexCount + " tri=" + mf.sharedMesh.triangles.Length + " bounds=" + msize) : "BROKEN") +
                          " mat=" + (okMat ? mr.sharedMaterial.name : "NO-MAT") +
                          " shader=" + (okMat ? mr.sharedMaterial.shader.name : "NONE") +
                          " pos=" + mr.transform.position.ToString("F2") + " scale=" + mr.transform.lossyScale.ToString("F2") +
                          " enabled=" + mr.enabled + " rendererOn=" + mr.gameObject.activeInHierarchy);
            }
            Debug.Log("[CaptureTest]   total renderers=" + rCount + " bad=" + bad);
        }

        private static void SaveCapture(Camera cam, string fileName)
        {
            int w = 1280, h = 720;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            cam.Render();

            var oldActive = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = oldActive;
            cam.targetTexture = null;

            string path = Path.Combine(OutDir, fileName);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log("[CaptureTest] wrote " + path + " (" + new FileInfo(path).Length + " bytes)");
        }

        // ----------------------------------------------------------------------------------
        // PLAY MODE validation run (artwork building). Enters play mode, drives the SAME
        // validated camera controller (BuildingPerspectiveCamera) through the standard
        // poses (front / pan left / pan right / zoom in / zoom out / aligned), captures
        // each one, plus a building-off reference of the front pose for transparency /
        // halo / rectangle checks. Run in batch (NO -nographics):
        //   Unity.exe -batchmode -quit -projectPath ... -executeMethod BeeKingdom.Experiments.Environment2D5D.FrontalCaptureTest.PlayModeRun
        // ----------------------------------------------------------------------------------
        private static int _pmStage;
        private static int _pmTick;
        private static int _pmPoseIdx;
        private static bool _pmBuildingOff;
        private static string _pmLastFile;
        private static bool _pmPlayModeOptionsChanged;
        private static float _pmStartTime;
        private static BuildingPremiumController _pmBuilding;
        private static bool _pmValidationDisabled;
        private static readonly Vector4[] PmPoses =
        {
            new Vector4(35f, 18.003f, 12f, 34f),   // 1 front
            new Vector4(15f, 18.003f, 12f, 34f),   // 2 pan left (same distance => same size)
            new Vector4(55f, 18.003f, 12f, 34f),   // 3 pan right (same distance => same size)
            new Vector4(35f, 18.003f, 12f, 20f),   // 4 zoom in
            new Vector4(35f, 18.003f, 12f, 110f),  // 5 zoom out
            new Vector4(35f, 18.003f, 18f, 30f)    // 6 aligned
        };
        // Each pose is captured TWICE: building ON then building OFF at the SAME camera
        // pose, so every verification has a same-pose reference (the painting moves with
        // the camera: a front-pose reference is NOT comparable at other poses).
        private static readonly string[][] PmFiles =
        {
            new[] { "play_capture1_front_ON.png", "play_capture2_front_OFF.png" },
            new[] { "play_capture3_pan_left_ON.png", "play_capture4_pan_left_OFF.png" },
            new[] { "play_capture5_pan_right_ON.png", "play_capture6_pan_right_OFF.png" },
            new[] { "play_capture7_zoom_in_ON.png", "play_capture8_zoom_in_OFF.png" },
            new[] { "play_capture9_zoom_out_ON.png", "play_capture10_zoom_out_OFF.png" },
            new[] { "play_capture11_aligned_ON.png", "play_capture12_aligned_OFF.png" }
        };

        public static void PlayModeRun()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            if (!scene.IsValid())
            {
                Debug.LogError("[CaptureTest] scene open FAILED");
                EditorApplication.Exit(2);
                return;
            }
            _pmStage = 0;
            _pmTick = 0;
            _pmPoseIdx = 0;
            _pmBuildingOff = false;
            _pmLastFile = null;
            // Batch-mode play: disable domain/scene reload so the static state machine
            // and update registration survive the play transition (restored on exit).
            _pmPlayModeOptionsChanged = !EditorSettings.enterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions =
                EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
            _pmStartTime = (float)EditorApplication.timeSinceStartup;
            Debug.Log("[CaptureTest] PlayModeRun: entering play mode (no domain reload)");
            EditorApplication.update += PlayModeUpdate;
            EditorApplication.isPlaying = true;
        }

        private static void PlayModeUpdate()
        {
            if (_pmStage == 0)
            {
                _pmTick++;
                if (EditorApplication.timeSinceStartup - _pmStartTime > 240f)
                {
                    Debug.LogError("[CaptureTest] play mode never started (timeout)");
                    EditorApplication.Exit(5);
                    return;
                }
                if (!EditorApplication.isPlaying)
                {
                    if (_pmTick > 600) { Debug.LogError("[CaptureTest] play mode never started"); EditorApplication.Exit(3); }
                    return;
                }
                if (_pmTick < 15) return; // let Awake/Start/LateUpdate settle
                // Stop the auto anchor-validation camera exercise: it would fight the
                // explicit poses below (scene is left untouched, disabled at runtime).
                if (!_pmValidationDisabled)
                {
                    var av = GameObject.Find("AnchorValidation");
                    if (av) { av.SetActive(false); Debug.Log("[CaptureTest] AnchorValidation exercise disabled for captures"); }
                    _pmValidationDisabled = true;
                }
                var buildingCtrls = Object.FindObjectsByType<BuildingPremiumController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                _pmBuilding = buildingCtrls.Length > 0 ? buildingCtrls[0] : null;
                _pmStage = 1;
                _pmTick = 0;
                Debug.Log("[CaptureTest] play mode active" + (_pmBuilding ? " (building found)" : " (NO BUILDING)"));
            }
            else if (_pmStage == 1)
            {
                if (EditorApplication.timeSinceStartup - _pmStartTime > 300f)
                {
                    Debug.LogError("[CaptureTest] play-mode capture run timed out");
                    EditorApplication.Exit(6);
                    return;
                }
                _pmTick++;
                if (_pmPoseIdx >= PmPoses.Length)
                {
                    _pmStage = 2;
                    return;
                }
                Vector4 p = PmPoses[_pmPoseIdx];
                bool isOffPhase = (_pmTick >= 8);
                string name = PmFiles[_pmPoseIdx][isOffPhase ? 1 : 0];

                if (_pmTick == 2)
                {
                    // Tick 2: apply the pose and ensure the building is ON, then let the
                    // next game frame(s) apply the camera pose.
                    var cam = Object.FindFirstObjectByType<Camera>();
                    var ctrl = cam ? cam.GetComponent<BuildingPerspectiveCamera>() : null;
                    if (ctrl) ctrl.ApplyPose(new Vector2(p.x, p.y), p.z, p.w);
                    if (_pmBuilding && !_pmBuilding.gameObject.activeSelf)
                    {
                        _pmBuilding.gameObject.SetActive(true);
                        _pmBuildingOff = false;
                    }
                    return;
                }
                if (_pmTick == 8)
                {
                    // Tick 8: same pose, building OFF for the same-pose reference.
                    if (_pmBuilding && _pmBuilding.gameObject.activeSelf)
                    {
                        _pmBuilding.gameObject.SetActive(false);
                        _pmBuildingOff = true;
                    }
                    return;
                }
                if (_pmTick != 6 && _pmTick != 12) return; // capture ON at 6, OFF at 12
                if (isOffPhase && _pmTick != 12) return;

                Transform buildT = _pmBuilding ? _pmBuilding.transform : null;
                var cam2 = Object.FindFirstObjectByType<Camera>();
                Debug.Log("[CaptureTest] pose " + _pmPoseIdx + " " + name + " anchor(" + p.x + "," + p.y + ") pitch " + p.z + " dist " + p.w +
                          (isOffPhase ? " building=OFF" : " building=ON") +
                          " camera=" + (cam2 ? cam2.transform.position.ToString("F2") : "NONE") +
                          " building=" + (buildT ? (buildT.position.ToString("F2") + " active=" + buildT.gameObject.activeSelf) : "NONE"));
                if (cam2)
                {
                    Vector3 contactWorld = buildT ? buildT.position : new Vector3(p.x, p.y, 30f);
                    Debug.Log("[CaptureTest]   contactWorld=" + contactWorld.ToString("F3") + " screen=" + cam2.WorldToScreenPoint(contactWorld).ToString("F1"));
                    SaveCapture(cam2, name);
                }
                if (isOffPhase)
                {
                    _pmPoseIdx++;
                    _pmTick = 0;
                    if (_pmBuildingOff) { _pmBuilding.gameObject.SetActive(true); _pmBuildingOff = false; }
                }
            }
            else if (_pmStage == 2)
            {
                _pmTick++;
                if (_pmTick == 1)
                {
                    Debug.Log("[CaptureTest] play mode run done");
                    EditorApplication.update -= PlayModeUpdate;
                    if (_pmPlayModeOptionsChanged)
                    {
                        EditorSettings.enterPlayModeOptionsEnabled = false;
                        _pmPlayModeOptionsChanged = false;
                    }
                    var build = GameObject.Find("BuildingPremium");
                    if (build && !build.activeSelf) build.SetActive(true);
                    EditorApplication.isPlaying = false;
                }
                // Keep requesting the exit every tick: in batch mode the update callbacks
                // can stop right after play mode ends, so the last successful call wins.
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError("[CaptureTest] invalid play-mode state " + _pmStage);
                EditorApplication.update -= PlayModeUpdate;
                EditorApplication.Exit(4);
            }
        }
    }
}
