using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace BeeKingdom.Experiments.Environment2D5D
{
    public class AnchorMarkerUI : MonoBehaviour
    {
        [Header("References")]
        public BuildingPerspectiveCamera cameraController;
        public AnchorMarker[] markers;
        public GameObject backdrop;
        public AnchorMarker buildingAnchor;

        [Header("UI Elements")]
        public Canvas canvas;
        public Text modeText;
        public Text viewText;
        public Text selectedText;
        public Text positionText;
        public Text debugText;
        public Button modeButton;
        public Button lowButton;
        public Button mediumButton;
        public Button highButton;
        public Button debugButton;
        public Button backdropButton;
        public Button resetButton;

        private bool _markerMode;
        private int _selectedIndex;
        private bool _backdropVisible = true;
        private bool _debugEnabled;

        private void Awake()
        {
            if (markers != null && markers.Length > 0)
            {
                markers[0].SetSelected(true);
            }
            SetupUI();
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.mKey.wasPressedThisFrame) ToggleMode();
            // B = BUILDING_TEST_ANCHOR selection (building premium test). The old B =
            // backdrop toggle stays available through its button.
            if (kb.bKey.wasPressedThisFrame)
            {
                if (buildingAnchor) SelectMarker(System.Array.IndexOf(markers, buildingAnchor));
                else ToggleBackdrop();
            }
            if (kb.gKey.wasPressedThisFrame) ToggleDebug();

            if (_markerMode)
            {
                if (cameraController) cameraController.inputEnabled = false;
                HandleMarkerInput(kb);
            }
            else
            {
                if (cameraController) cameraController.inputEnabled = true;
            }

            UpdateReadouts();
        }

        private void HandleMarkerInput(Keyboard kb)
        {
            if (markers == null || markers.Length == 0) return;

            if (kb.digit1Key.wasPressedThisFrame) SelectMarker(0);
            if (kb.digit2Key.wasPressedThisFrame) SelectMarker(1);
            if (kb.digit3Key.wasPressedThisFrame) SelectMarker(2);
            if (kb.leftBracketKey.wasPressedThisFrame) SelectMarker((_selectedIndex + markers.Length - 1) % markers.Length);
            if (kb.rightBracketKey.wasPressedThisFrame) SelectMarker((_selectedIndex + 1) % markers.Length);

            AnchorMarker m = markers[_selectedIndex];
            if (!m) return;

            // Anchors are glued to the backdrop surface: movement is along the painting
            // (X = left/right, Y = up/down), Z stays locked on BackdropZ. The anchor can
            // never leave the surface, so it can never slide relative to the landscape.
            float speed = (kb.leftShiftKey.isPressed ? 0.5f : 5f) * Time.deltaTime;
            Vector3 delta = Vector3.zero;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) delta.x -= speed;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) delta.x += speed;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) delta.y += speed;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) delta.y -= speed;
            if (delta != Vector3.zero) m.MoveLocal(delta);
        }

        private void SelectMarker(int index)
        {
            if (markers == null || markers.Length == 0) return;
            _selectedIndex = Mathf.Clamp(index, 0, markers.Length - 1);
            for (int i = 0; i < markers.Length; i++)
            {
                if (markers[i]) markers[i].SetSelected(i == _selectedIndex);
            }
            foreach (var m in markers)
            {
                if (m) m.RefreshLabel();
            }
        }

        private void ToggleMode()
        {
            _markerMode = !_markerMode;
            if (modeButton)
            {
                Text txt = modeButton.GetComponentInChildren<Text>();
                if (txt) txt.text = _markerMode ? "M - Mode: MARKER" : "M - Mode: CAMERA";
            }
        }

        private void ToggleBackdrop()
        {
            _backdropVisible = !_backdropVisible;
            if (backdrop) backdrop.SetActive(_backdropVisible);
            if (backdropButton)
            {
                Text txt = backdropButton.GetComponentInChildren<Text>();
                if (txt) txt.text = _backdropVisible ? "B - Hide Backdrop" : "B - Show Backdrop";
            }
        }

        private void ToggleDebug()
        {
            _debugEnabled = !_debugEnabled;
            if (debugButton)
            {
                Text txt = debugButton.GetComponentInChildren<Text>();
                if (txt) txt.text = _debugEnabled ? "G - Debug: ON" : "G - Debug: OFF";
            }
            if (debugText) debugText.gameObject.SetActive(_debugEnabled);
        }

        private void UpdateReadouts()
        {
            if (modeText) modeText.text = _markerMode ? "Mode: MARKER (move anchors)" : "Mode: CAMERA";

            if (!_markerMode && cameraController)
            {
                float p = cameraController.CurrentPitch;
                string preset = "CUSTOM";
                if (Mathf.Abs(p - cameraController.lowPitch) < 3f) preset = "LOW";
                else if (Mathf.Abs(p - cameraController.mediumPitch) < 3f) preset = "MEDIUM";
                else if (Mathf.Abs(p - cameraController.highPitch) < 3f) preset = "HIGH";
                if (viewText) viewText.text = $"Preset: {preset} | Pitch: {p:F1} deg | Zoom dist: {cameraController.CurrentDistance:F1} | FOV: {cameraController.CurrentFOV:F0}";
                if (selectedText) selectedText.text = "Selected: -";
                if (positionText) positionText.text = "Anchor: -";
            }
            else if (_markerMode && markers != null && markers.Length > 0 && markers[_selectedIndex])
            {
                AnchorMarker m = markers[_selectedIndex];
                if (selectedText) selectedText.text = $"Selected: {m.displayName} [{m.markerId}]  ({_selectedIndex + 1}/{markers.Length})";
                Vector3 p = m.transform.position;
                if (positionText)
                {
                    float distToBackdrop = cameraController ? Mathf.Max(0.1f, Mathf.Abs(30f - cameraController.transform.position.z)) : 1f;
                    float distToMarker = cameraController ? Mathf.Max(0.1f, Mathf.Abs(p.z - cameraController.transform.position.z)) : 1f;
                    positionText.text = $"Anchor: ({p.x:F2}, {p.y:F2}, {p.z:F2}) | Depth Z: {p.z:F2} | Scale: {distToMarker / distToBackdrop * 100f:F0}%";
                }
                m.RefreshLabel();
            }

            if (_debugEnabled && cameraController)
            {
                Vector3 pos = cameraController.transform.position;
                Vector3 rot = cameraController.transform.eulerAngles;
                float distToPlane = Mathf.Max(0f, 30f - pos.z);
                if (debugText)
                {
                    debugText.text =
                        $"DEBUG\n" +
                        $"FOV: {cameraController.CurrentFOV:F1} (fixed)\n" +
                        $"Zoom dist (view axis): {cameraController.CurrentDistance:F1}\n" +
                        $"Camera->backdrop dist: {distToPlane:F1}\n" +
                        $"Rotation (X,Y,Z): ({rot.x:F1}, {rot.y:F1}, {rot.z:F1})\n" +
                        $"Position: ({pos.x:F2}, {pos.y:F2}, {pos.z:F2})\n" +
                        $"Anchor: ({cameraController.CurrentAnchor.x:F1}, {cameraController.CurrentAnchor.y:F1})";
                }
            }
        }

        private void SetupUI()
        {
            if (!canvas)
            {
                GameObject canvasObj = new GameObject("AnchorTestUI");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObj.AddComponent<GraphicRaycaster>();
                canvasObj.transform.SetParent(transform);
            }

            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(15, -15);
            panelRect.sizeDelta = new Vector2(380, 320);
            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0, 0, 0, 0.8f);

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            Text title = CreateLabel(panel.transform, "ZOOM STABILITY + ANCHOR POINT TEST");
            title.fontSize = 14;
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(0.4f, 1f, 0.6f);
            title.alignment = TextAnchor.MiddleCenter;

            modeText = CreateLabel(panel.transform, "Mode: CAMERA");
            viewText = CreateLabel(panel.transform, "Preset: - | Pitch: - | Zoom: - | FOV: -");
            selectedText = CreateLabel(panel.transform, "Selected: -");
            positionText = CreateLabel(panel.transform, "Anchor: -");

            modeButton = CreateButton(panel.transform, "M - Mode: CAMERA", ToggleMode);
            lowButton = CreateButton(panel.transform, "1 - LOW view", () => cameraController?.MoveToPreset(cameraController.lowPitch, cameraController.lowAnchorY));
            mediumButton = CreateButton(panel.transform, "2 - MEDIUM view", () => cameraController?.MoveToPreset(cameraController.mediumPitch, cameraController.mediumAnchorY));
            highButton = CreateButton(panel.transform, "3 - HIGH view", () => cameraController?.MoveToPreset(cameraController.highPitch, cameraController.highAnchorY));
            debugButton = CreateButton(panel.transform, "G - Debug: OFF", ToggleDebug);
            backdropButton = CreateButton(panel.transform, "B - Hide Backdrop", ToggleBackdrop);
            resetButton = CreateButton(panel.transform, "0 - Reset View", () => cameraController?.ResetView());

            debugText = CreateLabel(panel.transform, "");
            debugText.fontSize = 11;
            debugText.color = new Color(0.4f, 1f, 0.8f);
            debugText.gameObject.SetActive(false);

            Text hint = CreateLabel(panel.transform,
                "CAMERA: WASD pan | R/F pitch | Wheel ZOOM (fixed angle, FOV 55) | 1/2/3 presets | 0 reset\nMARKER: M mode | 1/2/3 select | B = BUILDING anchor | WASD X/Y (ON surface, Z locked) | Shift fine | [ ] cycle\nBUILDING: Q/E height scale | O/P rotate | wheel zoom | G = debug | backdrop: button");
            hint.fontSize = 10;
            hint.color = new Color(0.7f, 0.7f, 0.7f);
        }

        private Text CreateLabel(Transform parent, string text)
        {
            GameObject obj = new GameObject("Label");
            obj.transform.SetParent(parent, false);
            Text t = obj.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 12;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleLeft;
            return t;
        }

        private Button CreateButton(Transform parent, string label, System.Action onClick)
        {
            GameObject obj = new GameObject("Btn_" + label);
            obj.transform.SetParent(parent, false);
            Button btn = obj.AddComponent<Button>();
            Image img = obj.AddComponent<Image>();
            img.color = new Color(0.15f, 0.2f, 0.3f, 0.95f);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            Text txt = textObj.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 11;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            RectTransform txtRect = textObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = new Vector2(8, 3);
            txtRect.offsetMax = new Vector2(-8, -3);

            btn.onClick.AddListener(() => onClick?.Invoke());
            return btn;
        }
    }
}
