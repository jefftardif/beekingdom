using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class WorldTargetPanelLayoutTests
    {
        [TestCase(390,844,0,0,390,844)]
        [TestCase(1080,2400,0,40,1080,2330)]
        [TestCase(430,932,0,26,430,880)]
        [TestCase(1440,900,0,0,1440,900)]
        [TestCase(1920,1080,0,0,1920,1080)]
        [TestCase(800,420,0,0,800,420)]
        [TestCase(320,568,0,0,320,568)]
        public void TargetAndSquadAlwaysRemainWithinDeviceSafeArea(
            int width,int height,int x,int y,int safeWidth,int safeHeight)
        {
            var deviceSafe=new Rect(x,y,safeWidth,safeHeight);
            var guiSafe=new Rect(deviceSafe.x,height-deviceSafe.yMax,deviceSafe.width,deviceSafe.height);
            foreach(bool squad in new[] {false,true})
            {
                Rect panel=WorldTargetPanelGeometry.Calculate(width,height,
                    deviceSafe,squad);
                Assert.That(panel.width,Is.GreaterThan(40),"min width");
                Assert.That(panel.height,Is.GreaterThan(40),"min height");
                Assert.That(panel.xMin,Is.GreaterThanOrEqualTo(guiSafe.xMin+7.9f));
                Assert.That(panel.yMin,Is.GreaterThanOrEqualTo(guiSafe.yMin+7.9f));
                Assert.That(panel.xMax,Is.LessThanOrEqualTo(guiSafe.xMax-7.9f));
                Assert.That(panel.yMax,Is.LessThanOrEqualTo(guiSafe.yMax-7.9f));
            }
        }

        [TestCase(390,844)]
        [TestCase(1080,2400)]
        [TestCase(1440,900)]
        [TestCase(1920,1080)]
        [TestCase(800,420)]
        public void EveryPrimaryAndSecondaryActionHitboxFitsInsidePanel(
            int width,int height)
        {
            Rect deviceSafe=new Rect(0,0,width,height);
            bool compact=WorldTargetPanelGeometry.Compact(width,height,deviceSafe);
            Rect panel=WorldTargetPanelGeometry.Calculate(width,height,deviceSafe);
            float logicalW=compact?420:600;
            float scale=panel.width/logicalW;
            Rect primary=compact
                ?new Rect(20,494,380,47)
                :new Rect(20,458,315,46);
            Rect details=compact
                ?new Rect(20,553,184,36)
                :new Rect(347,458,110,46);
            Rect center=compact
                ?new Rect(215,553,185,36)
                :new Rect(467,458,113,46);
            foreach(Rect action in new[] {primary,details,center})
            {
                Rect absolute=new Rect(panel.x+action.x*scale,
                    panel.y+action.y*scale,action.width*scale,action.height*scale);
                Assert.That(absolute.xMin,Is.GreaterThanOrEqualTo(panel.xMin));
                Assert.That(absolute.yMin,Is.GreaterThanOrEqualTo(panel.yMin));
                Assert.That(absolute.xMax,Is.LessThanOrEqualTo(panel.xMax+.01f));
                Assert.That(absolute.yMax,Is.LessThanOrEqualTo(panel.yMax+.01f));
            }
            Assert.That(primary.Overlaps(details),Is.False);
            Assert.That(primary.Overlaps(center),Is.False);
            Assert.That(details.Overlaps(center),Is.False);
        }

        [Test]
        public void OfficialResourcesAndEnemiesUseOneCommonWindowGeometry()
        {
            Rect safe=new Rect(0,0,390,844);
            Rect resource=WorldTargetPanelGeometry.Calculate(390,844,safe);
            Rect enemy=WorldTargetPanelGeometry.Calculate(390,844,safe);
            Assert.That(enemy,Is.EqualTo(resource));
            Assert.That(resource.width,Is.GreaterThan(300));
        }
    }
}
