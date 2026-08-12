from __future__ import annotations

import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SOURCE_IMAGE = PROJECT_ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_premium_v4_phase2_thread2_staging" / "thread2_v3o_reduced_offline_prototype" / "source" / "v3o_pictorial_source_4096_used_for_reduced_offline_prototype.png"
RESOURCE_ROOT = PROJECT_ROOT / "Assets" / "BeeKingdom" / "Playground" / "Resources" / "WorldMapWave6Runtime" / "UIB_ImmenseContinuousMaster50x50_v3o_reduced_audit_preview"
DOC_ROOT = PROJECT_ROOT / "Docs" / "BuilderA" / "WorldMapWave6_50x50_V3OReducedAuditPreview"

ROWS = 50
COLUMNS = 50
TILE_SIZE = 512
GUTTER = 2
RUNTIME_TILE_SIZE = TILE_SIZE + (GUTTER * 2)
CENTER_START_ROW = 21
CENTER_START_COLUMN = 21
CENTER_TILE_COUNT = 8
SOURCE_SHA256 = "8C0EB5250019B253BFE712D76B079E209AFE399DA645D8D68BD0BD77462F2D5B"


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def with_gutter(tile: Image.Image) -> Image.Image:
    runtime = Image.new("RGB", (RUNTIME_TILE_SIZE, RUNTIME_TILE_SIZE))
    runtime.paste(tile, (GUTTER, GUTTER))
    runtime.paste(tile.crop((0, 0, 1, TILE_SIZE)).resize((GUTTER, TILE_SIZE)), (0, GUTTER))
    runtime.paste(tile.crop((TILE_SIZE - 1, 0, TILE_SIZE, TILE_SIZE)).resize((GUTTER, TILE_SIZE)), (TILE_SIZE + GUTTER, GUTTER))
    runtime.paste(tile.crop((0, 0, TILE_SIZE, 1)).resize((TILE_SIZE, GUTTER)), (GUTTER, 0))
    runtime.paste(tile.crop((0, TILE_SIZE - 1, TILE_SIZE, TILE_SIZE)).resize((TILE_SIZE, GUTTER)), (GUTTER, TILE_SIZE + GUTTER))
    runtime.paste(tile.crop((0, 0, 1, 1)).resize((GUTTER, GUTTER)), (0, 0))
    runtime.paste(tile.crop((TILE_SIZE - 1, 0, TILE_SIZE, 1)).resize((GUTTER, GUTTER)), (TILE_SIZE + GUTTER, 0))
    runtime.paste(tile.crop((0, TILE_SIZE - 1, 1, TILE_SIZE)).resize((GUTTER, GUTTER)), (0, TILE_SIZE + GUTTER))
    runtime.paste(tile.crop((TILE_SIZE - 1, TILE_SIZE - 1, TILE_SIZE, TILE_SIZE)).resize((GUTTER, GUTTER)), (TILE_SIZE + GUTTER, TILE_SIZE + GUTTER))
    return runtime


def tile_record(row: int, column: int, path: Path, source_role: str) -> dict:
    return {
        "row": row,
        "column": column,
        "file": path.name,
        "runtime_sha256": sha256_file(path),
        "source_role": source_role,
    }


def main() -> None:
    if not SOURCE_IMAGE.exists():
        raise FileNotFoundError(SOURCE_IMAGE)

    source_sha = sha256_file(SOURCE_IMAGE)
    if source_sha != SOURCE_SHA256:
        raise RuntimeError(f"Unexpected V3O source SHA-256: {source_sha}")

    RESOURCE_ROOT.mkdir(parents=True, exist_ok=True)
    DOC_ROOT.mkdir(parents=True, exist_ok=True)

    for old_png in RESOURCE_ROOT.glob("R??C??_g2.png"):
        old_png.unlink()

    source = Image.open(SOURCE_IMAGE).convert("RGB")
    if source.size != (4096, 4096):
        raise RuntimeError(f"Unexpected V3O reduced source size: {source.size}")

    placeholder_tile = Image.new("RGB", (RUNTIME_TILE_SIZE, RUNTIME_TILE_SIZE), (14, 24, 18))
    records = []
    real_tile_count = 0

    for row in range(ROWS):
        for column in range(COLUMNS):
            filename = f"R{row:02d}C{column:02d}_g2.png"
            output = RESOURCE_ROOT / filename
            in_center = (
                CENTER_START_ROW <= row < CENTER_START_ROW + CENTER_TILE_COUNT
                and CENTER_START_COLUMN <= column < CENTER_START_COLUMN + CENTER_TILE_COUNT
            )
            if in_center:
                source_row = row - CENTER_START_ROW
                source_column = column - CENTER_START_COLUMN
                left = source_column * TILE_SIZE
                top = source_row * TILE_SIZE
                tile = source.crop((left, top, left + TILE_SIZE, top + TILE_SIZE))
                with_gutter(tile).save(output, "PNG")
                real_tile_count += 1
                role = "V3O_REDUCED_REAL_TILE"
            else:
                placeholder_tile.save(output, "PNG")
                role = "AUDIT_PLACEHOLDER_OUTSIDE_REDUCED_8X8"

            records.append(tile_record(row, column, output, role))

    manifest = {
        "schema": "bee-kingdom.world-map.wave6-unity-runtime-bundle.v1",
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "grid": {
            "rows": ROWS,
            "columns": COLUMNS,
            "tile_size": TILE_SIZE,
            "runtime_tile_size": RUNTIME_TILE_SIZE,
            "gutter": GUTTER,
            "origin_chunk_x": 7,
            "origin_chunk_y": 7,
            "tile_count": ROWS * COLUMNS,
        },
        "source": {
            "master_sha256": source_sha,
            "master_path": str(SOURCE_IMAGE),
            "visual_status": "V3O_REDUCED_AUDIT_PREVIEW_ONLY_NOT_FINAL_50X50",
            "notes": "Only rows 21-28 and columns 21-28 contain the V3O reduced 8x8 art. Outside tiles are neutral placeholders for Unity streaming validation.",
        },
        "audit_window": {
            "real_rows": [CENTER_START_ROW, CENTER_START_ROW + CENTER_TILE_COUNT - 1],
            "real_columns": [CENTER_START_COLUMN, CENTER_START_COLUMN + CENTER_TILE_COUNT - 1],
            "unity_center_chunk_x": 31,
            "unity_center_chunk_y": 31,
            "recommended_zoom": 0.58,
        },
        "tiles": records,
    }
    (RESOURCE_ROOT / "runtime_manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    receipt = {
        "artifact": "WorldMapWave6_V3OReducedAuditPreview_UnityRuntimePackage",
        "status": "PASS_STATIC_PACKAGE_CREATED",
        "created_utc": manifest["created_utc"],
        "resource_root": str(RESOURCE_ROOT),
        "source_image": str(SOURCE_IMAGE),
        "source_master_sha256": source_sha,
        "tile_count": ROWS * COLUMNS,
        "real_v3o_tile_count": real_tile_count,
        "placeholder_tile_count": (ROWS * COLUMNS) - real_tile_count,
        "ready_for_reduced_unity_visual_test": True,
        "ready_for_qa_builderc": False,
        "ready_for_unity_handoff": False,
        "master_25600_authorized": False,
        "premium_50x50_validated": False,
    }
    (DOC_ROOT / "WorldMapWave6_V3OReducedAuditPreview_RuntimePackageReceipt.json").write_text(
        json.dumps(receipt, indent=2),
        encoding="utf-8",
    )
    (DOC_ROOT / "WorldMapWave6_V3OReducedAuditPreview_RuntimePackageCheckpoint.md").write_text(
        "\n".join(
            [
                "# WorldMap Wave6 V3O Reduced Audit Preview Runtime Package",
                "",
                "STATUS=PASS_STATIC_PACKAGE_CREATED",
                f"source_master_sha256={source_sha}",
                f"resource_root={RESOURCE_ROOT}",
                "real_v3o_window=rows 21-28, columns 21-28",
                "unity_initial_view=C31_31, zoom 0.58",
                "READY_FOR_REDUCED_UNITY_VISUAL_TEST=YES",
                "READY_FOR_QA_BUILDERC=NO",
                "READY_FOR_UNITY_HANDOFF=NO",
                "MASTER_25600_AUTHORIZED=NO",
                "PREMIUM_50X50_VALIDATED=NO",
                "",
            ]
        ),
        encoding="utf-8",
    )

    print(json.dumps(receipt, indent=2))


if __name__ == "__main__":
    main()
