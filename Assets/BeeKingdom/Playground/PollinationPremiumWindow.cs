using System;
using System.Collections.Generic;
using BeeKingdom.Localization;
using BeeKingdom.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BeeKingdom.Playground
{
    /// <summary>
    /// P7: shared Activities-v4 canvas density, Cinzel header, royal cards,
    /// single vertical scrollbar and touch-safe input blocker. All actions
    /// delegate to the P5 server-authoritative controller: this screen
    /// never computes rewards or owns an independent economy.
    /// </summary>
    public sealed partial class PollinationPremiumWindow : MonoBehaviour
    {
        public const int RoyalSortingOrder = 520;
        private static readonly Color Gold = new Color(1f, .77f, .36f, 1f);
        private static readonly Color Ink = new Color(.055f, .037f, .024f, 1f);
        private static readonly Color Paper = new Color(1f, .95f, .82f, 1f);
        private static readonly Color Muted = new Color(.80f, .73f, .61f, 1f);
        private static readonly Color Card = new Color(.15f, .095f, .045f, 1f);
        private static readonly Color Panel = new Color(.085f, .060f, .036f, 1f);

        private IPollinationPanelController controller;
        private Action onClose;
        private Canvas canvas;
        private RectTransform content;
        private ScrollRect scroller;
        private GameObject ownedEventSystem;
        private TMP_FontAsset cinzel;
        private float viewportHeight;
        private float viewportWidth;
        private float nextRefreshAt;
        private Vector2 previousScreenSize;
        private Rect previousSafeArea;
        private string previousLanguage = string.Empty;
        private string previousSiteId = string.Empty;
        private int activeTab;
        private bool open;
        private IReadOnlyList<RemoteFloralSite> previousSites;
        private IReadOnlyList<RemoteFloralAllianceProjectSnapshot> previousProjects;
        private RemoteCivilPopulationSnapshot previousPopulation;
        private RemoteCivilExpeditionSnapshot previousExpedition;
        private PollinationScreenState previousState;
        private readonly List<LiveText> liveLabels = new List<LiveText>();
        private readonly List<LiveButton> liveButtons = new List<LiveButton>();

        private sealed class LiveText
        {
            public TextMeshProUGUI Widget;
            public Func<string> Read;
        }
        private sealed class LiveButton
        {
            public Button Widget;
            public Func<bool> Enabled;
        }

        public bool IsOpen => open;
        public int ActiveTabForProof => activeTab;
        public Canvas CanvasForProof => canvas;
        public ScrollRect ScrollForProof => scroller;
        public bool HasRaycastBlockerForProof =>
            canvas != null && canvas.gameObject.activeInHierarchy &&
            canvas.transform.Find("Backdrop")?.GetComponent<RawImage>()?.raycastTarget == true;

        public static Rect HeaderForProof(float safeWidth,float safeHeight) =>
            HiveMapPremiumBuildingWindow.HeaderRectForProof(safeWidth,safeHeight);
        public static Rect ViewportForProof(float safeWidth,float safeHeight) =>
            HiveMapPremiumBuildingWindow.ScrollViewportForProof(safeWidth,safeHeight);

        private static bool French =>
            string.IsNullOrEmpty(BeeLocalization.CurrentLocale) ||
            BeeLocalization.CurrentLocale.StartsWith("fr", StringComparison.OrdinalIgnoreCase);

        private static string T(string fr,string en) => French ? fr : en;

        public void Show(IPollinationPanelController source, Action close)
        {
            if (source == null || !source.IsConfigured)
                return;
            controller = source;
            onClose = close;
            open = true;
            activeTab = 0;
            previousSiteId = string.Empty;
            nextRefreshAt = 0f;
            BeeLocalization.LocaleChanged -= OnRoyalLocaleChanged;
            BeeLocalization.LocaleChanged += OnRoyalLocaleChanged;
            EnsureEventSystem();
            BuildWindow();
        }

        public void Hide()
        {
            BeeLocalization.LocaleChanged -= OnRoyalLocaleChanged;
            open = false;
            onClose = null;
            controller = null;
            liveLabels.Clear();
            liveButtons.Clear();
            if (canvas != null) canvas.gameObject.SetActive(false);
            if (ownedEventSystem != null) ownedEventSystem.SetActive(false);
        }

        private void OnDestroy()
        {
            BeeLocalization.LocaleChanged -= OnRoyalLocaleChanged;
            open = false;
            canvas = null;
            ownedEventSystem = null;
            liveLabels.Clear();
            liveButtons.Clear();
        }

        // Locale changes are an event, not a timer. Refresh immediately so
        // the premium window never displays stale FR/EN labels after a switch.
        private void OnRoyalLocaleChanged(string locale)
        {
            if (!open || controller == null || !isActiveAndEnabled) return;
            float previousScroll = scroller != null
                ? scroller.verticalNormalizedPosition : 1f;
            BuildWindow();
            if (scroller != null)
                scroller.verticalNormalizedPosition = previousScroll;
        }

        private void Update()
        {
            if (!open || controller == null) return;
            if (!controller.IsConfigured)
            {
                Action close = onClose;
                Hide();
                close?.Invoke();
                return;
            }
            Vector2 size = new Vector2(Screen.width,Screen.height);
            if (size != previousScreenSize || Screen.safeArea != previousSafeArea
                || previousLanguage != BeeLocalization.CurrentLocale)
            {
                BuildWindow();
                return;
            }
            if (NeedsBodyUpdate()) BuildBody(false);
            if (Time.unscaledTime < nextRefreshAt) return;
            nextRefreshAt = Time.unscaledTime + .2f;
            RefreshVisibleValues();
        }

        private bool NeedsBodyUpdate()
        {
            PollinationScreenModel m = controller.Model;
            return !ReferenceEquals(previousSites,m.Sites)
                || !ReferenceEquals(previousProjects,m.Projects)
                || !ReferenceEquals(previousPopulation,m.Population)
                || !ReferenceEquals(previousExpedition,m.Expedition)
                || previousSiteId != m.SelectedSiteId
                || previousState != m.State;
        }

        private void CaptureReferences()
        {
            PollinationScreenModel m = controller.Model;
            previousSites = m.Sites;
            previousProjects = m.Projects;
            previousPopulation = m.Population;
            previousExpedition = m.Expedition;
            previousSiteId = m.SelectedSiteId;
            previousState = m.State;
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            if (ownedEventSystem == null)
            {
                ownedEventSystem = new GameObject("Pollination UI Input",
                    typeof(EventSystem));
                ownedEventSystem.transform.SetParent(transform,false);
#if ENABLE_INPUT_SYSTEM
                ownedEventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                ownedEventSystem.AddComponent<StandaloneInputModule>();
#endif
            }
            ownedEventSystem.SetActive(true);
        }

        private void BuildWindow()
        {
            if (!open || controller == null) return;
            if (canvas == null)
            {
                RectTransform root = Node(transform,"PollinationPremiumCanvas",new Rect());
                canvas = root.gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = true;
                canvas.sortingOrder = RoyalSortingOrder;
                root.gameObject.AddComponent<GraphicRaycaster>();
                var scaler = root.gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                cinzel = Resources.Load<TMP_FontAsset>("Cinzel-Regular SDF");
            }
            canvas.gameObject.SetActive(true);
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                GameObject old = canvas.transform.GetChild(i).gameObject;
                old.SetActive(false);
                if (Application.isPlaying) Destroy(old);
                else DestroyImmediate(old);
            }
            previousScreenSize = new Vector2(Screen.width,Screen.height);
            previousSafeArea = Screen.safeArea;
            previousLanguage = BeeLocalization.CurrentLocale;
            float scale = HiveMapPremiumBuildingWindow.PremiumCanvasScale(previousScreenSize);
            canvas.GetComponent<CanvasScaler>().scaleFactor = scale;
            Rect safe = HiveMapPremiumBuildingWindow.PremiumSafeCanvasRect(
                previousScreenSize,previousSafeArea,scale);
            float w = safe.width, h = safe.height;

            // RaycastTarget is essential: no queue/bees/troops/world-map controls
            // can receive UI pointer events behind this fullscreen canvas.
            Plate(canvas.transform,"Backdrop",
                new Rect(0f,0f,Screen.width/scale,Screen.height/scale),
                Ink,"cave-background",true);
            RectTransform rootArea = Node(canvas.transform,"SafeArea",safe);
            Rect headerRect = HeaderForProof(w,h);
            RectTransform header = Plate(rootArea,"Header",headerRect,Ink,"panel-fill");
            Texture2D art = Resources.Load<Texture2D>(
                "PremiumBeeReference/BuildingBanners/alliance_future_hall");
            if (art != null)
            {
                var artwork = Node(header,"Artwork",new Rect(
                    w*.40f,0f,w*.60f,headerRect.height));
                RawImage banner = artwork.gameObject.AddComponent<RawImage>();
                banner.texture = art;
                banner.color = new Color(1f,1f,1f,.30f);
                banner.raycastTarget = false;
            }
            bool landscape = w > h && w >= 600f;
            ButtonAt(header,new Rect(16f,12f,86f,48f),
                T("Retour","Back"),false,RequestClose);
            ButtonAt(header,new Rect(Mathf.Max(108f,w-145f),12f,
                    Mathf.Min(128f,w-124f),44f),
                T("Actualiser","Refresh"),false,()=>controller?.Refresh(),
                ()=>controller != null && controller.IsConfigured && !controller.IsBusy);
            TextMeshProUGUI title = Label(header,landscape
                    ? new Rect(120f,12f,Mathf.Max(100f,w-262f),39f)
                    : new Rect(18f,headerRect.height-65f,w-36f,37f),
                T("POLLINISATION","POLLINATION"),landscape?26f:23f,Gold,true,true);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            Label(header,landscape
                    ? new Rect(122f,53f,Mathf.Max(100f,w-262f),23f)
                    : new Rect(20f,headerRect.height-27f,w-40f,22f),
                T("Écosystème du royaume","Kingdom Ecology"),13f,Paper);
            Plate(header,"GoldRule",new Rect(0f,headerRect.height-1f,w,1f),
                Gold,"etched-line");

            // Same exact screen-density and scrollbar geometry as Activities v4.
            Rect view = ViewportForProof(w,h);
            RectTransform viewport = Node(rootArea,"Viewport",view);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image hit = viewport.gameObject.AddComponent<Image>();
            hit.color=Color.clear;
            hit.raycastTarget=true;
            content = Node(viewport,"PollinationCards",new Rect(
                0f,0f,view.width,0f));
            content.anchorMin=new Vector2(0f,1f);
            content.anchorMax=new Vector2(1f,1f);
            content.sizeDelta=Vector2.zero;
            scroller = viewport.gameObject.AddComponent<ScrollRect>();
            scroller.viewport=viewport;
            scroller.content=content;
            scroller.horizontal=false;
            scroller.vertical=true;
            scroller.movementType=ScrollRect.MovementType.Clamped;
            scroller.scrollSensitivity=32f;
            scroller.decelerationRate=.10f;
            Rect track = HiveMapPremiumBuildingWindow.ScrollTrackForProof(w,h);
            RectTransform rail = Plate(rootArea,"ScrollTrack",track,Panel,"panel-fill");
            RectTransform sliding = Node(rail,"Sliding",
                new Rect(1f,1f,Mathf.Max(1f,track.width-2f),
                    Mathf.Max(1f,track.height-2f)));
            RectTransform handle = Node(sliding,"ScrollHandle",new Rect());
            handle.anchorMin=Vector2.zero;
            handle.anchorMax=Vector2.one;
            handle.offsetMin=handle.offsetMax=Vector2.zero;
            Image handleImg=handle.gameObject.AddComponent<Image>();
            handleImg.color=Gold;
            handleImg.raycastTarget=true;
            Scrollbar scrollbar=rail.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction=Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect=handle;
            scrollbar.targetGraphic=handleImg;
            scroller.verticalScrollbar=scrollbar;
            scroller.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            viewportWidth=view.width;
            viewportHeight=view.height;
            BuildBody(true);
        }

        private void RequestClose()
        {
            Action action = onClose;
            Hide();
            action?.Invoke();
        }

        public void SelectTabForProof(int tab)
        {
            if (!open || tab < 0 || tab > 2) return;
            activeTab=tab;
            BuildBody(true);
        }

        private static RectTransform Node(Transform parent,string name,Rect rect)
        {
            RectTransform node = new GameObject(name,typeof(RectTransform))
                .GetComponent<RectTransform>();
            node.SetParent(parent,false);
            node.anchorMin=node.anchorMax=new Vector2(0f,1f);
            node.pivot=new Vector2(0f,1f);
            node.anchoredPosition=new Vector2(rect.x,-rect.y);
            node.sizeDelta=rect.size;
            return node;
        }

        private static RectTransform Plate(Transform parent,string name,Rect rect,
            Color fill,string material,bool raycast=false)
        {
            RectTransform node=Node(parent,name,rect);
            RawImage image=node.gameObject.AddComponent<RawImage>();
            image.texture=Texture2D.whiteTexture;
            image.color=fill;
            image.raycastTarget=raycast;
            Texture2D texture=HiveViewProductUiPresenter.BuildingWindowMaterial(
                material=="panel-fill" ? "panel-grain" : material);
            if(texture != null)
            {
                var grain=Node(node,"Material",new Rect(0f,0f,rect.width,rect.height));
                var raw=grain.gameObject.AddComponent<RawImage>();
                raw.rectTransform.anchorMin=Vector2.zero;
                raw.rectTransform.anchorMax=Vector2.one;
                raw.rectTransform.offsetMin=raw.rectTransform.offsetMax=Vector2.zero;
                raw.texture=texture;
                raw.color=new Color(1f,1f,1f,.16f);
                raw.raycastTarget=false;
            }
            return node;
        }

        private TextMeshProUGUI Label(Transform parent,Rect rect,string value,
            float fontSize,Color color,bool bold=false,bool display=false)
        {
            var label=Node(parent,"Label",rect).gameObject
                .AddComponent<TextMeshProUGUI>();
            if(display && cinzel!=null) label.font=cinzel;
            label.text=value ?? string.Empty;
            label.fontSize=fontSize;
            label.color=color;
            label.fontStyle=bold?FontStyles.Bold:FontStyles.Normal;
            label.richText=false;
            label.raycastTarget=false;
            label.textWrappingMode=TextWrappingModes.Normal;
            label.overflowMode=TextOverflowModes.Ellipsis;
            return label;
        }

        private Button ButtonAt(Transform parent,Rect rect,string text,
            bool primary,Action action,Func<bool> enabled=null)
        {
            RectTransform node=Plate(parent,"Button",rect,
                primary?new Color(.80f,.49f,.13f):new Color(.22f,.15f,.07f),
                "panel-fill",true);
            var graphic=node.GetComponent<RawImage>();
            Button button=node.gameObject.AddComponent<Button>();
            button.targetGraphic=graphic;
            var caption=Label(node,new Rect(4f,0f,rect.width-8f,rect.height),
                text,15f,primary?Ink:Paper,true);
            caption.alignment=TextAlignmentOptions.Center;
            button.onClick.AddListener(()=>action?.Invoke());
            if (enabled != null)
                liveButtons.Add(new LiveButton{Widget=button,Enabled=enabled});
            return button;
        }

        private TextMeshProUGUI LiveLabel(Transform parent,Rect rect,
            Func<string> value,float size,Color color,bool bold=false)
        {
            var label=Label(parent,rect,string.Empty,size,color,bold);
            liveLabels.Add(new LiveText{Widget=label,Read=value});
            return label;
        }

        private void RefreshVisibleValues()
        {
            foreach(LiveText label in liveLabels)
            {
                if(label.Widget == null) continue;
                string value=label.Read?.Invoke() ?? string.Empty;
                if(label.Widget.text != value) label.Widget.text=value;
            }
            foreach(LiveButton button in liveButtons)
                if(button.Widget != null)
                    button.Widget.interactable=button.Enabled == null
                        || button.Enabled();
        }

        private RectTransform CardAt(float top,float height,string title,
            string iconName="nursery")
        {
            float left=18f,width=Mathf.Max(160f,viewportWidth-36f);
            var card=Plate(content,"Card",new Rect(left,top,width,height),
                Card,"panel-fill");
            Texture2D icon=Resources.Load<Texture2D>("PremiumBeeIcons/"+iconName);
            if (icon!=null)
            {
                var node=Node(card,"Icon",new Rect(18f,10f,48f,48f));
                var raw=node.gameObject.AddComponent<RawImage>();
                raw.texture=icon;
                raw.raycastTarget=false;
            }
            Label(card,new Rect(78f,16f,Mathf.Max(60f,width-96f),41f),
                title.ToUpperInvariant(),17f,Gold,true);
            Plate(card,"Separator",new Rect(18f,64f,width-36f,1f),
                new Color(.43f,.29f,.12f),"etched-line");
            return card;
        }

        private void SetContentHeight(float bottom)
        {
            if(content != null)
                content.sizeDelta=new Vector2(0f,
                    Mathf.Max(bottom+28f,viewportHeight+1f));
        }
    }
}
