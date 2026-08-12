from __future__ import annotations

import json
import argparse
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / "Assets" / "BeeKingdom" / "Playground" / "Resources" / "WorldMapWave6Runtime" / "UIB_ImmenseContinuousMaster50x50_route_lock_coherent_proof"
REPORT_DIR = ROOT / "Docs" / "BuilderA" / "WorldMapWave6_50x50_RouteLockCoherentProofPreview"
JSON_REPORT = REPORT_DIR / "WorldMapWave6_RouteLockCoherentProof_SeamVerifierReceipt.json"
MD_REPORT = REPORT_DIR / "WorldMapWave6_RouteLockCoherentProof_SeamVerifierReceipt.md"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--package", type=Path, default=PACKAGE)
    parser.add_argument("--report-dir", type=Path, default=REPORT_DIR)
    parser.add_argument("--report-stem", default="WorldMapWave6_RouteLockCoherentProof_SeamVerifierReceipt")
    return parser.parse_args()


def load_manifest(package: Path) -> dict:
    with (package / "runtime_manifest.json").open("r", encoding="utf-8") as handle:
        return json.load(handle)


def crop_bytes(image: Image.Image, box: tuple[int, int, int, int]) -> bytes:
    return image.crop(box).tobytes()


def main() -> int:
    args = parse_args()
    package = args.package.resolve()
    report_dir = args.report_dir.resolve()
    json_report = report_dir / f"{args.report_stem}.json"
    md_report = report_dir / f"{args.report_stem}.md"
    manifest = load_manifest(package)
    grid = manifest["grid"]
    rows = int(grid["rows"])
    columns = int(grid["columns"])
    tile_size = int(grid["tile_size"])
    runtime_tile_size = int(grid["runtime_tile_size"])
    gutter = int(grid["gutter"])

    failures: list[dict] = []
    dimensions_ok = True
    tiles: dict[tuple[int, int], Image.Image] = {}

    for row in range(rows):
        for column in range(columns):
            path = package / f"R{row:02d}C{column:02d}_g{gutter}.png"
            if not path.exists():
                failures.append({"type": "missing_tile", "row": row, "column": column, "path": str(path)})
                dimensions_ok = False
                continue
            image = Image.open(path).convert("RGBA")
            if image.size != (runtime_tile_size, runtime_tile_size):
                failures.append(
                    {
                        "type": "bad_dimensions",
                        "row": row,
                        "column": column,
                        "actual": list(image.size),
                        "expected": [runtime_tile_size, runtime_tile_size],
                    }
                )
                dimensions_ok = False
            tiles[(row, column)] = image

    horizontal_checks = 0
    vertical_checks = 0

    for row in range(rows):
        for column in range(columns - 1):
            left = tiles.get((row, column))
            right = tiles.get((row, column + 1))
            if left is None or right is None:
                continue
            horizontal_checks += 2
            if crop_bytes(left, (runtime_tile_size - gutter, gutter, runtime_tile_size, gutter + tile_size)) != crop_bytes(
                right, (gutter, gutter, gutter + gutter, gutter + tile_size)
            ):
                failures.append({"type": "right_gutter_mismatch", "row": row, "column": column})
            if crop_bytes(right, (0, gutter, gutter, gutter + tile_size)) != crop_bytes(
                left, (tile_size, gutter, tile_size + gutter, gutter + tile_size)
            ):
                failures.append({"type": "left_gutter_mismatch", "row": row, "column": column + 1})

    for row in range(rows - 1):
        for column in range(columns):
            top = tiles.get((row, column))
            bottom = tiles.get((row + 1, column))
            if top is None or bottom is None:
                continue
            vertical_checks += 2
            if crop_bytes(top, (gutter, runtime_tile_size - gutter, gutter + tile_size, runtime_tile_size)) != crop_bytes(
                bottom, (gutter, gutter, gutter + tile_size, gutter + gutter)
            ):
                failures.append({"type": "bottom_gutter_mismatch", "row": row, "column": column})
            if crop_bytes(bottom, (gutter, 0, gutter + tile_size, gutter)) != crop_bytes(
                top, (gutter, tile_size, gutter + tile_size, tile_size + gutter)
            ):
                failures.append({"type": "top_gutter_mismatch", "row": row + 1, "column": column})

    receipt = {
        "schema": "bee-kingdom.world-map.wave6-route-lock-seam-verifier.v1",
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "package": str(package),
        "manifest": str(package / "runtime_manifest.json"),
        "source_master_sha256": manifest.get("source", {}).get("master_sha256"),
        "source_proof_path": manifest.get("source", {}).get("source_proof_path"),
        "grid": grid,
        "expected_tile_count": rows * columns,
        "actual_tile_count": len(tiles),
        "dimensions_ok": dimensions_ok,
        "horizontal_gutter_checks": horizontal_checks,
        "vertical_gutter_checks": vertical_checks,
        "failure_count": len(failures),
        "status": "PASS" if not failures and dimensions_ok and len(tiles) == rows * columns else "FAIL",
        "failures_sample": failures[:50],
        "gate_decision": {
            "READY_FOR_FULL_50X50_TILE_BUILD": "NO",
            "READY_FOR_QA_BUILDERC": "NO",
            "READY_FOR_UNITY_HANDOFF": "NO",
            "READY_FOR_CANONICAL_SWAP": "NO",
            "PREMIUM_50X50_VALIDATED": "NO",
        },
    }

    report_dir.mkdir(parents=True, exist_ok=True)
    json_report.write_text(json.dumps(receipt, indent=2), encoding="utf-8")
    md_report.write_text(
        "\n".join(
            [
                "# WorldMapWave6 Route-Lock Coherent Proof Seam Verifier",
                "",
                f"STATUS={receipt['status']}",
                f"CREATED_UTC={receipt['created_utc']}",
                f"PACKAGE={receipt['package']}",
                f"SOURCE_MASTER_SHA256={receipt['source_master_sha256']}",
                f"TILES={receipt['actual_tile_count']}/{receipt['expected_tile_count']}",
                f"DIMENSIONS_OK={receipt['dimensions_ok']}",
                f"HORIZONTAL_GUTTER_CHECKS={receipt['horizontal_gutter_checks']}",
                f"VERTICAL_GUTTER_CHECKS={receipt['vertical_gutter_checks']}",
                f"FAILURE_COUNT={receipt['failure_count']}",
                "",
                "Decision: audit-only proof. No QA, handoff, canonical swap, or premium validation is authorized by this receipt.",
            ]
        )
        + "\n",
        encoding="utf-8",
    )

    print(json.dumps({"status": receipt["status"], "failure_count": receipt["failure_count"]}, indent=2))
    return 0 if receipt["status"] == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
