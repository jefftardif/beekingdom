import json
from pathlib import Path
import sys
import unittest

from PIL import Image

sys.path.insert(0, str(Path(__file__).parent))
from propose_wave6_semantic_terrain import (
    CORE_SIZE, LAND, UNKNOWN, WATER, manifest_tiles, propose, rle, rough_class
)


class Wave6SemanticProposalTests(unittest.TestCase):
    def test_water_and_land_both_detected_with_unknown_shore(self):
        img = Image.new("RGB", (CORE_SIZE, CORE_SIZE), (144, 178, 91))
        for y in range(CORE_SIZE):
            for x in range(200):
                img.putpixel((x, y), (55, 105, 112))
        raster = propose(img, 5)
        idx = lambda x, y: y * CORE_SIZE + x
        self.assertEqual(raster[idx(80, 256)], WATER)
        self.assertEqual(raster[idx(350, 256)], LAND)
        self.assertEqual(raster[idx(200, 256)], UNKNOWN)
        self.assertEqual(raster[idx(198, 256)], UNKNOWN)
        self.assertEqual(raster[idx(204, 256)], UNKNOWN)
        self.assertEqual(sum(r["Length"] for r in rle(raster)), 512 * 512)

    def test_ambiguous_colors_remain_unapproved(self):
        self.assertEqual(rough_class((105, 110, 108)), UNKNOWN)
        self.assertEqual(rough_class((240, 240, 240)), UNKNOWN)
        self.assertEqual(rough_class((55, 105, 112)), WATER)
        self.assertEqual(rough_class((144, 178, 91)), LAND)

    def test_wrong_dimensions_and_invalid_margin_rejected(self):
        with self.assertRaises(ValueError):
            propose(Image.new("RGB", (400, 400)))
        with self.assertRaises(ValueError):
            propose(Image.new("RGB", (512, 512)), 25)

    def test_rle_is_deterministic_and_complete(self):
        image = Image.new("RGB", (512, 512), (144, 178, 91))
        a = rle(propose(image, 4))
        b = rle(propose(image, 4))
        self.assertEqual(a, b)
        self.assertEqual(sum(run["Length"] for run in a), 512 * 512)

    def test_incompatible_grid_rejected_before_pixel_read(self):
        data = {"grid": {"rows": 25}}
        with self.assertRaisesRegex(ValueError, "Unsupported"):
            manifest_tiles(data)

    def test_real_candidate_output_is_never_automatically_reviewed(self):
        path=Path(r"C:\BeeKingdom\qa\SemanticTerrain-Prototype-20261106\wave6-semantic-candidates.json")
        if not path.exists():
            self.skipTest("Prototype not generated")
        doc=json.loads(path.read_text(encoding="utf-8"))
        self.assertFalse(doc["reviewed"])
        self.assertFalse(doc["safeForServerPlacement"])
        self.assertFalse(doc["safeForClientPlacement"])
        self.assertEqual(doc["selectedTiles"],9)
        for tile in doc["tiles"]:
            self.assertFalse(tile["reviewed"])
            self.assertEqual(sum(entry["Length"] for entry in tile["candidateRle"]),512*512)

    def test_real_stub_denies_every_tile(self):
        path=Path(r"C:\BeeKingdom\qa\SemanticTerrain-Prototype-20261106\review-stub-ALL-UNKNOWN.json")
        if not path.exists():
            self.skipTest("Prototype not generated")
        doc=json.loads(path.read_text(encoding="utf-8"))
        self.assertEqual(doc["Schema"],"bk-semantic-terrain-v1")
        self.assertEqual(len(doc["Tiles"]),9)
        for tile in doc["Tiles"]:
            self.assertFalse(tile["Reviewed"])
            self.assertEqual(tile["Runs"], [{"Length":512*512,"Kind":"Unknown"}])


if __name__ == "__main__":
    unittest.main()