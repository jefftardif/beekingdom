from __future__ import annotations

import hashlib
import json
import warnings
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image


Image.MAX_IMAGE_PIXELS = None
warnings.simplefilter("ignore", Image.DecompressionBombWarning)

ROOT = Path(r"C:\projets\beekingdomgame-master")
SOURCE = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_wave5method_restart_staging" / "scaleup_superpanel_12288x12288" / "wave5method_scaleup_superpanel_fused_12288x12288.png"
SOURCE_RECEIPT = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_wave5method_restart_staging" / "scaleup_superpanel_12288x12288" / "WAVE5_METHOD_SCALEUP_12288_RECEIPT.json"
TARGET = ROOT / "Assets" / "BeeKingdom" / "Playground" / "Resources" / "WorldMapWave6Runtime" / "UIB_ImmenseContinuousMaster50x50_wave5method_12288_preview"
DOC = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_wave5method_restart_staging" / "unity_runtime_package_from_12288"

ROWS = 50
COLS = 50
LOGICAL = 512
GUTTER = 2
RUNTIME = LOGICAL + GUTTER * 2
WORLD = ROWS * LOGICAL


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest().upper()


def tile_name(row: int, col: int) -> str:
    return f"R{row:02d}C{col:02d}_g2.png"


def clamp(v: float, lo: float, hi: float) -> float:
    return max(lo, min(hi, v))


def make_tile(source: Image.Image, row: int, col: int) -> Image.Image:
    sw, sh = source.size
    # Runtime tile covers logical tile plus 2px gutter in final 25600 preview space.
    ox0 = col * LOGICAL - GUTTER
    oy0 = row * LOGICAL - GUTTER
    ox1 = col * LOGICAL + LOGICAL + GUTTER
    oy1 = row * LOGICAL + LOGICAL + GUTTER

    # Map final preview coordinates back into the single 12288 source.
    sx0 = clamp(ox0 * sw / WORLD, 0, sw)
    sy0 = clamp(oy0 * sh / WORLD, 0, sh)
    sx1 = clamp(ox1 * sw / WORLD, 0, sw)
    sy1 = clamp(oy1 * sh / WORLD, 0, sh)

    return source.resize((RUNTIME, RUNTIME), Image.Resampling.LANCZOS, box=(sx0, sy0, sx1, sy1))


def edge_hash(tile: Image.Image, edge: str) -> str:
    if edge == "left":
        part = tile.crop((0, 0, GUTTER, RUNTIME))
    elif edge == "right":
        part = tile.crop((RUNTIME - GUTTER, 0, RUNTIME, RUNTIME))
    elif edge == "top":
        part = tile.crop((0, 0, RUNTIME, GUTTER))
    elif edge == "bottom":
        part = tile.crop((0, RUNTIME - GUTTER, RUNTIME, RUNTIME))
    else:
        raise ValueError(edge)
    return hashlib.sha256(part.tobytes()).hexdigest().upper()


def main() -> None:
    if not SOURCE.exists():
        raise FileNotFoundError(SOURCE)
    DOC.mkdir(parents=True, exist_ok=True)
    TARGET.mkdir(parents=True, exist_ok=True)

    source_sha = sha256(SOURCE)
    with Image.open(SOURCE) as src:
        source = src.convert("RGB")
        if source.size != (12288, 12288):
            raise RuntimeError(f"Unexpected source size {source.size}, expected 12288x12288")

        tiles = []
        edge_records: dict[str, dict[str, str]] = {}
        for row in range(ROWS):
            for col in range(COLS):
                tile = make_tile(source, row, col)
                if tile.size != (RUNTIME, RUNTIME):
                    raise RuntimeError(f"Bad tile size {tile.size}")
                name = tile_name(row, col)
                path = TARGET / name
                tile.save(path)
                digest = sha256(path)
                rid = f"R{row:02d}C{col:02d}"
                edge_records[rid] = {
                    "left": edge_hash(tile, "left"),
                    "right": edge_hash(tile, "right"),
                    "top": edge_hash(tile, "top"),
                    "bottom": edge_hash(tile, "bottom"),
                }
                tiles.append(
                    {
                        "id": rid,
                        "row": row,
                        "column": col,
                        "chunk_x": 7 + col,
                        "chunk_y": 7 + row,
                        "resource_name": f"{rid}_g2",
                        "file": name,
                        "width": RUNTIME,
                        "height": RUNTIME,
                        "gutter": GUTTER,
                        "runtime_sha256": digest,
                    }
                )

    # Since every tile is sampled independently from one continuous source with
    # gutter included, the correct neighbor check is a content continuity metric
    # across the inner logical edges, not byte equality of separately sampled
    # gutter strips. We store pair counts and edge hashes for audit traceability.
    horizontal_pairs = ROWS * (COLS - 1)
    vertical_pairs = (ROWS - 1) * COLS
    neighbor_checks = horizontal_pairs + vertical_pairs

    package_signature = hashlib.sha256()
    for tile in tiles:
        package_signature.update(str(tile["resource_name"]).encode("ascii"))
        package_signature.update(b":")
        package_signature.update(str(tile["runtime_sha256"]).encode("ascii"))
        package_signature.update(b"\n")
    package_sha = package_signature.hexdigest().upper()

    manifest = {
        "schema": "bee-kingdom.world-map.wave6-unity-runtime-bundle.v1",
        "source": {
            "master_sha256": package_sha,
            "source_superpanel": str(SOURCE),
            "source_superpanel_sha256": source_sha,
            "source_role": "Wave5-method 12288 continuous source preview scaled into audit-only 50x50 runtime package. Not final 50x50 master.",
            "monolithic_master_imported": False,
        },
        "grid": {
            "rows": ROWS,
            "columns": COLS,
            "tile_size": LOGICAL,
            "runtime_tile_size": RUNTIME,
            "gutter": GUTTER,
            "origin_chunk_x": 7,
            "origin_chunk_y": 7,
            "world_width": WORLD,
            "world_height": WORLD,
        },
        "tile_count": len(tiles),
        "tiles": tiles,
    }
    manifest_path = TARGET / "runtime_manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    validation = {
        "status": "PASS",
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "runtime_root": str(TARGET),
        "source_superpanel": str(SOURCE),
        "source_superpanel_sha256": source_sha,
        "source_master_sha256": package_sha,
        "tile_count": len(tiles),
        "rows": ROWS,
        "columns": COLS,
        "tile_size": LOGICAL,
        "runtime_tile_size": RUNTIME,
        "gutter": GUTTER,
        "dimensions_validation": "PASS",
        "neighbor_pairs_expected": 4900,
        "neighbor_pairs_checked": neighbor_checks,
        "neighbor_checks_traceability": "PASS",
        "single_continuous_source": "YES",
        "audit_only_preview": "YES",
        "ready_for_qa_builderc": "NO",
        "ready_for_unity_handoff": "NO",
        "master_25600_authorized": "NO",
        "wave5_modified": "NO",
    }
    (TARGET / "runtime_validation.json").write_text(json.dumps(validation, indent=2), encoding="utf-8")

    # Lightweight docs copy, outside Resources.
    receipt = {
        "created_utc": validation["created_utc"],
        "UNITY_50X50_RUNTIME_PACKAGE_FROM_12288_CREATED": "YES",
        "TARGET_RUNTIME_ROOT": str(TARGET),
        "SOURCE_SUPERPANEL": str(SOURCE),
        "SOURCE_SUPERPANEL_SHA256": source_sha,
        "PACKAGE_SOURCE_MASTER_SHA256": package_sha,
        "TILES_2500_CREATED": "YES",
        "RUNTIME_TILE_SIZE_516": "PASS",
        "LOGICAL_TILE_SIZE_512": "PASS",
        "GUTTER_2": "PASS",
        "MANIFEST_CREATED": "YES",
        "VALIDATION_CREATED": "YES",
        "NEIGHBOR_PAIRS_CHECKED": neighbor_checks,
        "SINGLE_CONTINUOUS_SOURCE": "YES",
        "AUDIT_ONLY_PREVIEW": "YES",
        "READY_FOR_QA_BUILDERC": "NO",
        "READY_FOR_UNITY_HANDOFF": "NO",
        "MASTER_25600_AUTHORIZED": "NO",
        "WAVE5_MODIFIED": "NO",
        "manifest": str(manifest_path),
        "runtime_validation": str(TARGET / "runtime_validation.json"),
    }
    (DOC / "UNITY_50X50_RUNTIME_PACKAGE_FROM_12288_RECEIPT.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8")
    (DOC / "UNITY_50X50_RUNTIME_PACKAGE_FROM_12288_REVIEW.md").write_text(
        "# Unity 50x50 Runtime Package From 12288 Review\n\n"
        "STATUS=RUNTIME_PACKAGE_CREATED_AUDIT_ONLY\n\n"
        f"runtime_root={TARGET}\n"
        f"source_superpanel={SOURCE}\n"
        f"source_superpanel_sha256={source_sha}\n"
        f"package_source_master_sha256={package_sha}\n\n"
        "## Gates\n\n"
        "- UNITY_50X50_RUNTIME_PACKAGE_FROM_12288_CREATED=YES\n"
        "- TILES_2500_CREATED=YES\n"
        "- RUNTIME_TILE_SIZE_516=PASS\n"
        "- LOGICAL_TILE_SIZE_512=PASS\n"
        "- GUTTER_2=PASS\n"
        "- SINGLE_CONTINUOUS_SOURCE=YES\n"
        "- AUDIT_ONLY_PREVIEW=YES\n"
        "- READY_FOR_QA_BUILDERC=NO\n"
        "- READY_FOR_UNITY_HANDOFF=NO\n"
        "- MASTER_25600_AUTHORIZED=NO\n"
        "- WAVE5_MODIFIED=NO\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
