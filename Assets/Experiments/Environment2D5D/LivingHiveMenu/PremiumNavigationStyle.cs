using System.Collections.Generic;
using UnityEngine;
namespace BeeKingdom.LivingHiveMenu
{
    // Shared surface generator used by the approved IMGUI map and the uGUI hive rail.
    public static class PremiumNavigationStyle
    {
        private static readonly Dictionary<string,Sprite> surfaces=new Dictionary<string,Sprite>();
        private static readonly Dictionary<string,Sprite> artwork=new Dictionary<string,Sprite>();
        public static Sprite Surface(string kind)
        {
            if(surfaces.TryGetValue(kind,out Sprite cached)&&cached!=null)return cached;
            Color gold=new Color(1f,.77f,.36f,1f);
            Color top=new Color(.17f,.115f,.065f,1f),bottom=new Color(.07f,.047f,.028f,1f),rim=new Color(.40f,.28f,.13f,1f);
            int radius=20;
            if(kind=="hover"){top=new Color(.26f,.18f,.085f,1f);bottom=new Color(.11f,.073f,.033f,1f);rim=gold;}
            if(kind=="pressed"){top=new Color(.10f,.065f,.03f,1f);bottom=new Color(.28f,.18f,.065f,1f);rim=gold;}
            if(kind=="selected"){top=new Color(.30f,.21f,.09f,1f);bottom=new Color(.12f,.077f,.03f,1f);rim=gold;}
            if(kind=="bar"){top=new Color(.12f,.085f,.045f,.98f);bottom=new Color(.045f,.032f,.02f,.98f);rim=new Color(.61f,.43f,.19f,1f);radius=24;}
            Texture2D texture=RoundedSurface(top,bottom,rim,radius);
            Sprite sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100f,0,SpriteMeshType.FullRect,new Vector4(24,24,24,24));
            surfaces[kind]=sprite;return sprite;
        }
        public static Sprite Artwork(string itemId)
        {
            if(artwork.TryGetValue(itemId,out Sprite cached)&&cached!=null)return cached;
            string path=itemId=="SurfaceSwitch"?"RoyalChatIcons/loupe":itemId=="Communication"?"RoyalChatIcons/nouvelle_discussion":
                itemId=="Activities"?"PremiumBeeIcons/quests":itemId=="Bag"?"PremiumHiveNavigation/bag-v2":"PremiumHiveNavigation/more-v2";
            Texture2D texture=Resources.Load<Texture2D>(path);
            if(texture==null)return LivingHiveMenuVisuals.IconSprite(itemId=="SurfaceSwitch"?"world":itemId=="Communication"?"inbox":itemId=="Activities"?"quests":itemId=="Bag"?"inventory":"more");
            Rect pixels=new Rect(0,0,texture.width,texture.height);
            if(itemId=="SurfaceSwitch"&&texture.width==1360)pixels=new Rect(68,73,1200,1051);
            else if(itemId=="Communication"&&texture.width==1337)pixels=new Rect(95,138,1180,889);
            else if(itemId=="Activities")
                pixels=new Rect(0,texture.height*.17f,texture.width,texture.height*.83f); // Exclude the baked English caption; use localized TMP text.
            Sprite sprite=Sprite.Create(texture,pixels,new Vector2(.5f,.5f),100f,0,SpriteMeshType.FullRect);
            artwork[itemId]=sprite;return sprite;
        }
        public static Texture2D RoundedSurface(Color top, Color bottom, Color rim, int radius)
        {
            const int width=128,height=96;
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false) { hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp };
            var pixels=new Color[width*height];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                float dx=Mathf.Max(Mathf.Abs(x+.5f-width*.5f)-(width*.5f-radius),0f);
                float dy=Mathf.Max(Mathf.Abs(y+.5f-height*.5f)-(height*.5f-radius),0f);
                float distance=Mathf.Sqrt(dx*dx+dy*dy)-radius;
                // Straight sides share the same signed distance as the corner arcs.
                if(dx==0f && dy==0f)distance=-Mathf.Min(Mathf.Min(x+.5f,width-x-.5f),Mathf.Min(y+.5f,height-y-.5f));
                Color color=Color.Lerp(bottom,top,(float)y/(height-1));
                if(distance>-2f)color=Color.Lerp(color,rim,Mathf.Clamp01(distance+2f));
                color.a*=Mathf.Clamp01(-distance+.5f);pixels[y*width+x]=color;
            }
            texture.SetPixels(pixels);texture.Apply();return texture;
        }
    }
}
