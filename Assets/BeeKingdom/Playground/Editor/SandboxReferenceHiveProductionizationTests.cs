using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxReferenceHiveProductionizationTests
    {
        public static void ValidateReferenceHiveProductionization()
        {
            var tests = new SandboxReferenceHiveProductionizationTests();
            tests.ReferenceHiveUsesFourteenOfficialPolygonHotspots();
            tests.ReferenceHiveSelectsExpectedZonesAtCentersAndBorders();
            tests.ReferenceHiveProductionizationGateKeepsBee641Blocked();
            Debug.Log("BEE-621 to BEE-640 reference hive productionization validation completed.");
        }

        [Test]
        public void ReferenceHiveUsesFourteenOfficialPolygonHotspots()
        {
            Assert.That(HiveViewProductUiPresenter.ReferenceHotspotCount, Is.EqualTo(14));

            string[] ids = HiveViewProductUiPresenter.GetReferenceHotspotIdsForProof();
            Assert.That(ids, Does.Contain("honey_storage"));
            Assert.That(ids, Does.Contain("alliance_future_hall"));
            Assert.That(ids, Does.Contain("archives_honeyfall"));

            for (int i = 0; i < ids.Length; i++)
            {
                Vector2[] polygon = HiveViewProductUiPresenter.GetReferenceHotspotPolygonForProof(ids[i]);
                Assert.That(polygon.Length, Is.GreaterThanOrEqualTo(6), ids[i] + " must use a real polygon, not a circle or point hitbox.");
            }
        }

        [Test]
        public void ReferenceHiveSelectsExpectedZonesAtCentersAndBorders()
        {
            Assert.That(HiveViewProductUiPresenter.TrySelectReferenceHotspotAtArtPointForProof(784f, 178f), Is.True);
            Assert.That(HiveViewProductUiPresenter.GetReferenceFocusedHotspotLabelForProof(), Is.EqualTo("Reserve miel"));

            Assert.That(HiveViewProductUiPresenter.TrySelectReferenceHotspotAtArtPointForProof(700f, 91f), Is.True);
            Assert.That(HiveViewProductUiPresenter.GetReferenceFocusedHotspotLabelForProof(), Is.EqualTo("Reserve miel"));

            Assert.That(HiveViewProductUiPresenter.TrySelectReferenceHotspotAtArtPointForProof(956f, 266f), Is.True);
            Assert.That(HiveViewProductUiPresenter.GetReferenceFocusedHotspotLabelForProof(), Is.EqualTo("Caserne"));

            Assert.That(HiveViewProductUiPresenter.TrySelectReferenceHotspotAtArtPointForProof(780f, 680f), Is.True);
            Assert.That(HiveViewProductUiPresenter.GetReferenceFocusedHotspotLabelForProof(), Is.EqualTo("Centre alliance"));
        }

        [Test]
        public void ReferenceHiveProductionizationGateKeepsBee641Blocked()
        {
            Assert.That(HiveViewProductUiPresenter.ReferenceClickableHotspotMap.VisualState, Does.Contain("14 polygon"));
            Assert.That(HiveViewProductUiPresenter.ReferenceMmoEntryGate.Verdict, Is.EqualTo(ReferenceHiveProductionizationVerdict.Pass));
            Assert.That(HiveViewProductUiPresenter.ReferenceMmoEntryGate.NonClaimRule, Does.Contain("BEE-641 remains blocked"));
            Assert.That(HiveViewProductUiPresenter.ReferenceNonClaimLanguageLedger.ForbiddenCopies, Does.Contain("chat live"));
        }
    }
}
