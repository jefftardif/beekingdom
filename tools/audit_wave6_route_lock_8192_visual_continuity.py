#!/usr/bin/env python3
import argparse
import json
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image, ImageChops, ImageStat


ROWS = 50
COLUMNS = 50
TILE_SIZE = 512
GUTTER = 2


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--package", required=True, type=Path)
    parser.add_argument("--report-dir", required=True, type=Path)
    parser.add_argument("--preview-size", default=3200, type=int)
    return parser.parse_args()


def tile_path(package: Path, row: int, column: int) -> Path:
    return package / f"R{row:02d}C{column:02d}_g2.png"


def load_core(package: Path, row: int, column: int) -> Image.Image:
    image = Image.open(tile_path(package, row, column)).convert("RGB")
    return image.crop((GUTTER, GUTTER, GUTTER + TILE_SIZE, GUTTER + TILE_SIZE))


def mean_abs_delta(left: Image.Image, right: Image.Image) -> float:
    diff = ImageChops.difference(left, right)
    stat = ImageStat.Stat(diff)
    return sum(stat.mean) / len(stat.mean)


def main() -> int:
    args = parse_args()
    package = args.package.resolve()
    report_dir = args.report_dir.resolve()
    report_dir.mkdir(parents=True, exist_ok=True)

    horizontal = []
    vertical = []

    for row in range(ROWS):
        previous_core = load_core(package, row, 0)
        for column in range(COLUMNS - 1):
            next_core = load_core(package, row, column + 1)
            delta = mean_abs_delta(
                previous_core.crop((TILE_SIZE - 1, 0, TILE_SIZE, TILE_SIZE)),
                next_core.crop((0, 0, 1, TILE_SIZE)),
            )
            horizontal.append({"row": row, "column": column, "delta": delta})
            previous_core = next_core

    previous_row = [load_core(package, 0, column) for column in range(COLUMNS)]
    for row in range(ROWS - 1):
        next_row = [load_core(package, row + 1, column) for column in range(COLUMNS)]
        for column in range(COLUMNS):
            delta = mean_abs_delta(
                previous_row[column].crop((0, TILE_SIZE - 1, TILE_SIZE, TILE_SIZE)),
                next_row[column].crop((0, 0, TILE_SIZE, 1)),
            )
            vertical.append({"row": row, "column": column, "delta": delta})
        previous_row = next_row

    all_deltas = [item["delta"] for item in horizontal] + [item["delta"] for item in vertical]
    top_horizontal = sorted(horizontal, key=lambda item: item["delta"], reverse=True)[:25]
    top_vertical = sorted(vertical, key=lambda item: item["delta"], reverse=True)[:25]

    preview_tile = max(1, args.preview_size // COLUMNS)
    preview = Image.new("RGB", (preview_tile * COLUMNS, preview_tile * ROWS))
    for row in range(ROWS):
        for column in range(COLUMNS):
            core = load_core(package, row, column)
            core = core.resize((preview_tile, preview_tile), Image.Resampling.LANCZOS)
            preview.paste(core, (column * preview_tile, row * preview_tile))

    preview_path = report_dir / "WorldMapWave6_RouteLock8192ScaleBridge_ReconstructedTilePreview.png"
    preview.save(preview_path)

    receipt = {
        "schema": "beekingdom.wave6.route_lock_8192_scale_bridge.visual_continuity_audit.v1",
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "status": "PASS_AUDIT_CREATED_REQUIRES_HUMAN_VISUAL_REVIEW",
        "package": str(package),
        "preview": str(preview_path),
        "checks": {
            "horizontal_core_seams": len(horizontal),
            "vertical_core_seams": len(vertical),
            "mean_delta": sum(all_deltas) / len(all_deltas),
            "max_delta": max(all_deltas),
            "top_horizontal": top_horizontal,
            "top_vertical": top_vertical,
        },
        "gate_decision": {
            "READY_FOR_QA_BUILDERC": "NO",
            "READY_FOR_UNITY_HANDOFF": "NO",
            "READY_FOR_CANONICAL_SWAP": "NO",
            "PREMIUM_50X50_VALIDATED": "NO",
        },
    }

    (report_dir / "WorldMapWave6_RouteLock8192ScaleBridge_VisualContinuityReceipt.json").write_text(
        json.dumps(receipt, indent=2),
        encoding="utf-8",
    )
    (report_dir / "WorldMapWave6_RouteLock8192ScaleBridge_VisualContinuityReceipt.md").write_text(
        "\n".join(
            [
                "# WorldMapWave6 Route-Lock 8192 Scale-Bridge Visual Continuity Audit",
                "",
                f"STATUS={receipt['status']}",
                f"CREATED_UTC={receipt['created_utc']}",
                f"PACKAGE={receipt['package']}",
                f"PREVIEW={receipt['preview']}",
                f"HORIZONTAL_CORE_SEAMS={receipt['checks']['horizontal_core_seams']}",
                f"VERTICAL_CORE_SEAMS={receipt['checks']['vertical_core_seams']}",
                f"MEAN_CORE_EDGE_DELTA={receipt['checks']['mean_delta']:.4f}",
                f"MAX_CORE_EDGE_DELTA={receipt['checks']['max_delta']:.4f}",
                "",
                "Decision: audit artifact only. Human Unity visual review is still required before premium validation.",
            ]
        )
        + "\n",
        encoding="utf-8",
    )
    print(json.dumps({"status": receipt["status"], "preview": str(preview_path), "max_delta": receipt["checks"]["max_delta"]}))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
