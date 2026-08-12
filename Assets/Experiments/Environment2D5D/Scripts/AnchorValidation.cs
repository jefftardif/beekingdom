using UnityEngine;
using UnityEngine.InputSystem;

namespace BeeKingdom.Experiments.Environment2D5D
{
    // FINAL VALIDATION HARNESS for the anchor system.
    // READ-ONLY on anchors: it never writes an anchor position and never derives one from
    // the screen. Anchors stay independent world-space GameObjects; the only writers are
    // the user (MARKER mode) and the anchor's own world-lock (LateUpdate snap-back).
    //
    // Attachment criterion: each anchor must sit ON the backdrop surface (z = BackdropZ,
    // position computed from its image UV through the plane mapping). Distance
    // Anchor->Surface must stay ~0 (<= worldTolerance) during ANY camera movement;
    // otherwise the anchor slides relative to the painted landscape -> FAIL.
    //
    // Automated test (10 steps, auto-run, V = re-run):
    //  1. memorize each anchor's initial world position + initial screen position + camera pose
    //  2. camera strongly LEFT, 3. RIGHT, 4. FORWARD, 5. BACKWARD, 6. UP, 7. DOWN,
    //  8. PITCH change, 9. ZOOM IN then ZOOM OUT, 10. exact return to the initial camera pose.
    //  At every step: world positions unchanged, surface distance ~0, anchor screen position
    //  identical to its surface point's screen position; after the exact camera return the
    //  screen positions must match the initial ones within a minimal numeric tolerance.
    //  HUD shows PASS/FAIL per anchor, UV, anchor/surface world positions, max displacement,
    //  max surface distance, screen deltas.
    public class AnchorValidation : MonoBehaviour
    {
        [Header("Test")]
        public bool autoRunOnStart = true;
        public float initialDelay = 1.0f;
        public float stepDuration = 1.4f;
        public float settleFrames = 3f;

        [Header("Tolerances")]
        [Tooltip("Max allowed anchor displacement in world units (FAIL above this).")]
        public float worldTolerance = 0.001f;
        [Tooltip("Max allowed screen-position delta in pixels after exact camera return.")]
        public float screenTolerance = 0.01f;

        private struct Pose
        {
            public Vector2 anchor;
            public float pitch;
            public float distance;
        }

        private struct StepDef
        {
            public string label;
            public Pose target;
        }

        private BuildingPerspectiveCamera _cam;
        private AnchorMarker[] _markers;
        private GUIStyle _style;

        private StepDef[] _steps;
        private int _stepIndex;
        private float _stepTimer;
        private bool _running;
        private bool _finished;
        private string _overall = "NOT RUN";
        private int _settleCount;
        private bool _firstFrame = true;

        private bool _initialized;
        private bool _showHud;
        private Pose _initPose;
        private Vector3[] _initWorld;
        private Vector3[] _initScreen;
        private float[] _maxDisplacement;
        private float[] _maxSurfaceDistance;
        private float[] _maxSurfaceScreenDelta;
        private float[] _screenDelta;
        private bool[] _pass;

        private void Awake()
        {
            _cam = FindFirstObjectByType<BuildingPerspectiveCamera>();
            _markers = FindObjectsByType<AnchorMarker>(FindObjectsSortMode.None);
            if (!_cam || _markers == null || _markers.Length == 0)
            {
                _overall = "MISSING CAMERA OR MARKERS";
            }
        }

        private void Start()
        {
            // GUIStyle created lazily inside OnGUI: GUI.skin access is forbidden
            // outside OnGUI in Unity 6000 (throws ArgumentException otherwise).
        }

        private void LateUpdate()
        {
            if (_firstFrame)
            {
                _firstFrame = false;
                if (_cam && _markers != null && _markers.Length > 0)
                {
                    CaptureInitialState();
                    if (autoRunOnStart) StartTest();
                }
            }
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.vKey.wasPressedThisFrame && !_running && _initialized)
            {
                StartTest();
            }
            if (kb != null && kb.xKey.wasPressedThisFrame)
            {
                _showHud = !_showHud;
            }

            if (!_initialized) return;

            // Continuous world monitor (always on once initialized):
            //  - world displacement of each anchor vs its recorded initial position
            //  - Distance Anchor->Surface (the surface is the backdrop plane z = BackdropZ);
            //    must stay ~0, otherwise the anchor slides relative to the painted landscape.
            for (int i = 0; i < _markers.Length; i++)
            {
                Vector3 now = _markers[i].transform.position;
                float d = Vector3.Distance(now, _initWorld[i]);
                if (d > _maxDisplacement[i]) _maxDisplacement[i] = d;

                float sd = Mathf.Abs(now.z - AnchorMarker.BackdropZ);
                if (sd > _maxSurfaceDistance[i]) _maxSurfaceDistance[i] = sd;

                if (_cam && _cam.cameraComponent)
                {
                    float sDelta = Vector2.Distance(
                        Project(_markers[i]),
                        _cam.cameraComponent.WorldToScreenPoint(SurfacePoint(now)));
                    if (sDelta > _maxSurfaceScreenDelta[i]) _maxSurfaceScreenDelta[i] = sDelta;
                }
            }

            if (!_running) return;

            if (_stepIndex >= _steps.Length)
            {
                // Camera is back at the exact initial pose: wait a few frames for the
                // pipeline to settle, then compare screen positions.
                _settleCount++;
                if (_settleCount >= settleFrames) FinishTest();
                return;
            }

            _stepTimer += Time.deltaTime;
            StepDef step = _steps[_stepIndex];
            Pose current = ReadPose();
            float t = Mathf.Clamp01(_stepTimer / stepDuration);
            Pose lerped = new Pose
            {
                anchor = Vector2.Lerp(current.anchor, step.target.anchor, t),
                pitch = Mathf.Lerp(current.pitch, step.target.pitch, t),
                distance = Mathf.Lerp(current.distance, step.target.distance, t)
            };
            _cam.ApplyPose(lerped.anchor, lerped.pitch, lerped.distance);

            if (_stepTimer >= stepDuration)
            {
                _stepTimer = 0f;
                _stepIndex++;
            }
        }

        private void CaptureInitialState()
        {
            _initPose = ReadPose();
            _initWorld = new Vector3[_markers.Length];
            _initScreen = new Vector3[_markers.Length];
            _maxDisplacement = new float[_markers.Length];
            _maxSurfaceDistance = new float[_markers.Length];
            _maxSurfaceScreenDelta = new float[_markers.Length];
            _screenDelta = new float[_markers.Length];
            _pass = new bool[_markers.Length];

            for (int i = 0; i < _markers.Length; i++)
            {
                _initWorld[i] = _markers[i].transform.position;
                _initScreen[i] = Project(_markers[i]);
            }
            _initialized = true;
        }

        private void StartTest()
        {
            _running = true;
            _finished = false;
            _overall = "RUNNING";
            _stepIndex = 0;
            _stepTimer = 0f;
            _settleCount = 0;
            for (int i = 0; i < _markers.Length; i++)
            {
                _maxDisplacement[i] = 0f;
                _maxSurfaceDistance[i] = 0f;
                _maxSurfaceScreenDelta[i] = 0f;
                _screenDelta[i] = 0f;
                _pass[i] = false;
            }
            _initPose = ReadPose();
            BuildSteps();
        }

        private void BuildSteps()
        {
            Pose p = _initPose;
            _steps = new[]
            {
                MakeStep("2 LEFT (strong)", p, anchor: new Vector2(p.anchor.x - 25f, p.anchor.y)),
                MakeStep("3 RIGHT (strong)", p, anchor: new Vector2(p.anchor.x + 25f, p.anchor.y)),
                MakeStep("4 FORWARD", p, dist: p.distance * 0.35f),
                MakeStep("5 BACKWARD", p, dist: p.distance * 2.6f),
                MakeStep("6 UP", p, anchor: new Vector2(p.anchor.x, p.anchor.y + 20f)),
                MakeStep("7 DOWN", p, anchor: new Vector2(p.anchor.x, p.anchor.y - 20f)),
                MakeStep("8 PITCH", p, pitch: p.pitch + 30f),
                MakeStep("9 ZOOM IN", p, dist: p.distance * 0.4f),
                MakeStep("9b ZOOM OUT", p, dist: p.distance * 2.6f),
                MakeStep("10 RETURN TO START", p, anchor: p.anchor, pitch: p.pitch, dist: p.distance)
            };
        }

        private StepDef MakeStep(string label, Pose from, Vector2? anchor = null, float? pitch = null, float? dist = null)
        {
            Pose t = from;
            if (anchor.HasValue) t.anchor = anchor.Value;
            if (pitch.HasValue) t.pitch = pitch.Value;
            if (dist.HasValue) t.distance = dist.Value;
            return new StepDef { label = label, target = t };
        }

        private void FinishTest()
        {
            _running = false;
            _finished = true;
            bool allPass = true;
            for (int i = 0; i < _markers.Length; i++)
            {
                _screenDelta[i] = Vector2.Distance(Project(_markers[i]), _initScreen[i]);
                _pass[i] = _maxDisplacement[i] <= worldTolerance &&
                           _maxSurfaceDistance[i] <= worldTolerance &&
                           _maxSurfaceScreenDelta[i] <= screenTolerance &&
                           _screenDelta[i] <= screenTolerance;
                if (!_pass[i]) allPass = false;
            }
            _overall = allPass ? "PASS" : "FAIL";
            if (allPass)
            {
                Debug.Log("[AnchorValidation] PASS - anchors ON surface and world-locked: max surface distance " +
                          MaxAll(_maxSurfaceDistance) + " u, max displacement " + MaxAll(_maxDisplacement) +
                          " u (tolerance " + worldTolerance + "), screen return within " + screenTolerance + " px.");
            }
            else
            {
                Debug.LogError("[AnchorValidation] FAIL - see HUD for per-anchor surface distance / displacement / screen delta.");
            }
        }

        private float MaxAll(float[] values)
        {
            float m = 0f;
            for (int i = 0; i < values.Length; i++) m = Mathf.Max(m, values[i]);
            return m;
        }

        private Vector3 SurfacePoint(Vector3 p)
        {
            return new Vector3(p.x, p.y, AnchorMarker.BackdropZ);
        }

        private Pose ReadPose()
        {
            return new Pose
            {
                anchor = _cam.CurrentAnchor,
                pitch = _cam.CurrentPitch,
                distance = _cam.CurrentDistance
            };
        }

        private Vector3 Project(AnchorMarker m)
        {
            if (!_cam || !_cam.cameraComponent) return Vector3.zero;
            return _cam.cameraComponent.WorldToScreenPoint(m.transform.position);
        }

        private void OnGUI()
        {
            // HUD hidden by default so the frontal backdrop stays fully visible; X toggles it.
            if (!_initialized || !_showHud) return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.box);
                _style.richText = true;
                _style.fontSize = 12;
                _style.padding = new RectOffset(10, 10, 8, 8);
            }

            string s = "<b>ANCHOR VALIDATION</b> (auto at start | V = re-run)\n";

            if (_running && _stepIndex < _steps.Length)
            {
                s += $"Step {_stepIndex + 1}/10: <b>{_steps[_stepIndex].label}</b> | pose anchor ({_initPose.anchor.x:F1},{_initPose.anchor.y:F1}) pitch {_initPose.pitch:F1} dist {_initPose.distance:F1}\n";
            }
            else
            {
                s += $"Camera pose: anchor ({_cam.CurrentAnchor.x:F1},{_cam.CurrentAnchor.y:F1}) pitch {_cam.CurrentPitch:F1} dist {_cam.CurrentDistance:F1} | FOV {_cam.CurrentFOV:F1}\n";
            }
            s += "\n";

            for (int i = 0; i < _markers.Length; i++)
            {
                AnchorMarker m = _markers[i];
                Vector3 now = m.transform.position;
                Vector3 surf = SurfacePoint(now);
                Vector2 uv = m.SurfaceUV;
                bool surfaceOk = _maxSurfaceDistance[i] <= worldTolerance && _maxSurfaceScreenDelta[i] <= screenTolerance;
                string status = _finished
                    ? (_pass[i] ? "<color=#7CFC00>PASS</color>" : "<color=#FF5555>FAIL</color>")
                    : (surfaceOk && _maxDisplacement[i] <= worldTolerance ? "<color=#7CFC00>OK</color>" : "<color=#FF5555>DRIFT!</color>");
                s += $"{m.displayName} [{m.markerId}]\n";
                s += $"  UV ({uv.x:F4}, {uv.y:F4}) | anchor ({now.x:F3}, {now.y:F3}, {now.z:F3}) | surface ({surf.x:F3}, {surf.y:F3}, {surf.z:F3})\n";
                s += $"  distSurf {_maxSurfaceDistance[i]:F6} u | maxDisp {_maxDisplacement[i]:F6} u | screenΔ {_screenDelta[i]:F4} px | {status}\n";
            }
            s += "\n";
            s += $"<b>VERDICT: {(_finished ? (_overall == "PASS" ? "<color=#7CFC00>PASS</color>" : "<color=#FF5555>FAIL</color>") : _overall)}</b>" +
                 $" | tolerance monde {worldTolerance:F3} u | tolerance écran {screenTolerance:F2} px\n";
            if (_running) s += "Do not touch the keyboard during the run.\n";

            GUI.Box(new Rect(Screen.width - 690f, 10f, 680f, 290f), s, _style);
        }
    }
}
