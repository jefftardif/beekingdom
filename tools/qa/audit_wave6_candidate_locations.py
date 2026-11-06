#!/usr/bin/env python3
"""Read-only comparison of loaded legacy coordinates vs unreviewed Wave6 colors.

This is only diagnostic evidence. CandidateLand/CandidateWater are NOT approved
terrain, do not relocate an icon, and do not grant any game/SQL reservation.
"""
from __future__ import annotations

import argparse
from bisect import bisect_right
from collections import Counter
import json
import math
from pathlib import Path


def semantic_lookup(tiles: dict, x: float, y: float) -> str:
    if not math.isfinite(x) or not math.isfinite(y):
        return "InvalidCoordinate"
    cx, cy = math.floor(x / 512), math.floor(y / 512)
    tile = tiles.get((cx, cy))
    if tile is None:
        return "NotReviewedPilotArea"
    px, py = math.floor(x - cx * 512), math.floor(y - cy * 512)
    if not (0 <= px < 512 and 0 <= py < 512):
        return "InvalidCoordinate"
    position = py * 512 + px
    ends, kinds = tile
    return kinds[bisect_right(ends, position)]


def index_tiles(proposal: dict) -> dict:
    if proposal.get("reviewed") is not False or proposal.get("safeForServerPlacement") is not False:
        raise ValueError("Source is not a candidate-only proposal")
    tiles = {}
    for entry in proposal["tiles"]:
        key = (entry["chunkX"], entry["chunkY"])
        ends = []
        kinds = []
        end = 0
        for run in entry["candidateRle"]:
            end += run["Length"]
            ends.append(end)
            kinds.append(run["Kind"])
        if end != 512*512 or key in tiles:
            raise ValueError("Candidate raster incomplete or duplicate tile")
        tiles[key] = (ends, kinds)
    return tiles


def audit(snapshot: dict, proposal: dict) -> dict:
    if snapshot.get("serverAuthorityCertified") is not False or snapshot.get("completeWorldInventory") is not False:
        raise ValueError("Expected a scoped, read-only Unity snapshot")
    tiles = index_tiles(proposal)
    nodes = [n for n in snapshot["nodes"] if n["source"].startswith("visible:")]
    counts = Counter()
    mismatch = []
    priorities = []
    for node in nodes:
        label = semantic_lookup(tiles, float(node["x"]), float(node["y"]))
        counts[label] += 1
        subtype = (node.get("subtype") or "").lower()
        kind = node.get("kind")
        should_be_water = kind == "resource" and subtype == "water"
        should_be_land = kind in ("hive", "bestiary") or (
            kind == "resource" and subtype != "water")
        concern = ((should_be_water and label == "CandidateLand") or
                   (should_be_land and label == "CandidateWater"))
        summary = {
            "kind": kind, "id": node.get("id"),
            "x": node["x"], "y": node["y"],
            "subtype": node.get("subtype"),
            "candidateSurface": label,
            "tentativeConflict": concern,
        }
        if concern:
            mismatch.append(summary)
        if node.get("id") in (
            "hive_player_test", "spider_next_to_hive",
            "beast_t3_core_demo", "res_water_core",
        ):
            priorities.append(summary)
    return {
        "schema": "bk-candidate-vs-live-readonly-v1",
        "candidateOnly": True,
        "serverAuthoritative": False,
        "completeWorldInventory": False,
        "worldId": snapshot.get("worldId"),
        "snapshotUtc": snapshot.get("utc"),
        "sourceManifestSha256": proposal.get("sourceManifestSha256"),
        "loadedVisibleCount": len(nodes),
        "pilotTileCount": len(tiles),
        "candidateSurfaceCounts": dict(counts),
        "tentativeConflictCount": len(mismatch),
        "coreObjects": priorities,
        "tentativeConflictExamples": mismatch[:25],
        "limitations": [
            "Anchor-only color candidates, NOT full sprite/caption footprint.",
            "Candidate colors have NOT been independently reviewed.",
            "Water/land interpretation is unsuitable for server grants.",
            "Only already-loaded visible nodes and chosen pilot tiles are analyzed.",
        ],
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--snapshot", type=Path, required=True)
    parser.add_argument("--proposal", type=Path, required=True)
    parser.add_argument("--report", type=Path, required=True)
    args = parser.parse_args()
    result = audit(json.loads(args.snapshot.read_text(encoding="utf-8")),
                   json.loads(args.proposal.read_text(encoding="utf-8")))
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(result, ensure_ascii=False, indent=2)+"\n",
                           encoding="utf-8")
    print("CANDIDATE_CLASSIFICATION_ONLY=TRUE")
    print("VISIBLE_NODES="+str(result["loadedVisibleCount"]))
    print("TENTATIVE_TERRAIN_CONFLICTS="+str(result["tentativeConflictCount"]))
    for item in result["coreObjects"]:
        print("CORE="+item["kind"]+":"+item["id"]+" "+item["candidateSurface"])
    return 0


if __name__ == "__main__":
    raise SystemExit(main())