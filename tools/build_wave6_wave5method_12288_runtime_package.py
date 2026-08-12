#!/usr/bin/env python3
import argparse
import hashlib
import json
import shutil
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image, ImageChops, ImageStat


ROWS = 50
COLUMNS = 50
TILE_SIZE = 512
GUTTER = 2
RUNTIME_TILE_SIZE = TILE_SIZE + GUTTER * 2
ORIGIN_CHUNK_X = 7
ORIGIN_CHUNK_Y = 7
SOURCE_SIZE = 12288
VIRTUAL_SIZE = ROWS * TILE_SIZE
EXPECTED_SOURCE_SHA256 = "3CE816052FFF97BCDE78251FA930C4D725DC622120D3644C806A9C1BE1330697"


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--reports", required=True, type=Path)
    parser.add_argument("--proof", required=True, type=Path)
    return parser.parse_args()


def source_extent(column: int, row: int) -> tuple[float, float, float, float]:
    scale = SOURCE_SIZE / VIRTUAL_SIZE
    x0 = max(0.0, (column * TILE_SIZE - GUTTER) * scale)
    y0 = max(0.0, (row * TILE_SIZE - GUTTER) * scale)
    x1 = min(float(SOURCE_SIZE), (column * TILE_SIZE + TILE_SIZE + GUTTER) * scale)
    y1 = min(float(SOURCE_SIZE), (row * TILE_SIZE + TILE_SIZE + GUTTER) * scale)
    return x0, y0, x1, y1


def make_tile(source: Image.Image, column: int, row: int) -> Image.Image:
    # Each tile, including its gutter, is sampled directly from one global source
    # coordinate space. This avoids neighbor swaps, rotations, and per-tile remaps.
    extent = source_extent(column, row)
    tile = source.transform(
        (RUNTIME_TILE_SIZE, RUNTIME_TILE_SIZE),
        Image.Transform.EXTENT,
        extent,
        resample=Image.Resampling.BICUBIC,
    )

    # Clamp outer-world gutters at the world border, matching texture Clamp mode.
    if column == 0:
        left = tile.crop((GUTTER, 0, GUTTER + 1, RUNTIME_TILE_SIZE))
        tile.paste(left.resize((GUTTER, RUNTIME_TILE_SIZE)), (0, 0))
    if column == COLUMNS - 1:
        right = tile.crop((GUTTER + TILE_SIZE - 1, 0, GUTTER + TILE_SIZE, RUNTIME_TILE_SIZE))
        tile.paste(right.resize((GUTTER, RUNTIME_TILE_SIZE)), (GUTTER + TILE_SIZE, 0))
    if row == 0:
        top = tile.crop((0, GUTTER, RUNTIME_TILE_SIZE, GUTTER + 1))
        tile.paste(top.resize((RUNTIME_TILE_SIZE, GUTTER)), (0, 0))
    if row == ROWS - 1:
        bottom = tile.crop((0, GUTTER + TILE_SIZE - 1, RUNTIME_TILE_SIZE, GUTTER + TILE_SIZE))
        tile.paste(bottom.resize((RUNTIME_TILE_SIZE, GUTTER)), (0, GUTTER + TILE_SIZE))
    return tile


def seam_rms(a: Image.Image, b: Image.Image) -> float:
    diff = ImageChops.difference(a.convert("RGB"), b.convert("RGB"))
    stat = ImageStat.Stat(diff)
    return float(sum(v * v for v in stat.rms) ** 0.5)


def build_proof_sheet(output_dir: Path, proof_dir: Path) -> Path:
    samples = [
        (0, 0), (0, 24), (0, 49),
        (8, 8), (8, 31), (9, 42),
        (24, 24), (24, 37), (39, 39),
        (40, 14), (49, 0), (49, 49),
    ]
    thumbs = []
    for row, column in samples:
        img = Image.open(output_dir / f"R{row:02d}C{column:02d}_g2.png").convert("RGB")
        img = img.crop((GUTTER, GUTTER, GUTTER + TILE_SIZE, GUTTER + TILE_SIZE))
        img.thumbnail((220, 220), Image.Resampling.LANCZOS)
        thumbs.append((row, column, img.copy()))

    cell_w, cell_h = 260, 250
    sheet = Image.new("RGB", (cell_w * 4, cell_h * 3), (15, 19, 20))
    for index, (row, column, thumb) in enumerate(thumbs):
        x = (index % 4) * cell_w + 20
        y = (index // 4) * cell_h + 20
        sheet.paste(thumb, (x, y))
    proof_dir.mkdir(parents=True, exist_ok=True)
    path = proof_dir / "wave5method_12288_runtime_sample_sheet.png"
    sheet.save(path)
    return path


def validate_sample_seams(output_dir: Path) -> dict:
    checks = []
    sample_rows = [0, 8, 16, 24, 32, 40, 48]
    sample_cols = [0, 8, 16, 24, 32, 40, 48]
    for row in sample_rows:
        for column in sample_cols[:-1]:
            left = Image.open(output_dir / f"R{row:02d}C{column:02d}_g2.png")
            right = Image.open(output_dir / f"R{row:02d}C{column + 1:02d}_g2.png")
            checks.append(seam_rms(left.crop((GUTTER + TILE_SIZE - 2, GUTTER, GUTTER + TILE_SIZE, GUTTER + TILE_SIZE)),
                                   right.crop((0, GUTTER, 2, GUTTER + TILE_SIZE))))
    for row in sample_rows[:-1]:
        for column in sample_cols:
            top = Image.open(output_dir / f"R{row:02d}C{column:02d}_g2.png")
            bottom = Image.open(output_dir / f"R{row + 1:02d}C{column:02d}_g2.png")
            checks.append(seam_rms(top.crop((GUTTER, GUTTER + TILE_SIZE - 2, GUTTER + TILE_SIZE, GUTTER + TILE_SIZE)),
                                   bottom.crop((GUTTER, 0, GUTTER + TILE_SIZE, 2))))
    return {
        "sample_count": len(checks),
        "max_rms": max(checks) if checks else 0.0,
        "avg_rms": sum(checks) / len(checks) if checks else 0.0,
    }


def main() -> int:
    args = parse_args()
    source_path = args.source.resolve()
    output_dir = args.output.resolve()
    reports_dir = args.reports.resolve()
    proof_dir = args.proof.resolve()
    reports_dir.mkdir(parents=True, exist_ok=True)

    source_sha = sha256_file(source_path)
    if source_sha != EXPECTED_SOURCE_SHA256:
        raise RuntimeError(f"Unexpected 12288 source SHA-256: {source_sha}")

    if output_dir.exists():
        backup = output_dir.with_name(output_dir.name + "_backup_" + datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"))
        shutil.move(str(output_dir), str(backup))
    output_dir.mkdir(parents=True, exist_ok=True)

    source = Image.open(source_path).convert("RGB")
    if source.size != (SOURCE_SIZE, SOURCE_SIZE):
        raise RuntimeError(f"Unexpected source size: {source.size}")

    tiles = []
    for row in range(ROWS):
        for column in range(COLUMNS):
            tile_id = f"R{row:02d}C{column:02d}_g2"
            tile = make_tile(source, column, row)
            tile.save(output_dir / f"{tile_id}.png", optimize=False)
            tiles.append({
                "row": row,
                "column": column,
                "chunk_x": ORIGIN_CHUNK_X + column,
                "chunk_y": ORIGIN_CHUNK_Y + row,
                "resource_name": tile_id,
                "width": RUNTIME_TILE_SIZE,
                "height": RUNTIME_TILE_SIZE,
                "gutter": GUTTER,
            })

    manifest = {
        "schema": "bee-kingdom.world-map.wave6-unity-runtime-bundle.v1",
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "package_kind": "wave5method_12288_preview_unity_runtime_audit_only",
        "source": {
            "master_sha256": EXPECTED_SOURCE_SHA256,
            "source_proof_path": str(source_path),
            "source_proof_resolution": [source.width, source.height],
            "virtual_runtime_resolution": [VIRTUAL_SIZE, VIRTUAL_SIZE],
            "production_note": "Unity 50x50 preview from one coherent 12288 Wave5-method superpanel. Audit only; not final QA handoff.",
        },
        "grid": {
            "rows": ROWS,
            "columns": COLUMNS,
            "tile_size": TILE_SIZE,
            "runtime_tile_size": RUNTIME_TILE_SIZE,
            "gutter": GUTTER,
            "origin_chunk_x": ORIGIN_CHUNK_X,
            "origin_chunk_y": ORIGIN_CHUNK_Y,
        },
        "tile_count": len(tiles),
        "tiles": tiles,
    }
    (output_dir / "runtime_manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    seam_stats = validate_sample_seams(output_dir)
    proof_sheet = build_proof_sheet(output_dir, proof_dir)
    receipt = {
        "schema": "beekingdom.wave6.wave5method_12288.runtime_package.receipt.v1",
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "status": "PASS_PACKAGE_CREATED_AUDIT_ONLY",
        "source": str(source_path),
        "source_sha256": source_sha,
        "output": str(output_dir),
        "tile_count": len(tiles),
        "runtime_tile_size": RUNTIME_TILE_SIZE,
        "gutter": GUTTER,
        "sample_seam_rms": seam_stats,
        "proof_sheet": str(proof_sheet),
        "gates": {
            "ONE_COHERENT_SOURCE_USED": "YES",
            "NO_TILE_SHUFFLE_ROTATE_OR_MIRROR": "YES",
            "READY_FOR_QA_BUILDERC": "NO",
            "READY_FOR_UNITY_HANDOFF": "NO",
            "READY_FOR_CANONICAL_SWAP": "NO",
            "MASTER_25600_AUTHORIZED": "NO",
        },
    }
    (reports_dir / "WorldMapWave6_Wave5Method12288_RuntimePackageReceipt.json").write_text(
        json.dumps(receipt, indent=2),
        encoding="utf-8",
    )
    print(json.dumps({"status": receipt["status"], "tile_count": len(tiles), "output": str(output_dir), "seams": seam_stats}))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
