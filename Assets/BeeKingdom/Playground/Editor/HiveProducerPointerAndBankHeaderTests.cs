using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class HiveProducerPointerAndBankHeaderTests
    {
        [TestCase(390f,844f,70f)]
        [TestCase(1080f,2400f,90f)]
        [TestCase(1280f,720f,88f)]
        [TestCase(1920f,1080f,88f)]
        public void GoldInfoBadgeIsTouchableAndBelowTheHiveHeader(
            float width,float height,float headerBottom)
        {
            Rect building=new Rect(width*.30f,10f,180f,155f);
            Rect badge=HiveMapProductionInfoBootstrap.ProducerInfoBadgeRectForProof(
                building,width,height,headerBottom);

                Assert.That(badge.width,Is.GreaterThanOrEqualTo(44f));
                Assert.That(badge.height,Is.GreaterThanOrEqualTo(44f));
                Assert.That(badge.xMin,Is.GreaterThanOrEqualTo(0f));
                Assert.That(badge.yMin,Is.GreaterThanOrEqualTo(headerBottom+7.9f));
                Assert.That(badge.xMax,Is.LessThanOrEqualTo(width));
                Assert.That(badge.yMax,Is.LessThanOrEqualTo(height));

            Vector2 guiPointer=HiveMapProductionInfoBootstrap.ProducerInfoGuiPointerForProof(
                new Vector2(badge.center.x,height-badge.center.y),height);
            Assert.That(badge.Contains(guiPointer),Is.True);
        }

        [TestCase(1920f,1080f,false)]
        [TestCase(800f,420f,true)]
        [TestCase(390f,844f,true)]
        public void BankUsesItsOwnHdHeaderWithoutCoveringUpgradeButton(
            float width,float height,bool compact)
        {
            const string expected="PremiumBeeReference/BuildingBanners/hive_bank";
            Assert.That(HiveViewProductUiPresenter.BankHeaderArtworkResourceForProof,
                Is.EqualTo(expected));
            Texture2D texture=Resources.Load<Texture2D>(expected);
            if(texture==null)
            {
                Sprite sprite=Resources.Load<Sprite>(expected);
                if(sprite!=null)texture=sprite.texture;
            }
            Assert.That(texture,Is.Not.Null,"No bank-specific HD header asset");
            Assert.That(texture.width,Is.GreaterThan(900));
            Assert.That(texture.height,Is.GreaterThan(500));
            Rect image=HiveViewProductUiPresenter.BankHeaderArtworkRectForProof(width,compact);
            Rect upgrade=HiveViewProductUiPresenter.PremiumBuildingUpgradeRectForProof(
                width,compact);
            Assert.That(image.width,Is.GreaterThanOrEqualTo(90f));
            Assert.That(image.height,Is.GreaterThanOrEqualTo(60f));
            Assert.That(image.Overlaps(upgrade),Is.False,
                "The bank banner must not cover Upgrade interactions");
        }

        [TestCase(1822f,863f,240f,96f)]
        [TestCase(1672f,941f,220f,84f)]
        [TestCase(1536f,1024f,120f,120f)]
        public void SharedHeaderBannerPreservesArtworkAspect(
            float imageWidth,float imageHeight,float screenWidth,float screenHeight)
        {
            Rect uv=HiveMapPremiumBuildingWindow.HeaderArtworkCropForProof(
                imageWidth,imageHeight,screenWidth,screenHeight);
            Assert.That(uv.xMin,Is.GreaterThanOrEqualTo(0f));
            Assert.That(uv.yMin,Is.GreaterThanOrEqualTo(0f));
            Assert.That(uv.xMax,Is.LessThanOrEqualTo(1.0001f));
            Assert.That(uv.yMax,Is.LessThanOrEqualTo(1.0001f));
            float actualAspect=(imageWidth*uv.width)/(imageHeight*uv.height);
            float targetAspect=screenWidth/screenHeight;
            Assert.That(actualAspect,Is.EqualTo(targetAspect).Within(.001f));
        }

        [Test]
        public void WorldInputOwnershipProtectsInfoBadgeAndLegacyManualCollection()
        {
            string baseDir=Path.Combine(Application.dataPath,"BeeKingdom","Playground");
            string gate=File.ReadAllText(Path.Combine(baseDir,
                "HiveMapOverlayInputGateBootstrap.cs"));
            StringAssert.Contains("ProducerInfoBadgeOwnsCurrentPointer()",gate);
            string producer=File.ReadAllText(Path.Combine(baseDir,
                "HiveMapProductionInfoBootstrap.Pointer.cs"));
            StringAssert.Contains("private void LateUpdate()",producer);
            StringAssert.Contains("IsPointerOverGameObject()",producer);
            StringAssert.Contains("QueryTriggerInteraction.Ignore",producer);
            StringAssert.DoesNotContain("CollectManualProductionForExternalHost(",producer);
            string collection=File.ReadAllText(Path.Combine(baseDir,
                "HiveMapProductionBootstrap.cs"));
            StringAssert.Contains("CollectManualProductionForExternalHost(",collection);
        }
    }
}
