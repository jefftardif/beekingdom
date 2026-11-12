using UnityEngine;

namespace BeeKingdom.Playground
{
    internal static class WorldMapHudLayout
    {
        private static GUIStyle buttonStyle, barStyle, shadowStyle;
        private static GUIStyle labelStyle;
        private static Texture2D returnGlyph;
        private static Texture2D idleTexture, hoverTexture, pressedTexture, selectedTexture, barTexture, shadowTexture;
        private static readonly Color Gold = new Color(1f, .77f, .36f, 1f);
        private static readonly Color Paper = new Color(1f, .95f, .82f, 1f);

        // All rects use GUI top-left coordinates. Convert the device safe area once.
        internal static Rect SafeGuiRect(float width, float height, Rect deviceSafe)
        {
            if (deviceSafe.width <= 0f || deviceSafe.height <= 0f)
                return new Rect(0f, 0f, width, height);
            return new Rect(deviceSafe.x, height - deviceSafe.yMax, deviceSafe.width, deviceSafe.height);
        }

        internal static Rect NavigationBar(float width, float height, Rect deviceSafe)
        {
            Rect safe = SafeGuiRect(width, height, deviceSafe);
            float w = Mathf.Min(760f, Mathf.Max(1f, safe.width - 16f));
            return new Rect(safe.center.x - w * .5f, safe.yMax - 126f, w, 118f);
        }

        internal static Rect NavigationButton(Rect bar, int index, int count)
        {
            const float gap = 6f;
            float w = (bar.width - 16f - gap * (count - 1)) / count;
            return new Rect(bar.x + 8f + index * (w + gap), bar.y + 28f, w, 82f);
        }

        internal static Rect CurrentBar => NavigationBar(Screen.width, Screen.height, Screen.safeArea);
        internal static Rect ChatFeed(Rect bar,Rect safe)
        {
            float bottom=Mathf.Max(safe.yMin,bar.y-8f);
            float height=Mathf.Min(104f,Mathf.Max(0f,bottom-safe.yMin-8f));
            return new Rect(bar.x,bottom-height,bar.width,height);
        }
        internal static Rect CurrentChatFeed => ChatFeed(CurrentBar,SafeGuiRect(Screen.width,Screen.height,Screen.safeArea));
        internal static Rect CurrentChatSettings => WithinSafeArea(new Rect(CurrentBar.x,CurrentBar.y-240f,CurrentBar.width,232f));
        internal static void DrawChatFeedSurface(Rect panel,float alpha=.64f)
        {
            EnsureStyles();Color saved=GUI.color;GUI.color=new Color(1,1,1,alpha);
            Surface(panel,barTexture,24);GUI.color=saved;
        }
        internal static int ButtonCount => MobileAccountSessionRuntimeBootstrap.PollinationFeatureEnabledForWorldMap ? 6 : 5;
        internal static Rect CurrentButton(int index) => NavigationButton(CurrentBar, index, ButtonCount);

        internal static Rect Pollination(float width, float height) =>
            NavigationButton(NavigationBar(width,height,new Rect(0,0,width,height)), 3, ButtonCount);
        internal static Rect ChatButton(float width, float height) =>
            NavigationButton(NavigationBar(width,height,new Rect(0,0,width,height)), 0, ButtonCount);
        internal static Rect MarchCounter(float width, float height)
        {
            Rect bar = NavigationBar(width,height,new Rect(0,0,width,height));
            return new Rect(bar.x+12f,bar.y+6f,120f,18f);
        }
        internal static Rect ChatPanel(float width, float height)
        {
            Rect bar = NavigationBar(width,height,new Rect(0,0,width,height));
            float w = Mathf.Min(520f,width-24f);
            float h = Mathf.Min(360f,Mathf.Max(1f,bar.y-16f));
            return new Rect(bar.x,bar.y-8f-h,w,h);
        }
        internal static Rect RelativeToSafeChat(Rect rect)
        {
            Rect nominal = NavigationBar(Screen.width,Screen.height,new Rect(0,0,Screen.width,Screen.height));
            rect.position += CurrentBar.position-nominal.position;
            return WithinSafeArea(rect);
        }
        internal static Rect WithinSafeArea(Rect rect)
        {
            Rect safe = SafeGuiRect(Screen.width,Screen.height,Screen.safeArea);
            rect.width = Mathf.Min(rect.width,Mathf.Max(1f,safe.width-16f));
            rect.height = Mathf.Min(rect.height,Mathf.Max(1f,safe.height-16f));
            rect.x = Mathf.Clamp(rect.x,safe.xMin+8f,Mathf.Max(safe.xMin+8f,safe.xMax-rect.width-8f));
            rect.y = Mathf.Clamp(rect.y,safe.yMin+8f,Mathf.Max(safe.yMin+8f,safe.yMax-rect.height-8f));
            return rect;
        }

        private static void Fill(Rect rect, Color color)
        {
            Color saved=GUI.color; GUI.color=color;
            GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color=saved;
        }
        private static void Surface(Rect rect,Texture2D texture,int corner)
        {
            // Nine-slice retains round ends at both phone and desktop widths.
            GUIStyle style=texture==barTexture?barStyle:shadowStyle;
            if(Event.current.type==EventType.Repaint)style.Draw(rect,GUIContent.none,false,false,false,false);
        }
        private static void DrawArtwork(Rect box,Texture2D icon)
        {
            Rect pixels=new Rect(0,0,icon.width,icon.height);
            // Existing reviewed artwork, fitted to its opaque envelope; source PNGs stay intact.
            if(icon.name=="nouvelle_discussion" && icon.width==1337)pixels=new Rect(95,138,1180,889);
            else if(icon.name=="loupe" && icon.width==1360)pixels=new Rect(68,73,1200,1051);
            else if(icon.name=="hive-starter" && icon.width==1254)pixels=new Rect(337,323,586,605);
            else if(icon.name=="field_bloom" && icon.width==332)pixels=new Rect(33,39,276,165);
            float scale=Mathf.Min(box.width/pixels.width,box.height/pixels.height);
            Rect fitted=new Rect(box.center.x-pixels.width*scale*.5f,box.center.y-pixels.height*scale*.5f,pixels.width*scale,pixels.height*scale);
            GUI.DrawTextureWithTexCoords(fitted,icon,new Rect(pixels.x/icon.width,pixels.y/icon.height,pixels.width/icon.width,pixels.height/icon.height));
        }
        private static void EnsureStyles()
        {
            if(buttonStyle!=null && idleTexture!=null)return;
            idleTexture=BeeKingdom.LivingHiveMenu.PremiumNavigationStyle.RoundedSurface(new Color(.17f,.115f,.065f,1f),new Color(.07f,.047f,.028f,1f),new Color(.40f,.28f,.13f,1f),20);
            hoverTexture=BeeKingdom.LivingHiveMenu.PremiumNavigationStyle.RoundedSurface(new Color(.26f,.18f,.085f,1f),new Color(.11f,.073f,.033f,1f),Gold,20);
            pressedTexture=BeeKingdom.LivingHiveMenu.PremiumNavigationStyle.RoundedSurface(new Color(.10f,.065f,.03f,1f),new Color(.28f,.18f,.065f,1f),Gold,20);
            selectedTexture=BeeKingdom.LivingHiveMenu.PremiumNavigationStyle.RoundedSurface(new Color(.30f,.21f,.09f,1f),new Color(.12f,.077f,.03f,1f),Gold,20);
            barTexture=BeeKingdom.LivingHiveMenu.PremiumNavigationStyle.RoundedSurface(new Color(.12f,.085f,.045f,.98f),new Color(.045f,.032f,.02f,.98f),new Color(.61f,.43f,.19f,1f),24);
            shadowTexture=BeeKingdom.LivingHiveMenu.PremiumNavigationStyle.RoundedSurface(new Color(0,0,0,.30f),new Color(0,0,0,.45f),Color.clear,24);
            barStyle=new GUIStyle { border=new RectOffset(24,24,24,24) };barStyle.normal.background=barTexture;
            shadowStyle=new GUIStyle { border=new RectOffset(24,24,24,24) };shadowStyle.normal.background=shadowTexture;
            buttonStyle=new GUIStyle(GUI.skin.button) { border=new RectOffset(24,24,24,24),padding=new RectOffset(),margin=new RectOffset() };
            buttonStyle.normal.background=idleTexture;buttonStyle.hover.background=hoverTexture;
            buttonStyle.active.background=pressedTexture;buttonStyle.focused.background=hoverTexture;
            labelStyle=new GUIStyle(GUI.skin.label) { alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,clipping=TextClipping.Clip };
        }
        private static float ReturnStrokeDistance(Vector2 point,Vector2 a,Vector2 b)
        {
            Vector2 delta=b-a;
            return Vector2.Distance(point,a+delta*Mathf.Clamp01(Vector2.Dot(point-a,delta)/delta.sqrMagnitude));
        }
        private static Texture2D CreateReturnGlyph()
        {
            const int size=64;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false) { hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp };
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                Vector2 p=new Vector2(x+.5f,y+.5f);
                float distance=Mathf.Min(ReturnStrokeDistance(p,new Vector2(20,32),new Vector2(44,32)),Mathf.Min(ReturnStrokeDistance(p,new Vector2(20,32),new Vector2(32,44)),ReturnStrokeDistance(p,new Vector2(20,32),new Vector2(32,20))));
                Color gold=Color.Lerp(new Color(.72f,.43f,.10f,1),new Color(1f,.91f,.62f,1),y/63f);
                gold=Color.Lerp(gold,new Color(.48f,.28f,.065f,1),Mathf.Clamp01(distance-1.5f)*.45f);
                gold.a=Mathf.Clamp01(3.4f-distance);pixels[y*size+x]=gold;
            }
            texture.SetPixels(pixels);texture.Apply();return texture;
        }
        private static Sprite returnIdleSprite, returnHoverSprite, returnPressedSprite;
        internal static Rect ReturnHitRect(Rect visual)
        {
            float width=Mathf.Max(52f,visual.width),height=Mathf.Max(52f,visual.height);
            return new Rect(visual.center.x-width*.5f,visual.center.y-height*.5f,width,height);
        }
        private static void EnsureReturnTextures()
        {
            if(returnGlyph==null)returnGlyph=CreateReturnGlyph();
            if(selectedTexture==null)selectedTexture=BeeKingdom.LivingHiveMenu.PremiumNavigationStyle.RoundedSurface(new Color(.30f,.21f,.09f,1f),new Color(.12f,.077f,.03f,1f),Gold,20);
            if(hoverTexture==null)hoverTexture=BeeKingdom.LivingHiveMenu.PremiumNavigationStyle.RoundedSurface(new Color(.26f,.18f,.085f,1f),new Color(.11f,.073f,.033f,1f),Gold,20);
            if(pressedTexture==null)pressedTexture=BeeKingdom.LivingHiveMenu.PremiumNavigationStyle.RoundedSurface(new Color(.10f,.065f,.03f,1f),new Color(.28f,.18f,.065f,1f),Gold,20);
        }
        private static Sprite ReturnSprite(Texture2D texture) => Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100f,0,SpriteMeshType.FullRect,new Vector4(24,24,24,24));
        internal static void StyleReturnButton(UnityEngine.UI.Button button)
        {
            if(button==null)return;
            EnsureReturnTextures();
            if(returnIdleSprite==null)returnIdleSprite=ReturnSprite(selectedTexture);
            if(returnHoverSprite==null)returnHoverSprite=ReturnSprite(hoverTexture);
            if(returnPressedSprite==null)returnPressedSprite=ReturnSprite(pressedTexture);
            var image=button.targetGraphic as UnityEngine.UI.Image;
            if(image==null)return;
            image.sprite=returnIdleSprite;image.type=UnityEngine.UI.Image.Type.Sliced;image.color=Color.white;
            button.transition=UnityEngine.UI.Selectable.Transition.SpriteSwap;
            button.spriteState=new UnityEngine.UI.SpriteState { highlightedSprite=returnHoverSprite,selectedSprite=returnHoverSprite,pressedSprite=returnPressedSprite,disabledSprite=returnIdleSprite };
            var rect=(RectTransform)button.transform;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,52f);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,52f);
            foreach(var label in button.GetComponentsInChildren<TMPro.TMP_Text>(true))label.enabled=false;
            foreach(var label in button.GetComponentsInChildren<UnityEngine.UI.Text>(true))label.enabled=false;
            if(button.transform.Find("PremiumReturnGlyph")!=null)return;
            var go=new GameObject("PremiumReturnGlyph",typeof(RectTransform),typeof(UnityEngine.UI.RawImage));
            go.transform.SetParent(button.transform,false);var glyphRect=(RectTransform)go.transform;
            glyphRect.anchorMin=glyphRect.anchorMax=new Vector2(.5f,.5f);glyphRect.sizeDelta=new Vector2(38f,38f);glyphRect.anchoredPosition=Vector2.zero;
            var glyph=go.GetComponent<UnityEngine.UI.RawImage>();glyph.texture=returnGlyph;glyph.raycastTarget=false;
        }
        internal static bool DrawChatReturnButton(Rect visual)
        {
            EnsureStyles();
            EnsureReturnTextures();
            Rect hit=ReturnHitRect(visual);
            Surface(new Rect(hit.x,hit.y+2f,hit.width,hit.height),shadowTexture,24);
            buttonStyle.normal.background=selectedTexture;
            bool clicked=GUI.Button(hit,new GUIContent(string.Empty,"Retour"),buttonStyle);
            Color saved=GUI.color;GUI.color=GUI.enabled?Color.white:new Color(1,1,1,.4f);
            GUI.DrawTexture(new Rect(hit.center.x-19f,hit.center.y-19f,38f,38f),returnGlyph,ScaleMode.ScaleToFit,true);
            GUI.color=saved;return clicked;
        }

        internal static void DrawNavigationBar(string title)
        {
            EnsureStyles(); Rect bar=CurrentBar;
            Surface(new Rect(bar.x-4f,bar.y+3f,bar.width+8f,bar.height+5f),shadowTexture,24);
            Surface(bar,barTexture,24);
            labelStyle.fontSize=10;labelStyle.normal.textColor=new Color(.80f,.73f,.61f,1f);
            // No navigation heading: the illustrated actions identify this bar.
        }
        internal static bool DrawNavigationButton(Rect rect,string text,string tooltip,Texture2D icon,bool selected=false)
        {
            EnsureStyles();
            Rect inner=new Rect(rect.x+1f,rect.y+1f,rect.width-2f,rect.height-2f);
            buttonStyle.normal.background=selected?selectedTexture:idleTexture;
            bool clicked=GUI.Button(inner,new GUIContent(string.Empty,tooltip),buttonStyle);
            Color saved=GUI.color;GUI.color=GUI.enabled?Color.white:new Color(1,1,1,.4f);
            if(icon!=null){float iconSize=Mathf.Min(50f,Mathf.Max(1f,rect.width-8f));DrawArtwork(new Rect(rect.center.x-iconSize*.5f,rect.y+5f,iconSize,50f),icon);}
            labelStyle.fontSize=rect.width<90f?10:12;labelStyle.normal.textColor=selected?Gold:Paper;
            GUI.Label(new Rect(rect.x+3f,rect.y+59f,rect.width-6f,18f),text,labelStyle);
            GUI.color=saved;
            if(selected)Fill(new Rect(rect.center.x-12f,rect.yMax-3f,24f,2f),Gold);
            return clicked;
        }
    }
}
