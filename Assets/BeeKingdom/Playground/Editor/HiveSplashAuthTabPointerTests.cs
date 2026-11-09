using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class HiveSplashAuthTabPointerTests
    {
        [TearDown]
        public void RestoreSplash()
        {
            HiveViewProductUiPresenter.SetSplashAuthGateForProof("splash");
        }

        [TestCase(390f,844f,true)]
        [TestCase(800f,420f,false)]
        [TestCase(1280f,720f,false)]
        [TestCase(1920f,1080f,false)]
        public void PointerDownOpensEachVisibleTabWithoutMouseUp(
            float width,float height,bool portrait)
        {
            var tabs=HiveViewProductUiPresenter.SplashAuthTabRectsForProof(
                portrait,width,height);
            Assert.That(tabs.Length,Is.EqualTo(3));
            for(int i=0;i<tabs.Length;i++)
            {
                HiveViewProductUiPresenter.SetSplashAuthGateForProof("splash");
                Assert.That(tabs[i].width,Is.GreaterThan(32f));
                Assert.That(tabs[i].height,Is.GreaterThan(28f));
                Assert.That(tabs[i].xMin,Is.GreaterThanOrEqualTo(0f));
                Assert.That(tabs[i].xMax,Is.LessThanOrEqualTo(width));
                Assert.That(tabs[i].yMin,Is.GreaterThanOrEqualTo(0f));
                Assert.That(tabs[i].yMax,Is.LessThanOrEqualTo(height));
                bool handled=HiveViewProductUiPresenter.TryNavigateSplashTabForProof(
                    portrait,width,height,tabs[i].center,true);
                Assert.That(handled,Is.True,"Tab "+i+" click not handled");
                string expected=i==0?"Splash":i==1?"Login":"CreateAccount";
                Assert.That(HiveViewProductUiPresenter.SplashAuthTabStateForProof,
                    Is.EqualTo(expected));
            }
        }

        [Test]
        public void PointerOutsideTabsAndPointerNotPressedCannotNavigate()
        {
            HiveViewProductUiPresenter.SetSplashAuthGateForProof("splash");
            var tabs=HiveViewProductUiPresenter.SplashAuthTabRectsForProof(
                false,1280f,720f);
            Assert.That(HiveViewProductUiPresenter.TryNavigateSplashTabForProof(
                false,1280f,720f,tabs[1].center,false),Is.False);
            Assert.That(HiveViewProductUiPresenter.TryNavigateSplashTabForProof(
                false,1280f,720f,new Vector2(2f,2f),true),Is.False);
            Assert.That(HiveViewProductUiPresenter.SplashAuthTabStateForProof,
                Is.EqualTo("Splash"));
        }

        [Test]
        public void AccountTabCanReturnHomeWithoutStartingLocalDemo()
        {
            HiveViewProductUiPresenter.SetSplashAuthGateForProof("splash");
            var tabs=HiveViewProductUiPresenter.SplashAuthTabRectsForProof(
                true,390f,844f);
            Assert.That(HiveViewProductUiPresenter.TryNavigateSplashTabForProof(
                true,390f,844f,tabs[1].center,true),Is.True);
            Assert.That(HiveViewProductUiPresenter.SplashAuthTabStateForProof,
                Is.EqualTo("Login"));
            Assert.That(HiveViewProductUiPresenter.HasEnteredHiveForExternalHost,Is.False);
            Assert.That(HiveViewProductUiPresenter.TryNavigateSplashTabForProof(
                true,390f,844f,tabs[0].center,true),Is.True);
            Assert.That(HiveViewProductUiPresenter.SplashAuthTabStateForProof,
                Is.EqualTo("Splash"));
        }

        [TestCase(390f,844f,true)]
        [TestCase(1280f,720f,false)]
        public void PollingFallbackWorksWithoutIMGUIEvents(
            float width, float height, bool portrait)
        {
            Rect[] tabs=HiveViewProductUiPresenter.SplashAuthTabRectsForProof(
                portrait,width,height);
            for(int i=0;i<tabs.Length;i++)
            {
                HiveViewProductUiPresenter.SetSplashAuthGateForProof("splash");
                Vector2 gamePoint=HiveMapSplashBootstrap.ScreenPointerToGuiPointForProof(
                    tabs[i].center,height);
                Assert.That(HiveMapSplashBootstrap.TryNavigateFromScreenPointerForProof(
                    gamePoint,width,height,true),Is.True);
                string expected=i==0?"Splash":i==1?"Login":"CreateAccount";
                Assert.That(HiveViewProductUiPresenter.SplashAuthTabStateForProof,
                    Is.EqualTo(expected));
                Assert.That(HiveMapSplashBootstrap.TryNavigateFromScreenPointerForProof(
                    gamePoint,width,height,false),Is.False);
            }
        }
        [Test]
        public void AlreadyEnteredHiveCannotBeMovedBackByStaleSplashClick()
        {
            HiveViewProductUiPresenter.SetSplashAuthGateForProof("hive");
            var tabs=HiveViewProductUiPresenter.SplashAuthTabRectsForProof(
                true,390f,844f);
            Assert.That(HiveViewProductUiPresenter.TryNavigateSplashTabForProof(
                true,390f,844f,tabs[1].center,true),Is.False);
            Assert.That(HiveViewProductUiPresenter.HasEnteredHiveForExternalHost,Is.True);
        }
    }
}
