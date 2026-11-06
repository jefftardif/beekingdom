#!/usr/bin/env python3
"""Convert a verified image proposal into a semantic raster *only* with
independent, per-tile review evidence. No automatic approval mode exists.

This creates a versioned document for code review; it does NOT deploy it or
grant a server-side placement lease. Keep the current preview mask disabled.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import struct

KIND_MAP = {
    "CandidateLand": "Land",
    "Unknown": "Unknown",
}


def dotnet_string(value: str) -> bytes:
    data = value.encode("utf-8")
    size = len(data)
    prefix = bytearray()
    while size >= 128:
        prefix.append((size & 127) | 128)
        size >>= 7
    prefix.append(size)
    return bytes(prefix) + data


def semantic_content_digest(doc: dict) -> str:
    # Byte-for-byte mirror of SemanticTerrain.ComputeContentSha256 in Unity.
    encoded = bytearray()
    for key in ("Schema", "Revision", "ManifestSha256", "CoordinateContract"):
        encoded.extend(dotnet_string(doc.get(key) or ""))
    for tile in sorted(doc["Tiles"], key=lambda item: (item["ChunkY"], item["ChunkX"])):
        encoded.extend(struct.pack("<ii", tile["ChunkX"], tile["ChunkY"]))
        encoded.extend(dotnet_string(tile.get("PngSha256") or ""))
        encoded.extend(bytes([1 if tile["Reviewed"] else 0]))
        encoded.extend(dotnet_string(tile.get("ReviewEvidence") or ""))
        encoded.extend(struct.pack("<i", len(tile["Runs"])))
        for run in tile["Runs"]:
            encoded.extend(struct.pack("<i", run["Length"]))
            encoded.extend(dotnet_string(run["Kind"]))
    return hashlib.sha256(encoded).hexdigest().upper()


def validate_proposal(candidate: dict) -> None:
    if candidate.get("schema") != "bk-wave6-semantic-candidate-v1":
        raise ValueError("Unsupported candidate proposal schema")
    if candidate.get("reviewed") is not False or candidate.get("safeForServerPlacement") is not False:
        raise ValueError("This proposal was not generated in safe candidate-only mode")
    if not isinstance(candidate.get("tiles"), list) or not candidate["tiles"]:
        raise ValueError("No source tiles to review")
    coords = set()
    for tile in candidate["tiles"]:
        key = (tile["chunkX"], tile["chunkY"])
        if key in coords or tile.get("reviewed") is not False:
            raise ValueError("Duplicated or prematurely approved source tile")
        coords.add(key)
        if len(tile.get("runtimePngSha256", "")) != 64:
            raise ValueError("Missing source image SHA-256")
        rows = tile["candidateRle"]
        if sum(item["Length"] for item in rows) != 512 * 512:
            raise ValueError("Source candidate pixels incomplete")
        if any(item["Kind"] not in ("CandidateLand", "CandidateWater", "Unknown")
               or item["Length"] < 1 for item in rows):
            raise ValueError("Source proposal has unexpected pixel labels")


def review_template(proposal: dict) -> dict:
    validate_proposal(proposal)
    return {
        "schema": "bk-wave6-independent-terrain-review-v1",
        "sourceManifestSha256": proposal["sourceManifestSha256"],
        "allReviewed": False,
        "tiles": [{
            "chunkX": t["chunkX"], "chunkY": t["chunkY"],
            "runtimePngSha256": t["runtimePngSha256"],
            "previewSha256": "",
            "reviewer": "", "reviewedAtUtc": "",
            "evidence": "", "waterType": "",
            "manualShorelineVerified": False,
            "singleWaterTypeVerified": False,
            "approved": False
        } for t in proposal["tiles"]]
    }


def promote(proposal: dict, reviews: dict, preview_root: Path, revision: str,
            original_tile_root: Path | None = None) -> dict:
    validate_proposal(proposal)
    if (reviews.get("schema") != "bk-wave6-independent-terrain-review-v1"
        or reviews.get("sourceManifestSha256") != proposal["sourceManifestSha256"]
        or reviews.get("allReviewed") is not True):
        raise ValueError("Independent review is incomplete or mismatched")
    if not revision or len(revision) > 64 or revision.startswith("candidate"):
        raise ValueError("Invalid reviewed revision identifier")
    review_by_key = {}
    for review in reviews.get("tiles", []):
        key = (review.get("chunkX"), review.get("chunkY"))
        if key in review_by_key:
            raise ValueError("Duplicate tile review")
        review_by_key[key] = review
    # Review metadata is not enough if the original tile bytes changed.
    # Independently check the current PNG at approval time; the release
    # server must still pin the resulting semantic content SHA-256.
    if original_tile_root is None:
        raise ValueError("Original Wave6 PNG directory required for reviewed promotion")
    if len(review_by_key) != len(proposal["tiles"]):
        raise ValueError("Every source tile requires an independent review")

    result = {
        "Schema": "bk-semantic-terrain-v1",
        "Revision": revision,
        "ManifestSha256": proposal["sourceManifestSha256"],
        "CoordinateContract": proposal["coordinateContract"],
        "Tiles": []
    }
    for tile in proposal["tiles"]:
        key = (tile["chunkX"],tile["chunkY"])
        review = review_by_key.get(key)
        if review is None or review.get("approved") is not True:
            raise ValueError(f"Unapproved tile {key}")
        if review.get("runtimePngSha256") != tile["runtimePngSha256"]:
            raise ValueError(f"Original tile changed since review {key}")
        original_name = tile.get("sourceFile")
        if (not isinstance(original_name, str)
            or not original_name.lower().endswith(".png")
            or Path(original_name).name != original_name):
            raise ValueError(f"Original tile source path invalid {key}")
        original = original_tile_root / original_name
        if (not original.is_file()
            or hashlib.sha256(original.read_bytes()).hexdigest() != tile["runtimePngSha256"]):
            raise ValueError(f"Original tile changed or missing since proposal {key}")
        preview = preview_root / tile["previewFile"]
        if not preview.is_file() or hashlib.sha256(preview.read_bytes()).hexdigest() != review.get("previewSha256"):
            raise ValueError(f"Preview changed or lacked signed hash {key}")
        if not (isinstance(review.get("reviewer"), str) and
                review["reviewer"].strip() and
                isinstance(review.get("evidence"), str) and
                review["evidence"].strip() and
                review.get("manualShorelineVerified") is True and
                isinstance(review.get("reviewedAtUtc"), str) and
                review["reviewedAtUtc"].endswith("Z")):
            raise ValueError(f"Independent evidence missing for {key}")
        water_type = review.get("waterType")
        has_water = any(row["Kind"] == "CandidateWater"
                        for row in tile["candidateRle"])
        if has_water:
            # One tile can cross a river/lake confluence: do not promote an
            # unverified mixed-water tile with a single automatic label.
            if water_type not in ("River", "Lake") or review.get("singleWaterTypeVerified") is not True:
                raise ValueError(f"Single River/Lake type must be independently established for {key}")
        elif water_type not in ("", None):
            raise ValueError(f"Land-only tile cannot invent water type {key}")

        accepted = []
        for run in tile["candidateRle"]:
            kind = water_type if run["Kind"] == "CandidateWater" else KIND_MAP[run["Kind"]]
            if accepted and accepted[-1]["Kind"] == kind:
                accepted[-1]["Length"] += run["Length"]
            else:
                accepted.append({"Length": run["Length"], "Kind": kind})
        result["Tiles"].append({
            "ChunkX": tile["chunkX"], "ChunkY": tile["chunkY"],
            "PngSha256": tile["runtimePngSha256"],
            "Reviewed": True,
            "ReviewEvidence": (
                review["reviewer"] + " | " + review["reviewedAtUtc"] +
                " | " + review["evidence"] + " | preview-sha256:" +
                review["previewSha256"]
            ),
            "Runs": accepted
        })
    result["ContentSha256"] = semantic_content_digest(result)
    result["DeploymentAuthorized"] = False  # Explicit independent server gate still required.
    return result


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--proposal", type=Path, required=True)
    parser.add_argument("--previews", type=Path, required=True)
    parser.add_argument("--original-tiles", type=Path,
                        help="Required on promotion: independent Wave6 source PNG check")
    parser.add_argument("--review-file", type=Path)
    parser.add_argument("--revision")
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    proposal = json.loads(args.proposal.read_text(encoding="utf-8"))
    if args.review_file:
        reviews = json.loads(args.review_file.read_text(encoding="utf-8"))
        result = promote(proposal, reviews, args.previews, args.revision or "", args.original_tiles)
        print("REVIEWED_SEMANTIC_DOCUMENT=CREATED_NOT_DEPLOYED")
    else:
        result = review_template(proposal)
        print("REVIEW_TEMPLATE=ALL_APPROVALS_FALSE")
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(result, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8")
    print("OUTPUT=" + str(args.output))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())