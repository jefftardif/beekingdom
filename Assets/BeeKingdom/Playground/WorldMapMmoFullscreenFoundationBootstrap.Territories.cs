using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed partial class WorldMapMmoFullscreenFoundationBootstrap
    {
        private void DrawOfficialAllianceTerritory()
        {
            if(Event.current.type!=EventType.Repaint) return;
            // Explicit adapter contract for the existing WorldToScreen coordinates.
            // A server configured in another unit/surface is never silently rescaled.
            string realm=MobileAccountSessionRuntimeBootstrap.TerritoryConfiguredMapWorld;
            var outline=MobileAccountSessionRuntimeBootstrap.TerritoryOutlineForMap(realm,WorldId,"wave6-world-coordinates");
            if(outline==null || outline.Ring==null || outline.Ring.Length<3) return;
            DrawAllianceTerritoryOutline(outline);
        }

        private void DrawAllianceTerritoryOutline(BeeKingdom.Networking.RemoteTerritoryOutline outline)
        {
            for(int i=0;i<outline.Ring.Length;i++)
            {
                var a=outline.Ring[i];var b=outline.Ring[(i+1)%outline.Ring.Length];
                Vector2 from=WorldToScreen(new Vector2((float)a.X,(float)a.Y));
                Vector2 to=WorldToScreen(new Vector2((float)b.X,(float)b.Y));
                DrawTerritorySegment(from,to,new Color(.10f,.14f,.08f,.8f),4.5f);
                DrawTerritorySegment(from,to,new Color(.96f,.78f,.34f,.95f),2f);
            }
            // Presentation only: no fill, no collider, no terrain replacement,
            // no synthetic headquarters marker in place of the real building.
        }

        private void DrawTerritorySegment(Vector2 from, Vector2 to, Color color, float width)
        {
            if (from == to) return;
            // Axis-aligned edges need no GUI rotation. Rotating textured rects can
            // clip their ends in narrow Game/Editor viewports, even inside the view.
            if (from.x != to.x && from.y != to.y)
            {
                DrawLine(from, to, color, width);
                return;
            }
            EnsurePixel();
            Rect rect = from.x == to.x
                ? new Rect(from.x - width * .5f, Mathf.Min(from.y, to.y), width, Mathf.Abs(to.y - from.y))
                : new Rect(Mathf.Min(from.x, to.x), from.y - width * .5f, Mathf.Abs(to.x - from.x), width);
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, pixel);
            GUI.color = previous;
        }
    }
}
