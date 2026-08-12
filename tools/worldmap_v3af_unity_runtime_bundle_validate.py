from __future__ import annotations

import hashlib
import json
from datetime import datetime
from pathlib import Path

from PIL import Image


ROOT = Path(r"C:\projets\beekingdomgame-master")
BUNDLE = ROOT / "Assets" / "BeeKingdom" / "Playground" / "Resources" / "WorldMapWave6Runtime" / "UIB_ImmenseContinuousMaster50x50_v3e_candidate"
OUT = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging" / "production_v3af_unity_runtime_candidate_validation"
EXPECTED_SHA = "978C79C66792040F3FDE79077BE8506041FD993E695599EDCD693F2FFB60CDE3"


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest().upper()


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    manifest_path = BUNDLE / "runtime_manifest.json"
    validation_path = BUNDLE / "runtime_validation.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    validation = json.loads(validation_path.read_text(encoding="utf-8"))

    grid = manifest["grid"]
    issues: list[str] = []
    if manifest.get("schema") != "bee-kingdom.world-map.wave6-unity-runtime-bundle.v1":
        issues.append("schema mismatch")
    if manifest.get("source", {}).get("master_sha256", "").upper() != EXPECTED_SHA:
        issues.append("source master sha mismatch")
    for key, expected in {
        "rows": 50,
        "columns": 50,
        "tile_size": 512,
        "runtime_tile_size": 516,
        "gutter": 2,
        "origin_chunk_x": 7,
        "origin_chunk_y": 7,
        "world_width": 25600,
        "world_height": 25600,
    }.items():
        if grid.get(key) != expected:
            issues.append(f"grid {key} expected {expected}, got {grid.get(key)}")
    if manifest.get("tile_count") != 2500 or len(manifest.get("tiles", [])) != 2500:
        issues.append("tile count mismatch")

    seen = set()
    bad_tiles = []
    sample_dimensions = {}
    for tile in manifest["tiles"]:
        expected_file = f"R{tile['row']:02d}C{tile['column']:02d}_g2.png"
        if tile.get("file") != expected_file or tile.get("resource_name") != expected_file[:-4]:
            bad_tiles.append({"tile": tile.get("id"), "reason": "name mismatch"})
            continue
        if (tile["row"], tile["column"]) in seen:
            bad_tiles.append({"tile": tile.get("id"), "reason": "duplicate coordinate"})
            continue
        seen.add((tile["row"], tile["column"]))
        path = BUNDLE / expected_file
        if not path.exists():
            bad_tiles.append({"tile": tile.get("id"), "reason": "missing file"})
            continue
        digest = sha256(path)
        if digest != tile.get("runtime_sha256", "").upper():
            bad_tiles.append({"tile": tile.get("id"), "reason": "sha mismatch"})
            continue
        with Image.open(path) as im:
            if im.size != (516, 516):
                bad_tiles.append({"tile": tile.get("id"), "reason": f"dimension {im.size}"})
            if tile["row"] in (0, 24, 49) and tile["column"] in (0, 24, 49):
                sample_dimensions[tile["id"]] = list(im.size)

    if len(seen) != 2500:
        issues.append(f"coordinate coverage {len(seen)}/2500")
    if bad_tiles:
        issues.append(f"bad tile entries {len(bad_tiles)}")

    validation_ok = (
        validation.get("status") == "PASS"
        and validation.get("tile_count") == 2500
        and validation.get("neighbor_validation", {}).get("pass") is True
        and validation.get("inner_pixel_mismatch_count") == 0
        and validation.get("neighbor_gutter_mismatch_count") == 0
    )
    if not validation_ok:
        issues.append("runtime_validation is not PASS")

    unity_lock = ROOT / "Temp" / "UnityLockfile"
    pass_offline = not issues
    receipt = {
        "artifact": "V3AF_UNITY_RUNTIME_CANDIDATE_VALIDATION",
        "created_at": datetime.now().isoformat(timespec="seconds"),
        "bundle": str(BUNDLE),
        "manifest": str(manifest_path),
        "runtime_validation": str(validation_path),
        "expected_master_sha256": EXPECTED_SHA,
        "manifest_master_sha256": manifest.get("source", {}).get("master_sha256"),
        "grid": grid,
        "tile_count": len(manifest.get("tiles", [])),
        "sample_dimensions": sample_dimensions,
        "issues": issues,
        "bad_tiles_sample": bad_tiles[:20],
        "unity_lockfile_present": unity_lock.exists(),
        "verdict": "OFFLINE_UNITY_RUNTIME_CANDIDATE_PASS_PLAYMODE_PENDING" if pass_offline else "FAIL",
        "gates": {
            "ACTIVE_WORK_RESUMED": "YES",
            "V3AF_MANIFEST_SCHEMA_PASS": "YES" if not any("schema" in i for i in issues) else "NO",
            "V3AF_MASTER_SHA_MATCH_PROVIDER": "YES" if manifest.get("source", {}).get("master_sha256", "").upper() == EXPECTED_SHA else "NO",
            "V3AF_TILE_COUNT_2500": "YES" if len(manifest.get("tiles", [])) == 2500 else "NO",
            "V3AF_TILE_FILES_2500_PRESENT": "YES" if len(seen) == 2500 and not bad_tiles else "NO",
            "V3AF_TILE_DIMENSIONS_516": "YES" if not bad_tiles else "NO",
            "V3AF_RUNTIME_VALIDATION_PASS": "YES" if validation_ok else "NO",
            "V3AF_OFFLINE_UNITY_RUNTIME_CANDIDATE_PASS": "YES" if pass_offline else "NO",
            "V3AF_PLAY_MODE_VERIFIED": "NO",
            "READY_FOR_QA_BUILDERC": "NO",
            "READY_FOR_UNITY_HANDOFF": "NO",
        },
    }
    receipt_path = OUT / "V3AF_UNITY_RUNTIME_CANDIDATE_VALIDATION_RECEIPT.json"
    receipt_path.write_text(json.dumps(receipt, indent=2), encoding="utf-8")

    checkpoint = OUT / "V3AF_UNITY_RUNTIME_CANDIDATE_VALIDATION_CHECKPOINT.md"
    checkpoint.write_text(
        "\n".join(
            [
                "# V3AF Unity Runtime Candidate Validation",
                "",
                f"Created: {receipt['created_at']}",
                "",
                f"Verdict: `{receipt['verdict']}`",
                "",
                "## Gates",
                *[f"- `{k}={v}`" for k, v in receipt["gates"].items()],
                "",
                "## Note",
                "This proves the V3E candidate bundle is structurally ready for Unity loading. It does not prove Play Mode because a Unity lockfile is currently present.",
            ]
        ),
        encoding="utf-8",
    )
    print(json.dumps({"receipt": str(receipt_path), "verdict": receipt["verdict"], "issues": issues}, indent=2))


if __name__ == "__main__":
    main()
