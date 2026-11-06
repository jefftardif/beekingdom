"""Read-only candidate-vs-visible snapshot proof tests."""
import sys
from pathlib import Path
import unittest

sys.path.insert(0,str(Path(__file__).parent))
from audit_wave6_candidate_locations import audit, index_tiles, semantic_lookup


def proposal():
    return {
        "reviewed": False,
        "safeForServerPlacement": False,
        "sourceManifestSha256": "a"*64,
        "tiles": [{
            "chunkX":32, "chunkY":32,
            "candidateRle": [
                {"Length": 512*256, "Kind": "CandidateWater"},
                {"Length": 512*256, "Kind": "CandidateLand"}
            ]
        }]
    }


def node(kind, name, subtype, x, y, source="visible:resources"):
    return {
        "source":source,"kind":kind,"id":name,
        "subtype":subtype,"x":x,"y":y
    }


def snapshot(nodes):
    return {
        "serverAuthorityCertified":False,
        "completeWorldInventory":False,
        "worldId":"BK-DEMO-WORLD-WAVE6-LOCAL",
        "utc":"2026-11-06T06:10:37Z",
        "nodes":nodes
    }


class CandidateLocationAuditTests(unittest.TestCase):
    def test_water_raster_bounds(self):
        tiles=index_tiles(proposal())
        self.assertEqual(semantic_lookup(tiles,16384+10,16384+20),"CandidateWater")
        self.assertEqual(semantic_lookup(tiles,16384+11,16384+300),"CandidateLand")
        self.assertEqual(semantic_lookup(tiles,16384+11,16384+256),"CandidateLand")
        self.assertEqual(semantic_lookup(tiles,999,999),"NotReviewedPilotArea")

    def test_illegal_classification_detected_but_never_authorized(self):
        data=snapshot([
            node("hive","hive_player_test","Beginning",16400,16400),
            node("resource","water1","Water",16400,16720),
            node("resource","water2","Water",16400,16400),
            node("hive","land1","Beginning",16400,16720)
        ])
        output=audit(data,proposal())
        self.assertEqual(output["tentativeConflictCount"],2)
        self.assertFalse(output["serverAuthoritative"])
        self.assertTrue(output["candidateOnly"])

    def test_cached_nodes_are_not_counted_as_separate_visible(self):
        data=snapshot([
            node("hive","H1","Beginning",16420,16420,"visible:hives"),
            node("hive","H1","Beginning",16420,16420,"cache:hives")
        ])
        report=audit(data,proposal())
        self.assertEqual(report["loadedVisibleCount"],1)

    def test_untrusted_snapshot_cannot_claim_authority(self):
        data=snapshot([])
        data["serverAuthorityCertified"]=True
        with self.assertRaisesRegex(ValueError,"read-only"):
            audit(data,proposal())

    def test_incomplete_rle_rejected(self):
        bad=proposal()
        bad["tiles"][0]["candidateRle"] = [{"Length":100,"Kind":"CandidateLand"}]
        with self.assertRaisesRegex(ValueError, "incomplete"):
            index_tiles(bad)


if __name__=="__main__":
    unittest.main()