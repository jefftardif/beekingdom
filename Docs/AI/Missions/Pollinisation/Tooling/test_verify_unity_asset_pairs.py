"""Isolated no-Unity tests for the read-only Unity asset/meta audit."""
import subprocess
import tempfile
from pathlib import Path
import unittest

from verify_unity_asset_pairs import inspect


class UnityAssetMetaPreflightTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="bk-assets-qa-")
        self.root = Path(self.temporary.name)
        subprocess.run(["git", "init", "-q", str(self.root)], check=True)
        self.assets = self.root / "Assets" / "Art"
        self.assets.mkdir(parents=True)
        self.png = self.assets / "flower.png"
        self.meta = self.assets / "flower.png.meta"
        self.png.write_bytes(b"test-image-payload")
        self.meta.write_text("fileFormatVersion: 2\nguid: 0123456789abcdef0123456789abcdef\n")
        subprocess.run(
            ["git", "-C", str(self.root), "add", "-f", "Assets/Art/flower.png.meta"],
            check=True,
        )

    def tearDown(self):
        self.temporary.cleanup()

    def test_pair_present(self):
        report = inspect(self.root, None)
        self.assertEqual(report["tracked_meta_count"], 1)
        self.assertEqual(report["pairs_present"], 1)
        self.assertEqual(report["issues"], 0)

    def test_orphan_metadata_is_flagged_without_rewriting_anything(self):
        self.png.unlink()
        report = inspect(self.root, None)
        self.assertEqual(report["missing_source_with_meta"],
                         ["Assets/Art/flower.png.meta"])
        self.assertEqual(report["issues"], 1)
        self.assertTrue(self.meta.is_file())

    def test_missing_meta_and_asset_can_be_classified_as_recoverable(self):
        ref = self.root / "reference"
        reference_asset = ref / "Assets" / "Art" / "flower.png"
        reference_asset.parent.mkdir(parents=True)
        reference_asset.write_bytes(self.png.read_bytes())
        self.png.unlink()
        self.meta.unlink()
        report = inspect(self.root, ref)
        self.assertEqual(report["missing_both_recoverable_from_reference"],
                         ["Assets/Art/flower.png.meta"])
        self.assertEqual(report["issues"], 1)
        self.assertFalse(self.png.exists())
        self.assertFalse(self.meta.exists())


if __name__ == "__main__":
    unittest.main()
