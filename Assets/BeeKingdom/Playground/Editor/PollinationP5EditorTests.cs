using System;
using BeeKingdom.Networking;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    /// <summary>
    /// Execute after normal Unity package resolution is restored. Does not
    /// write any real account, hive, world or alliance data.
    /// </summary>
    public sealed class PollinationP5EditorTests
    {
        [Test]
        public void NewRuntimeAssetKeepsPollinationDisabledWithoutExplicitOptIn()
        {
            var configuration =
                ScriptableObject.CreateInstance<MobileAccountSessionRuntimeConfiguration>();
            try
            {
                Assert.That(configuration.PollinationFeatureEnabled, Is.False);
                Assert.That(configuration.OfficialGameplayEnabled, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(configuration); }
        }

        [Test]
        public void BilingualSharedSiteAcceptsRealServerOnlyValues()
        {
            var now = new DateTimeOffset(2026,10,21,19,0,0,TimeSpan.Zero);
            var site = new RemoteFloralSite
            {
                ContractVersion = PollinationClient.SiteVersion,
                WorldId = "world-qa", RegionId = "region-qa",
                SiteId = "clover-qa-01", FamilyId = "clover",
                NameFr = "Trèfle blanc", NameEn = "White clover",
                CatalogVersion = "alpha.qa.v1",
                Revision = 4, ServerTimeUtc = now,
                PollenRemaining = 30, PollenCapacity = 50,
                FertilityBp = 7200, BiodiversityBp = 6400,
                Phase = "blooming", PublicAccess = true,
                TotalPollinationPoints = 20, DistinctContributors = 2
            };

            PollinationClient.ValidateSite(site);
            Assert.That(site.LocalizedName("fr-CA"), Is.EqualTo("Trèfle blanc"));
            Assert.That(site.LocalizedName("en-US"), Is.EqualTo("White clover"));

            site.PollenRemaining = 60;
            Assert.Throws<PollinationClientException>(
                () => PollinationClient.ValidateSite(site));
        }

        [Test]
        public void EmptyCivilScreenHasNoImplicitExpeditionOrResourceAward()
        {
            var model = new PollinationScreenModel();
            Assert.That(model.State, Is.EqualTo(PollinationScreenState.NotConfigured));
            Assert.That(model.HasActiveExpedition, Is.False);
            Assert.That(model.CanLaunch, Is.False);
            Assert.That(model.CanCreateProject, Is.False);
            Assert.That(model.Sites, Is.Empty);
        }

        [Test]
        public void MapInputOverlayRemainsClosedUntilExplicitUserAction()
        {
            Assert.That(PollinationWorldMapOverlay.IsModalOpen, Is.False);
        }
    }
}
