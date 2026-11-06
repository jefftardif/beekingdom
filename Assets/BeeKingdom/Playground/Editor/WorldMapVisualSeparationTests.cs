using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class WorldMapVisualSeparationTests
    {
        private static WorldMapVisualSeparation.Claim Hive(Vector2 point) =>
            new WorldMapVisualSeparation.Claim {
                Key="hive:JEFF",Anchor=point,
                ArtWidth=100,ArtHeight=100,CaptionSize=new Vector2(130,62),
                Priority=0
            };
        private static WorldMapVisualSeparation.Claim Spider(Vector2 point) =>
            new WorldMapVisualSeparation.Claim {
                Key="bestiary:spider-T3",Anchor=point,
                ArtWidth=80,ArtHeight=80,CaptionSize=new Vector2(170,45),
                Priority=5
            };
        private static WorldMapVisualSeparation.Claim Floral(Vector2 point) =>
            new WorldMapVisualSeparation.Claim {
                Key="floral:site",Anchor=point,ArtWidth=120,ArtHeight=88,
                ArtTopFraction=.52f,CaptionSize=new Vector2(160,22),
                CaptionAbove=true,ExtraBottom=17f,Priority=3
            };
        private static WorldMapVisualSeparation.Claim Resource(Vector2 point) =>
            new WorldMapVisualSeparation.Claim {
                Key="resource:yellow",Anchor=point,ArtWidth=92,ArtHeight=92,
                CaptionSize=new Vector2(110,38),Priority=4
            };

        private static void AssertDistinct(WorldMapVisualSeparation.Layout result)
        {
            Assert.That(result.IsClear,Is.True,
                "Unresolved geometry: "+string.Join(",",result.Unresolved));
            var list = new List<WorldMapVisualSeparation.Slot>(result.Slots.Values);
            for(int i=0;i<list.Count;i++)
                for(int j=i+1;j<list.Count;j++)
                    Assert.That(WorldMapVisualSeparation.Conflicts(list[i],list[j],10f),Is.False,
                        list[i].Key+" collided with "+list[j].Key);
        }

        [Test]
        public void ScreenshotScenarioSpiderUnderPlayerAndYellowMarkersAtFloralSite()
        {
            var claims = new [] {
                Hive(new Vector2(320,260)),
                Spider(new Vector2(324,264)),
                Floral(new Vector2(486,282)),
                Resource(new Vector2(484,280))
            };
            var solved=WorldMapVisualSeparation.Solve(claims,new Rect(8,32,940,610),10f);
            Assert.That(solved.Slots.Count,Is.EqualTo(4),"No icon may be discarded");
            AssertDistinct(solved);
            Assert.That(solved.Slots["hive:JEFF"].Point,
                Is.EqualTo(new Vector2(320,260)),"Player hive remains the priority anchor");
            Assert.That(solved.Slots["bestiary:spider-T3"].Relocated,Is.True);
            Assert.That(solved.Slots["floral:site"].Relocated ||
                solved.Slots["resource:yellow"].Relocated,Is.True);
            foreach(var claim in claims)
            {
                var slot=solved.Slots[claim.Key];
                Assert.That(solved.TryHit(slot.Art.center,out string iconHit),Is.True);
                Assert.That(iconHit,Is.EqualTo(claim.Key));
                if(slot.HasCaption) {
                    Assert.That(solved.TryHit(slot.Caption.center,out string captionHit),Is.True);
                    Assert.That(captionHit,Is.EqualTo(claim.Key));
                }
            }
        }

        [Test]
        public void VaryingZoomAndCameraKeepHitsAndDrawRectsTogether()
        {
            foreach(float zoom in new [] {.55f,.9f,1.4f,2.2f})
            foreach(float pan in new [] {-40f,0f,55f})
            {
                float x=430f+pan;
                var claims=new [] {
                    Hive(new Vector2(x,260)),
                    new WorldMapVisualSeparation.Claim {
                        Key="bestiary:t3",Anchor=new Vector2(x+8f*zoom,270f),
                        ArtWidth=75f*zoom,ArtHeight=75f*zoom,
                        CaptionSize=new Vector2(140,37),Priority=5
                    },
                    Floral(new Vector2(x+125f,270f)),
                    Resource(new Vector2(x+127f,271f))
                };
                var layout=WorldMapVisualSeparation.Solve(claims,new Rect(8,32,990,650));
                Assert.That(layout.IsClear,Is.True,
                    "zoom="+zoom+" pan="+pan+" unresolved="+string.Join(",",layout.Unresolved));
                var rows=new List<WorldMapVisualSeparation.Slot>(layout.Slots.Values);
                for(int i=0;i<rows.Count;i++)
                    for(int j=i+1;j<rows.Count;j++)
                        Assert.That(WorldMapVisualSeparation.Conflicts(rows[i],rows[j],10f),
                            Is.False, "zoom="+zoom+" pan="+pan+" "+rows[i].Key+
                            " ("+rows[i].Point+") versus "+rows[j].Key+
                            " ("+rows[j].Point+")");
                foreach(var slot in layout.Slots.Values)
                    Assert.That(layout.TryHit(slot.Art.center,out var key) && key==slot.Key,Is.True);
            }
        }

        [Test]
        public void StableRegardlessOfEnumerationOrderAndRepeatCalls()
        {
            var list=new List<WorldMapVisualSeparation.Claim> {
                Spider(new Vector2(300,300)),Resource(new Vector2(309,302)),
                Hive(new Vector2(310,315)),Floral(new Vector2(312,296))
            };
            Rect screen=new Rect(8,32,1100,710);
            var before=WorldMapVisualSeparation.Solve(list,screen);
            list.Reverse();
            var after=WorldMapVisualSeparation.Solve(list,screen);
            AssertDistinct(before);
            AssertDistinct(after);
            foreach(var entry in before.Slots)
            {
                Assert.That(after.Slots.ContainsKey(entry.Key),Is.True);
                Assert.That(after.Slots[entry.Key].Point,Is.EqualTo(entry.Value.Point));
            }
        }

        [Test]
        public void PinnedOfficialClaimsNeverChangeAuthorityPosition()
        {
            var a=Hive(new Vector2(300,270));
            a.Pinned=true;
            var b=Resource(new Vector2(303,274));
            b.Pinned=true;
            var result=WorldMapVisualSeparation.Solve(new [] {a,b},new Rect(8,32,850,640));
            Assert.That(result.Slots.Count,Is.EqualTo(2));
            Assert.That(result.IsClear,Is.False);
            Assert.That(result.Unresolved,Does.Contain("resource:yellow"),
                "Unresolved official overlap must remain auditable, not hidden");
            Assert.That(result.Slots[a.Key].Point,Is.EqualTo(a.Anchor));
            Assert.That(result.Slots[b.Key].Point,Is.EqualTo(b.Anchor));
        }

        [Test]
        public void NarrowMobileViewportKeepsAllAndReservesLabels()
        {
            var a=Hive(new Vector2(140,170));
            var b=Spider(new Vector2(151,181));
            var c=Floral(new Vector2(288,382));
            var layout=WorldMapVisualSeparation.Solve(new [] {a,b,c},new Rect(8,32,424,735));
            Assert.That(layout.Slots.Count,Is.EqualTo(3));
            AssertDistinct(layout);
        }
    }
}
