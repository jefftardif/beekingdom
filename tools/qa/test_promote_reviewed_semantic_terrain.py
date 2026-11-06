import hashlib
import json
from pathlib import Path
import sys
from tempfile import TemporaryDirectory
import unittest

from PIL import Image

sys.path.insert(0, str(Path(__file__).parent))
from promote_reviewed_semantic_terrain import (
    promote, review_template, semantic_content_digest, validate_proposal
)


def sample_proposal():
    return {
        "schema": "bk-wave6-semantic-candidate-v1",
        "reviewed": False,
        "safeForServerPlacement": False,
        "sourceManifestSha256": "a" * 64,
        "coordinateContract": "wave6:origin=7,7;grid=50,50;core=512;gutter=2;y=down",
        "tiles": [{
            "chunkX": 32, "chunkY": 32,
            "runtimePngSha256": "b" * 64,
            "previewFile": "preview.png",
            "sourceFile": "runtime.png",
            "reviewed": False,
            "candidateRle": [
                {"Length": 512 * 100, "Kind": "CandidateWater"},
                {"Length": 512 * 20, "Kind": "Unknown"},
                {"Length": 512 * 392, "Kind": "CandidateLand"}
            ]
        }]
    }


class IndependentTerrainReviewTests(unittest.TestCase):
    def setUp(self):
        self.temp = TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.preview = self.root / "preview.png"
        Image.new("RGB", (512, 512), (20, 150, 200)).save(self.preview)
        self.original = self.root / "runtime.png"
        Image.new("RGB", (516, 516), (31, 108, 151)).save(self.original)
        self.proposal = sample_proposal()
        self.proposal["tiles"][0]["runtimePngSha256"] = hashlib.sha256(
            self.original.read_bytes()).hexdigest()
        self.review = review_template(self.proposal)

    def tearDown(self):
        self.temp.cleanup()

    def approved_review(self):
        result = json.loads(json.dumps(self.review))
        result["allReviewed"] = True
        tile = result["tiles"][0]
        tile.update({
            "approved": True, "reviewer": "Independent QA operator",
            "reviewedAtUtc": "2026-11-06T12:00:00Z",
            "evidence": "file:qa-human-review-32-32.png",
            "previewSha256": hashlib.sha256(self.preview.read_bytes()).hexdigest(),
            "manualShorelineVerified": True,
            "singleWaterTypeVerified": True,
            "waterType": "River"
        })
        return result

    def test_template_is_all_denied(self):
        self.assertFalse(self.review["allReviewed"])
        self.assertFalse(self.review["tiles"][0]["approved"])
        with self.assertRaisesRegex(ValueError, "Independent review"):
            promote(self.proposal, self.review, self.root, "review-v1", self.root)

    def test_nonhuman_or_empty_evidence_fails(self):
        bad = self.approved_review()
        bad["tiles"][0]["evidence"] = ""
        with self.assertRaisesRegex(ValueError, "evidence missing"):
            promote(self.proposal, bad, self.root, "review-v1", self.root)

    def test_changed_png_fails(self):
        bad = self.approved_review()
        bad["tiles"][0]["runtimePngSha256"] = "0" * 64
        with self.assertRaisesRegex(ValueError, "Original tile changed"):
            promote(self.proposal, bad, self.root, "review-v1", self.root)

    def test_original_runtime_tile_mutation_is_rejected_at_approval(self):
        reviewed = self.approved_review()
        self.original.write_bytes(b"substituted source PNG")
        with self.assertRaisesRegex(ValueError, "Original tile changed"):
            promote(self.proposal, reviewed, self.root, "review-v1", self.root)

    def test_original_tile_directory_is_mandatory(self):
        reviewed = self.approved_review()
        with self.assertRaisesRegex(ValueError, "Original Wave6 PNG directory required"):
            promote(self.proposal, reviewed, self.root, "review-v1")
    def test_tampered_preview_fails(self):
        bad = self.approved_review()
        self.preview.write_bytes(b"tampered")
        with self.assertRaisesRegex(ValueError, "Preview changed"):
            promote(self.proposal, bad, self.root, "review-v1", self.root)

    def test_missing_shoreline_review_fails(self):
        bad = self.approved_review()
        bad["tiles"][0]["manualShorelineVerified"] = False
        with self.assertRaisesRegex(ValueError, "evidence missing"):
            promote(self.proposal, bad, self.root, "review-v1", self.root)

    def test_mixed_water_type_without_independent_confirmation_fails(self):
        bad = self.approved_review()
        bad["tiles"][0]["singleWaterTypeVerified"] = False
        with self.assertRaisesRegex(ValueError, "Single River/Lake"):
            promote(self.proposal, bad, self.root, "review-v1", self.root)

    def test_land_only_tile_does_not_require_fabricated_river_type(self):
        proposal = json.loads(json.dumps(self.proposal))
        proposal["tiles"][0]["candidateRle"] = [
            {"Length": 512 * 512, "Kind": "CandidateLand"}]
        review = review_template(proposal)
        review["allReviewed"] = True
        review["tiles"][0].update({
            "approved": True, "reviewer": "Independent QA operator",
            "reviewedAtUtc": "2026-11-06T12:00:00Z",
            "evidence": "file:qa-human-review-land-only.png",
            "previewSha256": hashlib.sha256(self.preview.read_bytes()).hexdigest(),
            "manualShorelineVerified": True,
            "waterType": ""
        })
        doc = promote(proposal, review, self.root, "review-land-v1", self.root)
        self.assertEqual(doc["Tiles"][0]["Runs"],
                         [{"Length":512*512,"Kind":"Land"}])

    def test_explicit_synthetic_review_can_produce_versioned_document(self):
        doc = promote(self.proposal, self.approved_review(), self.root, "review-v1", self.root)
        self.assertFalse(doc["DeploymentAuthorized"])
        self.assertEqual(doc["Tiles"][0]["Runs"], [
            {"Length": 512 * 100, "Kind": "River"},
            {"Length": 512 * 20, "Kind": "Unknown"},
            {"Length": 512 * 392, "Kind": "Land"}])
        self.assertEqual(doc["ContentSha256"], semantic_content_digest(doc))
        self.assertEqual(len(doc["ContentSha256"]), 64)

    def test_invalid_proposal_does_not_convert(self):
        bad = sample_proposal()
        bad["tiles"][0]["candidateRle"] = [
            {"Length": 100, "Kind": "CandidateWater"}]
        with self.assertRaisesRegex(ValueError, "incomplete"):
            validate_proposal(bad)

    def test_missing_independent_review_tile_is_rejected(self):
        bad = self.approved_review()
        bad["tiles"] = []
        with self.assertRaisesRegex(ValueError, "Every source tile"):
            promote(self.proposal, bad, self.root, "review-v1", self.root)


if __name__ == "__main__":
    unittest.main()