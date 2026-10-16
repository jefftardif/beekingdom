using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class BKStudioNurseryPremiumCompatibilityTests
    {
        [TestCase(390f,844f)]
        [TestCase(844f,390f)]
        [TestCase(1920f,1080f)]
        public void PremiumUIFitsSafeAreaAndScrollbarDoesNotOverlap(
            float width,float height)
        {
            Vector2 screen = new Vector2(width,height);
            float scale = HiveMapPremiumBuildingWindow.PremiumCanvasScale(screen);
            Assert.That(scale, Is.InRange(.85f,1.6f));
            Rect safe=HiveMapPremiumBuildingWindow.PremiumSafeCanvasRect(
                screen,new Rect(0f,0f,width,height),scale);
            Rect header=HiveMapPremiumBuildingWindow.HeaderRectForProof(
                safe.width,safe.height);
            Rect body=HiveMapPremiumBuildingWindow.ScrollViewportForProof(
                safe.width,safe.height);
            Rect rail=HiveMapPremiumBuildingWindow.ScrollTrackForProof(
                safe.width,safe.height);
            Assert.That(header.width,Is.EqualTo(safe.width).Within(.01f));
            Assert.That(body.yMin,Is.GreaterThan(header.yMax));
            Assert.That(body.yMax,Is.LessThanOrEqualTo(safe.height));
            Assert.That(rail.xMin,Is.GreaterThan(body.xMax));
            Assert.That(rail.xMax,Is.LessThanOrEqualTo(safe.width));
        }

        [Test]
        public void SafeAreaRespectsNotchAndBottomInset()
        {
            Vector2 screen=new Vector2(390f,844f);
            float scale=HiveMapPremiumBuildingWindow.PremiumCanvasScale(screen);
            Rect safe=HiveMapPremiumBuildingWindow.PremiumSafeCanvasRect(
                screen,new Rect(0f,24f,390f,780f),scale);
            Assert.That(safe.yMin,Is.EqualTo(40f/scale).Within(.01f));
            Assert.That(safe.height,Is.EqualTo(780f/scale).Within(.01f));
        }

        [Test]
        public void UsesRealAvailableTexturesAndNoDeletedUIHelpers()
        {
            string path=Path.Combine(Application.dataPath,
                "BeeKingdom/Playground/HiveMapPremiumBuildingWindow.cs");
            string code=File.ReadAllText(path);
            Assert.That(code,Does.Not.Contain(
                "HiveMapActivitiesBootstrap.CanvasScale("));
            Assert.That(code,Does.Not.Contain(
                "HiveViewProductUiPresenter.PremiumTextureForActivities("));
            Assert.That(code,Does.Contain("PremiumMaterial("));
            Assert.That(Resources.Load<Texture2D>(
                "PremiumBeeReference/background_hive"),Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>(
                "PremiumBeeIcons/nursery"),Is.Not.Null);
        }

        [Test]
        public void NurseryCohortsAreNotFalselyExposedBeforeServerActivation()
        {
            string path=Path.Combine(Application.dataPath,
                "BeeKingdom/Playground/HiveMapPremiumBuildingWindow.cs");
            string code=File.ReadAllText(path);
            int first=code.IndexOf("private void RenderNurseryCards(",
                StringComparison.Ordinal);
            int last=code.IndexOf("private RectTransform CardAt(",
                first,StringComparison.Ordinal);
            Assert.That(last,Is.GreaterThan(first));
            string section=code.Substring(first,last-first);
            Assert.That(section,Does.Contain("COUVÉES ROYALES"));
            Assert.That(section,Does.Contain("Prototype serveur désactivé"));
            Assert.That(section,Does.Not.Contain("cohortClient.StartAsync("));
        }

        [Test]
        public void LegacyQueueTimersRespectSharedNurseryModalGuard()
        {
            var p=typeof(HiveMapNurseryBootstrap).GetProperty(
                "OverlayOpenForExternalHost",
                BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
            Assert.That(p,Is.Not.Null);
            var set=p.GetSetMethod(true);
            Assert.That(set,Is.Not.Null);
            bool previous=(bool)p.GetValue(null,null);
            try
            {
                set.Invoke(null,new object[]{true});
                Assert.That(HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking(),
                    Is.True);
                string queue=File.ReadAllText(Path.Combine(Application.dataPath,
                    "BeeKingdom/Playground/HiveMapQueueSidebarBootstrap.cs"));
                Assert.That(queue,Does.Contain(
                    "return HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking();"));
            }
            finally{set.Invoke(null,new object[]{previous});}
        }
    }
}
