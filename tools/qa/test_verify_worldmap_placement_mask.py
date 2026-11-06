"""Unit tests for the read-only preview placement-mask consistency gate."""
import copy
import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
from verify_worldmap_placement_mask import inspect, valid_local_pair


def entry():
    return {
        "chunk_x": 32, "chunk_y": 32,
        "hive_safe": True, "audit_borrowed_hive_anchor": 0,
        "hive_x": 0.2, "hive_y": 0.2,
        "water_x": 0.8, "water_y": 0.7,
        "threat_x": 0.7, "threat_y": 0.3,
    }


class PreviewMaskGateTests(unittest.TestCase):
    def test_clean_geometry_still_is_not_server_authority(self):
        report = inspect({"entries": [entry()]})
        self.assertTrue(report["consistency_pass"])
        self.assertFalse(report["semantic_hydrology_certified"])
        self.assertFalse(report["safe_for_authoritative_placement"])

    def test_negative_water_sentinel_is_rejected(self):
        item = entry()
        item["water_x"] = -1.5
        report = inspect({"entries": [item]})
        self.assertEqual(report["invalid_water_anchor_count"], 1)
        self.assertFalse(report["consistency_pass"])

    def test_borrowed_hive_anchor_is_not_authorized_by_safe_flag(self):
        item = entry()
        item["audit_borrowed_hive_anchor"] = 1
        report = inspect({"entries": [item]})
        self.assertEqual(report["borrowed_hive_still_marked_safe_count"], 1)
        self.assertFalse(report["consistency_pass"])

    def test_identical_hive_and_threat_anchor_is_flagged(self):
        item = entry()
        item["threat_x"] = item["hive_x"]
        item["threat_y"] = item["hive_y"]
        report = inspect({"entries": [item]})
        self.assertEqual(report["shared_hive_threat_anchor_count"], 1)

    def test_duplicates_do_not_claim_full_world_coverage(self):
        item = entry()
        report = inspect({"entries": [item, copy.deepcopy(item)]})
        self.assertEqual(report["duplicate_chunk_count"], 1)
        self.assertEqual(report["unique_chunks"], 1)
        self.assertFalse(report["consistency_pass"])

    def test_nan_and_missing_values_do_not_pass_as_coordinates(self):
        item = entry()
        item["water_y"] = float("nan")
        self.assertFalse(valid_local_pair(item, "water"))
        self.assertFalse(valid_local_pair({}, "water"))

    def test_missing_entries_rejected(self):
        with self.assertRaisesRegex(ValueError, "expected_nonempty_entries"):
            inspect({"entries": []})


if __name__ == "__main__":
    unittest.main()