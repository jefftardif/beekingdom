using BeeKingdom.Playground;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class WorldMapZoomAnchorTests
    {
        private static void AssertFixed(Vector2 world, Vector2 expected,
            Vector2 center, float zoom, Vector2 viewport, string context)
        {
            Vector2 actual = WorldMapZoomAnchor.ScreenPoint(world, center,
                zoom, viewport);
            Assert.That(Vector2.Distance(actual, expected),
                Is.LessThan(.09f), context + ": terrain and icon drifted under the cursor");
        }

        [Test]
        public void MouseAndTouchZoomKeepWorldPointFixedEachFrame()
        {
            foreach (Vector2 viewport in new[] {
                new Vector2(1920,1080), new Vector2(1080,1920),
                new Vector2(720,1280)
            })
            foreach (float from in new[] { .35f, .75f, 1.3f, 2.4f })
            foreach (float to in new[] { .35f, .9f, 1.8f, 2.6f })
            {
                Vector2 center = new Vector2(16500f,16100f);
                Vector2 cursor = new Vector2(viewport.x*.67f, viewport.y*.41f);
                float zoom = from;
                Vector2 world = WorldMapZoomAnchor.WorldPoint(cursor,center,zoom,viewport);
                Vector2 targetCenter=WorldMapZoomAnchor.CenterForCursor(world,cursor,to,viewport);
                for (int frame=0;frame<65;frame++)
                {
                    WorldMapZoomAnchor.Step(ref center,ref zoom,targetCenter,to,10f,1f/60f);
                    AssertFixed(world,cursor,center,zoom,viewport,
                        "viewport="+viewport+", from="+from+", to="+to+", frame="+frame);
                }
            }
        }

        [Test]
        public void ScrollDuringUnfinishedZoomDoesNotRebaseOnOldTarget()
        {
            Vector2 viewport=new Vector2(1280,720);
            Vector2 cursor=new Vector2(920,310);
            Vector2 center=new Vector2(16300,16000);
            float zoom=.65f;
            Vector2 firstWorld=WorldMapZoomAnchor.WorldPoint(cursor,center,zoom,viewport);
            Vector2 target=WorldMapZoomAnchor.CenterForCursor(firstWorld,cursor,1.7f,viewport);
            for(int i=0;i<5;i++)
                WorldMapZoomAnchor.Step(ref center,ref zoom,target,1.7f,10f,1f/60f);
            Vector2 currentWorld=WorldMapZoomAnchor.WorldPoint(cursor,center,zoom,viewport);
            target=WorldMapZoomAnchor.CenterForCursor(currentWorld,cursor,2.45f,viewport);
            for(int i=0;i<55;i++)
            {
                WorldMapZoomAnchor.Step(ref center,ref zoom,target,2.45f,10f,1f/60f);
                AssertFixed(currentWorld,cursor,center,zoom,viewport,
                    "repeated wheel zoom frame="+i);
            }
        }

        [Test]
        public void WorldCoordsAndHitProjectionStayTogetherDuringZoom()
        {
            Vector2 world=new Vector2(16548.5f,16079.25f);
            Vector2 original=world;
            Vector2 viewport=new Vector2(960,600);
            Vector2 cursor=new Vector2(290,490);
            Vector2 center=new Vector2(16500,16000);
            float zoom=.5f;
            Vector2 targetCenter=WorldMapZoomAnchor.CenterForCursor(
                WorldMapZoomAnchor.WorldPoint(cursor,center,zoom,viewport),
                cursor,2.2f,viewport);
            for(int i=0;i<40;i++)
            {
                WorldMapZoomAnchor.Step(ref center,ref zoom,targetCenter,2.2f,10f,.02f);
                Vector2 screen=WorldMapZoomAnchor.ScreenPoint(world,center,zoom,viewport);
                Vector2 hitWorld=WorldMapZoomAnchor.WorldPoint(screen,center,zoom,viewport);
                Assert.That(Vector2.Distance(hitWorld,world),Is.LessThan(.006f),
                    "world-to-pixel and hit-to-world must use the same projection");
            }
            Assert.That(world,Is.EqualTo(original),
                "camera changes must never mutate entity/server world coordinates");
        }

        [Test]
        public void LegacyIndependentPanZoomEasingWasNotCursorStable()
        {
            Vector2 viewport=new Vector2(1280f,720f);
            Vector2 center=new Vector2(16000f,16200f);
            float zoom=.6f;
            Vector2 cursor=new Vector2(925f,290f);
            Vector2 world=WorldMapZoomAnchor.WorldPoint(cursor,center,zoom,viewport);
            Vector2 target=WorldMapZoomAnchor.CenterForCursor(world,cursor,2f,viewport);
            Vector2 oldCenter=Vector2.Lerp(center,target,1f-Mathf.Exp(-14f/60f));
            float oldZoom=Mathf.Lerp(zoom,2f,1f-Mathf.Exp(-10f/60f));
            float oldDrift=Vector2.Distance(
                WorldMapZoomAnchor.ScreenPoint(world,oldCenter,oldZoom,viewport),cursor);
            Assert.That(oldDrift,Is.GreaterThan(3f),
                "regression test should detect former independent pan/zoom damping");
            WorldMapZoomAnchor.Step(ref center,ref zoom,target,2f,10f,1f/60f);
            AssertFixed(world,cursor,center,zoom,viewport,"corrected camera");
        }
    }
}