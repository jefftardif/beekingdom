#!/usr/bin/env python3
"""Verifie le handoff Wave 3 et l'integrite des sources Unity en lecture seule."""

from __future__ import annotations

import hashlib
import json
from pathlib import Path


EXPECTED_MASTER = "d3cdc2dde9d56cac58be6833790b6fd8fc38ac157f72a01dcebd8117583a95b4"
EXPECTED_UV_MIN = 2 / 516
EXPECTED_UV_MAX = 514 / 516


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def require(condition: bool, message: str, checks: list[dict[str, str]]) -> None:
    checks.append({"check": message, "status": "PASS" if condition else "FAIL"})
    if not condition:
        raise ValueError(message)


def main() -> int:
    handoff_dir = Path(__file__).resolve().parent
    project_root = Path(__file__).resolve().parents[3]
    manifest_path = handoff_dir / "WorldMapWave3_RuntimeTileUnityHandoff.manifest.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    bundle = project_root / manifest["source_bundle"]["root"]
    checks: list[dict[str, str]] = []

    require(manifest["master"]["sha256"] == EXPECTED_MASTER, "master_sha256_exact", checks)
    require(len(manifest["tiles"]) == 25, "runtime_tile_count_25", checks)
    require(
        [tile["id"] for tile in manifest["tiles"]]
        == [f"R{row}C{column}" for row in range(5) for column in range(5)],
        "row_major_order_R0C0_to_R4C4",
        checks,
    )
    require(len({tile["future_unity_destination"] for tile in manifest["tiles"]}) == 25, "future_destinations_unique", checks)

    for tile in manifest["tiles"]:
        source_path = bundle / tile["source"]["relative_to_bundle"]
        require(source_path.is_file(), f"{tile['id']}_source_exists", checks)
        require(sha256_file(source_path) == tile["source"]["png_sha256"], f"{tile['id']}_sha256_exact", checks)
        require(tile["runtime_dimensions"] == {"height": 516, "width": 516}, f"{tile['id']}_dimensions_516", checks)
        uv = tile["uv_inner_normalized"]
        require(
            abs(uv["u_min"] - EXPECTED_UV_MIN) < 1e-15
            and abs(uv["v_min"] - EXPECTED_UV_MIN) < 1e-15
            and abs(uv["u_max"] - EXPECTED_UV_MAX) < 1e-15
            and abs(uv["v_max"] - EXPECTED_UV_MAX) < 1e-15,
            f"{tile['id']}_inner_uv_exact",
            checks,
        )

    for source in manifest["unity_read_only_baseline"]:
        path = project_root / source["path"]
        require(path.is_file(), f"unity_source_exists:{source['path']}", checks)
        require(sha256_file(path) == source["sha256"], f"unity_source_unchanged:{source['path']}", checks)

    result = {
        "schema": "bee-kingdom.world-map-wave3-handoff-validation.v1",
        "status": "PASS",
        "manifest_sha256": sha256_file(manifest_path),
        "checks_passed": len(checks),
        "checks_failed": 0,
        "checks": checks,
        "claims": {
            "handoff_validated": True,
            "unity_product_files_modified_by_builder_b": False,
            "unity_integration_done": False,
            "unity_runtime_validation_done": False,
        },
    }
    output = handoff_dir / "WorldMapWave3_HandoffValidation.json"
    output.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(f"status={result['status']}")
    print(f"checks_passed={result['checks_passed']}")
    print(f"output={output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
