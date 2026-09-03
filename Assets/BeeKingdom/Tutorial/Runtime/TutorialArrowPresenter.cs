using UnityEngine;

namespace BeeKingdom.Tutorial
{
    public sealed class TutorialArrowPresenter : MonoBehaviour
    {
        private string _targetId;
        private Camera _cam;
        private RectTransform _uiTarget;
        private float _anim;
        private bool _visible;

        private static Texture2D _arrowTex;
        private static Texture2D ArrowTexture
        {
            get
            {
                if (_arrowTex != null) return _arrowTex;
                _arrowTex = new Texture2D(64, 64, TextureFormat.ARGB32, false);
                Color c = new Color(1f, 0.85f, 0.2f, 1f);
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) _arrowTex.SetPixel(x, y, new Color(0,0,0,0));
                // simple triangle arrow
                for (int y = 8; y < 56; y++)
                {
                    int w = (int)((y - 8) * 0.7f);
                    for (int x = 32 - w; x < 32 + w; x++) if (x>=0 && x<64) _arrowTex.SetPixel(x, y, c);
                }
                _arrowTex.Apply();
                return _arrowTex;
            }
        }

        public void Show(string targetId)
        {
            _targetId = targetId;
            _visible = !string.IsNullOrEmpty(targetId);
            _cam = Camera.main;
            if (_cam == null) _cam = FindAnyObjectByType<Camera>();
        }

        public void Hide()
        {
            _visible = false;
            _targetId = null;
        }

        private void Update()
        {
            _anim += Time.unscaledDeltaTime * 3f;
            if (_cam == null) _cam = Camera.main;
        }

        private void OnGUI()
        {
            if (!_visible || string.IsNullOrEmpty(_targetId)) return;
            if (!TutorialTargetRegistry.Instance.TryGetTargetPosition(_targetId, _cam, out Vector2 screenPos, out RectTransform uiRect))
            {
                // fallback: if we have ui rect directly, use it
                if (TutorialTargetRegistry.Instance.TryResolveUi(_targetId, out var rt) && rt != null)
                {
                    Vector3[] corners = new Vector3[4];
                    rt.GetWorldCorners(corners);
                    Vector3 center = (corners[0] + corners[2]) * 0.5f;
                    screenPos = new Vector2(center.x, center.y);
                    // corners already in screen space for overlay
                    screenPos = new Vector2((corners[0].x + corners[2].x)*0.5f, Screen.height - (corners[0].y + corners[2].y)*0.5f);
                }
                else return;
            }

            // bounce animation
            float bounce = Mathf.Sin(_anim) * 10f;
            Rect r = new Rect(screenPos.x - 24, screenPos.y - 60 + bounce, 48, 48);
            // glow
            Color prev = GUI.color;
            GUI.color = new Color(1f, 0.9f, 0.3f, 0.85f + Mathf.Sin(_anim*1.5f)*0.15f);
            // rotate 180 to point down to target
            Matrix4x4 old = GUI.matrix;
            GUIUtility.RotateAroundPivot(180f, r.center);
            GUI.DrawTexture(r, ArrowTexture, ScaleMode.ScaleToFit, true);
            GUI.matrix = old;
            GUI.color = prev;

            // optional pulse ring around target for building
            if (uiRect == null)
            {
                Rect pulse = new Rect(screenPos.x - 30, screenPos.y - 30, 60, 60);
                float a = 0.3f + Mathf.Sin(_anim)*0.2f;
                GUI.color = new Color(1f, 0.85f, 0.2f, a);
                GUI.DrawTexture(pulse, Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
                GUI.color = Color.white;
                // draw border
                GUI.color = new Color(1f, 0.6f, 0.1f, 0.9f);
                GUI.Box(pulse, GUIContent.none);
                GUI.color = prev;
            }
        }
    }
}
