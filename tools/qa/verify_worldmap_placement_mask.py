#!/usr/bin/env python3
"""Read-only QA gate for the *legacy preview* WorldMap placement mask.

This mask is not authoritative hydrology or server-owned occupation data.
Passing geometry checks would not certify land/water or grant placement rights.
No resource is moved, regenerated, suppressed or saved by this script.
"""
from __future__ import annotations

import argparse
import json
import math
from pathlib import Path


def valid_local_pair(item, prefix):
    try:
        x = float(item[prefix + "_x"])
        y = float(item[prefix + "_y"])
    except (KeyError, TypeError, ValueError):
        return False
    return math.isfinite(x) and math.isfinite(y) and 0.0 <= x <= 1.0 and 0.0 <= y <= 1.0


def same_anchor(item, left, right, epsilon=1e-5):
    if not valid_local_pair(item, left) or not valid_local_pair(item, right):
        return False
    return (
        abs(float(item[left + "_x"]) - float(item[right + "_x"])) < epsilon
        and abs(float(item[left + "_y"]) - float(item[right + "_y"])) < epsilon
    )


def inspect(data):
    entries = data.get("entries") if isinstance(data, dict) else None
    if not isinstance(entries, list) or not entries:
        raise ValueError("expected_nonempty_entries")

    seen = set()
    duplicate_coords = []
    invalid_water = []
    invalid_hive = []
    borrowed_but_safe = []
    hive_threat_same = []
    invalid_threat = []
    for i, entry in enumerate(entries):
        if not isinstance(entry, dict):
            raise ValueError("entry_must_be_object_at_index_" + str(i))
        try:
            key = (int(entry["chunk_x"]), int(entry["chunk_y"]))
        except (KeyError, TypeError, ValueError) as exc:
            raise ValueError("missing_chunk_coordinates_at_index_" + str(i)) from exc
        if key in seen:
            duplicate_coords.append(key)
        seen.add(key)
        if not valid_local_pair(entry, "water"):
            invalid_water.append(key)
        if not valid_local_pair(entry, "hive"):
            invalid_hive.append(key)
        if not valid_local_pair(entry, "threat"):
            invalid_threat.append(key)
        if entry.get("hive_safe") is True and int(entry.get("audit_borrowed_hive_anchor", 0) or 0) > 0:
            borrowed_but_safe.append(key)
        if same_anchor(entry, "hive", "threat"):
            hive_threat_same.append(key)

    counts = {
        "duplicate_chunk_count": len(duplicate_coords),
        "invalid_water_anchor_count": len(invalid_water),
        "invalid_hive_anchor_count": len(invalid_hive),
        "invalid_threat_anchor_count": len(invalid_threat),
        "borrowed_hive_still_marked_safe_count": len(borrowed_but_safe),
        "shared_hive_threat_anchor_count": len(hive_threat_same),
    }
    flags = [key for key, value in counts.items() if value]
    return {
        "schema": "beekingdom-worldmap-preview-mask-audit-v1",
        "preview_mask_only": True,
        "server_authoritative": False,
        "semantic_hydrology_certified": False,
        "entries": len(entries),
        "unique_chunks": len(seen),
        **counts,
        "examples": {
            "duplicate": duplicate_coords[:5],
            "invalid_water": invalid_water[:5],
            "invalid_hive": invalid_hive[:5],
            "invalid_threat": invalid_threat[:5],
            "borrowed_hive_marked_safe": borrowed_but_safe[:5],
            "shared_hive_threat": hive_threat_same[:5],
        },
        "consistency_pass": not flags,
        "safe_for_authoritative_placement": False,
        "limitations": [
            "Anchor coordinate bounds do not establish terrain eligibility.",
            "Water/land polygons have not been certified server-side.",
            "Shared anchors indicate potential collisions if both node types spawn.",
            "No server reservation or atomic movement checks are performed here.",
        ],
    }


def main(argv=None):
    parser = argparse.ArgumentParser()
    parser.add_argument("--mask", type=Path, required=True)
    parser.add_argument("--report", type=Path)
    parser.add_argument("--strict", action="store_true",
                        help="Return 2 if any structural inconsistency is detected")
    args = parser.parse_args(argv)
    result = inspect(json.loads(args.mask.read_text(encoding="utf-8-sig")))
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(
            json.dumps(result, indent=2, ensure_ascii=False) + "\n",
            encoding="utf-8")
    print("WORLD_MAP_PREVIEW_MASK_CONSISTENCY=" +
          ("PASS" if result["consistency_pass"] else "FAIL"))
    print("WORLD_MAP_AUTHORITATIVE_TERRAIN=NOT_CERTIFIED")
    print("ENTRIES=" + str(result["entries"]))
    for key in (
        "invalid_water_anchor_count", "invalid_hive_anchor_count",
        "borrowed_hive_still_marked_safe_count",
        "shared_hive_threat_anchor_count", "duplicate_chunk_count",
    ):
        print(key.upper() + "=" + str(result[key]))
    return 2 if args.strict and not result["consistency_pass"] else 0


if __name__ == "__main__":
    raise SystemExit(main())