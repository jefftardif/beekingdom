from __future__ import annotations

import hashlib
import json
import math
import shutil
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter


ROOT = Path(r"C:\projets\beekingdomgame-master")
BASELINE = ROOT / r"Assets\BeeKingdom\Playground\Resources\WorldMapWave6Runtime\UIB_ImmenseContinuousMaster50x50_v2i_repair_audit_preview"
PACKAGE = ROOT / r"Assets\BeeKingdom\Playground\Resources\WorldMapWave6Runtime\UIB_ImmenseContinuousMaster50x50_v2i_selected_hd_local_repair_review"
DOCS = ROOT / r"Docs\BuilderA\WorldMapWave6_50x50_SelectedHdLocalRepairReview"
STAGE = ROOT / r"artifacts\UIB_ImmenseContinuousMaster50x50_premium_v4_phase2_thread2_staging\thread2_selected_hd_local_repair_review"
PROOF = STAGE / "proof"


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def tile_name(row: int, col: int) -> str:
    return f"R{row:02d}C{col:02d}_g2.png"


def baseline_tile(row: int, col: int) -> Path:
    return BASELINE / tile_name(row, col)


def package_tile(row: int, col: int) -> Path:
    return PACKAGE / tile_name(row, col)


def load_tile(row: int, col: int, patched: bool = False) -> Image.Image:
    source = package_tile(row, col) if patched and package_tile(row, col).exists() else baseline_tile(row, col)
    return Image.open(source).convert("RGB")


def save_png(image: Image.Image, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, optimize=True)


def copy_baseline_package() -> None:
    PACKAGE.mkdir(parents=True, exist_ok=True)
    for source in BASELINE.iterdir():
        if source.is_file():
            shutil.copy2(source, PACKAGE / source.name)


def color_transfer(source: Image.Image, reference: Image.Image) -> Image.Image:
    source_array = np.asarray(source).astype(np.float32)
    reference_array = np.asarray(reference).astype(np.float32)
    source_mean = source_array.reshape(-1, 3).mean(axis=0)
    source_std = source_array.reshape(-1, 3).std(axis=0) + 0.001
    reference_mean = reference_array.reshape(-1, 3).mean(axis=0)
    reference_std = reference_array.reshape(-1, 3).std(axis=0) + 0.001
    result = (source_array - source_mean) / source_std * reference_std + reference_mean
    return Image.fromarray(np.clip(result, 0, 255).astype(np.uint8), "RGB")


def protect_perimeter(original: Image.Image, edited: Image.Image, margin: int = 18) -> Image.Image:
    result = edited.copy()
    width, height = result.size
    result.paste(original.crop((0, 0, width, margin)), (0, 0))
    result.paste(original.crop((0, height - margin, width, height)), (0, height - margin))
    result.paste(original.crop((0, 0, margin, height)), (0, 0))
    result.paste(original.crop((width - margin, 0, width, height)), (width - margin, 0))
    return result


def organic_mask(width: int, height: int, center_y: float, radius: float, seed: int, edge_margin: int = 36) -> Image.Image:
    mask = Image.new("L", (width, height), 0)
    pixels = mask.load()
    for y in range(height):
        for x in range(width):
            wave = 18 * math.sin(x * 0.023 + seed) + 8 * math.sin(x * 0.071 + seed * 0.3)
            distance = abs(y - (center_y + wave))
            vertical = max(0.0, min(1.0, 1.0 - distance / radius))
            edge = min(x, width - 1 - x, y, height - 1 - y)
            edge_weight = max(0.0, min(1.0, (edge - edge_margin) / 50.0))
            pixels[x, y] = int(255 * vertical * edge_weight)
    return mask.filter(ImageFilter.GaussianBlur(10))


def repair_horizontal_seam(row: int, col: int) -> Image.Image:
    original = load_tile(row, col)
    width, height = original.size
    band_top = 152
    band_bottom = 366
    band_height = band_bottom - band_top

    upper = original.crop((0, 64, width, 64 + band_height)).resize((width, band_height), Image.Resampling.BICUBIC)
    lower = original.crop((0, 302, width, 302 + band_height)).resize((width, band_height), Image.Resampling.BICUBIC)
    bridge = Image.blend(upper, lower, 0.52)

    if col > 0:
        left = load_tile(row, col - 1).crop((width - 180, band_top, width, band_bottom)).resize((180, band_height), Image.Resampling.BICUBIC)
        bridge.paste(left, (0, 0), organic_mask(180, band_height, band_height / 2, 95, row * 100 + col))
    if col < 49:
        right = load_tile(row, col + 1).crop((0, band_top, 180, band_bottom)).resize((180, band_height), Image.Resampling.BICUBIC)
        bridge.paste(right, (width - 180, 0), organic_mask(180, band_height, band_height / 2, 95, row * 100 + col + 33))

    bridge = Image.blend(bridge.filter(ImageFilter.GaussianBlur(3)), bridge.filter(ImageFilter.UnsharpMask(radius=1.1, percent=145, threshold=3)), 0.62)
    bridge = ImageEnhance.Contrast(bridge).enhance(1.07)
    bridge = ImageEnhance.Sharpness(bridge).enhance(1.25)

    edited = original.copy()
    mask = organic_mask(width, band_height, band_height / 2, 108, 2000 + col, edge_margin=26)
    edited.paste(bridge, (0, band_top), mask)
    return protect_perimeter(original, edited)


def repair_mountain_orientation(row: int, col: int) -> Image.Image:
    original = load_tile(row, col)
    width, height = original.size
    y0, y1 = 30, 338
    target_height = y1 - y0

    below = load_tile(min(49, row + 1), col).crop((0, 0, width, min(height, target_height))).resize((width, target_height), Image.Resampling.BICUBIC)
    own_lower = original.crop((0, 170, width, min(height, 170 + target_height))).resize((width, target_height), Image.Resampling.BICUBIC)
    above = load_tile(max(0, row - 1), col).crop((0, 236, width, min(height, 236 + target_height))).resize((width, target_height), Image.Resampling.BICUBIC)

    reference = original.crop((0, y0, width, y1))
    replacement = Image.blend(color_transfer(own_lower, reference), color_transfer(below, reference), 0.48)
    replacement = Image.blend(replacement, color_transfer(above, reference), 0.18)
    replacement = replacement.filter(ImageFilter.UnsharpMask(radius=1.0, percent=135, threshold=3))
    replacement = ImageEnhance.Contrast(replacement).enhance(1.06)

    mask = Image.new("L", (width, target_height), 0)
    pixels = mask.load()
    phase = row * 17 + col * 5
    for y in range(target_height):
        global_y = y0 + y
        for x in range(width):
            upper_limit = 42 + 20 * math.sin(x * 0.021 + phase)
            lower_limit = 302 + 28 * math.sin(x * 0.018 + phase * 0.4)
            vertical = max(0.0, min(1.0, (global_y - upper_limit) / 84.0)) * max(0.0, min(1.0, (lower_limit - global_y) / 96.0))
            edge = min(x, width - 1 - x, global_y, height - 1 - global_y)
            edge_weight = max(0.0, min(1.0, (edge - 38) / 58.0))
            pixels[x, y] = int(255 * vertical * edge_weight)
    mask = mask.filter(ImageFilter.GaussianBlur(13))

    edited = original.copy()
    edited.paste(replacement, (0, y0), mask)

    # Reintroduce small same-tile rock/forest texture so the repair does not read as a soft band.
    detail = original.crop((50, 245, width - 50, 438)).resize((width - 100, 160), Image.Resampling.BICUBIC)
    detail = detail.filter(ImageFilter.UnsharpMask(radius=1.0, percent=150, threshold=4))
    detail_mask = organic_mask(width - 100, 160, 70, 80, 5000 + row * 100 + col, edge_margin=8).point(lambda value: int(value * 0.33))
    edited.paste(detail, (50, 145), detail_mask)
    return protect_perimeter(original, edited)


def stitch(rows: list[int], cols: list[int], patched: bool, title: str) -> Image.Image:
    tile_size = 516
    header = 42
    image = Image.new("RGB", (tile_size * len(cols), header + tile_size * len(rows)), (8, 10, 9))
    draw = ImageDraw.Draw(image)
    draw.text((10, 12), title, fill=(255, 230, 120))
    for row_index, row in enumerate(rows):
        for col_index, col in enumerate(cols):
            x = col_index * tile_size
            y = header + row_index * tile_size
            image.paste(load_tile(row, col, patched), (x, y))
            draw.rectangle((x, y, x + tile_size - 1, y + tile_size - 1), outline=(255, 210, 0), width=2)
            draw.text((x + 8, y + 8), f"R{row:02d}C{col:02d}", fill=(255, 255, 255))
    return image


def side_by_side(left: Image.Image, right: Image.Image, output: Path) -> None:
    gap = 24
    result = Image.new("RGB", (left.width + right.width + gap, max(left.height, right.height)), (6, 8, 7))
    result.paste(left, (0, 0))
    result.paste(right, (left.width + gap, 0))
    save_png(result, output)


def update_manifest(changed_tiles: list[tuple[int, int]]) -> str:
    manifest_path = PACKAGE / "runtime_manifest.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    changed_by_name = {tile_name(row, col): sha256(package_tile(row, col)) for row, col in changed_tiles}
    for tile in manifest["tiles"]:
        file_name = tile.get("file")
        if file_name in changed_by_name:
            tile["runtime_sha256"] = changed_by_name[file_name]
    package_digest = hashlib.sha256()
    for png in sorted(PACKAGE.glob("R??C??_g2.png")):
        package_digest.update(sha256(png).encode("ascii"))
        package_digest.update(b"\n")
    package_sha = package_digest.hexdigest().upper()
    manifest["source"]["master_sha256"] = package_sha
    manifest["source"]["source"] = str(STAGE / "local_defect_repair_patch_tiles")
    manifest["source"]["source_role"] = "Selected HD local repair review sibling; V2I repair baseline plus local patches for DEFECT-001 R02C46-C48 and DEFECT-002 R18-R20 C45-C47; audit-only, not final handoff"
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    return package_sha


def validate_package(changed_tiles: list[tuple[int, int]], package_sha: str) -> dict:
    pngs = sorted(PACKAGE.glob("R??C??_g2.png"))
    unchanged_diffs = []
    changed_names = {tile_name(row, col) for row, col in changed_tiles}
    for png in pngs:
        base = BASELINE / png.name
        if png.name in changed_names:
            continue
        if sha256(png) != sha256(base):
            unchanged_diffs.append(png.name)
    tile_metrics = []
    for row, col in changed_tiles:
        original = Image.open(baseline_tile(row, col)).convert("RGB")
        patched = Image.open(package_tile(row, col)).convert("RGB")
        diff = np.abs(np.asarray(original, dtype=np.int16) - np.asarray(patched, dtype=np.int16)).sum(axis=2)
        edge = np.zeros((516, 516), dtype=bool)
        edge[:18, :] = True
        edge[-18:, :] = True
        edge[:, :18] = True
        edge[:, -18:] = True
        tile_metrics.append(
            {
                "tile": tile_name(row, col),
                "baseline_sha256": sha256(baseline_tile(row, col)),
                "patched_sha256": sha256(package_tile(row, col)),
                "changed_pixels": int((diff > 0).sum()),
                "edge_pixels_changed_within_18px": int(((diff > 0) & edge).sum()),
            }
        )
    return {
        "png_count": len(pngs),
        "meta_count": len(list(PACKAGE.glob("*.png.meta"))),
        "package_sha256": package_sha,
        "unchanged_tile_diffs": unchanged_diffs,
        "tile_metrics": tile_metrics,
        "validation_pass": len(pngs) == 2500 and len(unchanged_diffs) == 0,
    }


def write_review_files(changed_tiles: list[tuple[int, int]], validation: dict) -> None:
    DOCS.mkdir(parents=True, exist_ok=True)
    changed_list = [tile_name(row, col) for row, col in changed_tiles]
    manifest = {
        "artifact": "THREAD2_SELECTED_HD_LOCAL_REPAIR_REVIEW_PACKAGE",
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "baseline_resource_root": "WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v2i_repair_audit_preview",
        "review_resource_root": "WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v2i_selected_hd_local_repair_review",
        "review_scene": "Assets/Scenes/WorldMapWave6SelectedHdLocalRepairReview.unity",
        "changed_tiles": changed_list,
        "defects": [
            {
                "id": "DEFECT-001",
                "unity_chunk": "C54_09",
                "internal_neighborhood": "R01..R03/C46..C48",
                "patched_tiles": [tile_name(2, col) for col in range(46, 49)],
            },
            {
                "id": "DEFECT-002",
                "unity_chunk": "C53_26",
                "internal_neighborhood": "R18..R20/C45..C47",
                "patched_tiles": [tile_name(row, col) for row in range(18, 21) for col in range(45, 48)],
            },
        ],
        "validation": validation,
        "proofs": {
            "defect_001_before_after": str(PROOF / "DEFECT_001_R02_C46_C48_before_after.png"),
            "defect_002_before_after": str(PROOF / "DEFECT_002_R18_R20_C45_C47_before_after.png"),
            "changed_tiles_contact_sheet": str(PROOF / "changed_tiles_contact_sheet.png"),
        },
        "gates": {
            "LOCAL_PATCH_REVIEW_PACKAGE_CREATED": "YES",
            "BASELINE_V2I_REPAIR_INTACT": "YES",
            "ONLY_LOCAL_PATCH_TILES_CHANGED": "YES" if not validation["unchanged_tile_diffs"] else "NO",
            "UNITY_RETEST_REQUIRED": "YES",
            "READY_FOR_QA_BUILDERC": "NO",
            "READY_FOR_UNITY_HANDOFF": "NO",
            "MASTER_25600_AUTHORIZED": "NO",
        },
    }
    (STAGE / "THREAD2_SELECTED_HD_LOCAL_REPAIR_REVIEW_MANIFEST.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    receipt = {
        "artifact": "THREAD2_SELECTED_HD_LOCAL_REPAIR_REVIEW",
        "status": "REVIEW_PACKAGE_READY_FOR_UNITY_RETEST_ONLY",
        "resource_root_unity": manifest["review_resource_root"],
        "scene": manifest["review_scene"],
        "package_sha256": validation["package_sha256"],
        "changed_tiles": changed_list,
        "manifest": str(STAGE / "THREAD2_SELECTED_HD_LOCAL_REPAIR_REVIEW_MANIFEST.json"),
        "gates": manifest["gates"],
    }
    (STAGE / "THREAD2_SELECTED_HD_LOCAL_REPAIR_REVIEW_RECEIPT.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8")
    (DOCS / "WorldMapWave6_SelectedHdLocalRepairReview_Checkpoint.md").write_text(
        "\n".join(
            [
                "# WorldMapWave6 Selected HD Local Repair Review",
                "",
                "STATUS=REVIEW_PACKAGE_READY_FOR_UNITY_RETEST_ONLY",
                "BASELINE_PACKAGE=UIB_ImmenseContinuousMaster50x50_v2i_repair_audit_preview",
                "SIBLING_PACKAGE=UIB_ImmenseContinuousMaster50x50_v2i_selected_hd_local_repair_review",
                "SCENE=Assets/Scenes/WorldMapWave6SelectedHdLocalRepairReview.unity",
                "",
                "## Scope",
                "- DEFECT-001 C54_09 / R02C47: patched local row R02C46..R02C48, controlled against R01..R03/C46..C48.",
                "- DEFECT-002 C53_26 / R19C46: patched local neighborhood R18..R20/C45..C47.",
                "- Baseline V2I repair package remains intact.",
                "- No full 25600 master and no blind 2500-tile regeneration.",
                "",
                "## Proofs",
                f"- {PROOF / 'DEFECT_001_R02_C46_C48_before_after.png'}",
                f"- {PROOF / 'DEFECT_002_R18_R20_C45_C47_before_after.png'}",
                f"- {PROOF / 'changed_tiles_contact_sheet.png'}",
                "",
                "## Gates",
                "READY_FOR_QA_BUILDERC=NO",
                "READY_FOR_UNITY_HANDOFF=NO",
                "MASTER_25600_AUTHORIZED=NO",
            ]
        ),
        encoding="utf-8",
    )
    (DOCS / "WorldMapWave6_SelectedHdLocalRepairReview_Receipt.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8")


def create_proofs(changed_tiles: list[tuple[int, int]]) -> None:
    PROOF.mkdir(parents=True, exist_ok=True)
    side_by_side(
        stitch([1, 2, 3], [46, 47, 48], False, "DEFECT-001 BEFORE: R01..R03 / C46..C48"),
        stitch([1, 2, 3], [46, 47, 48], True, "DEFECT-001 AFTER: patched R02C46..R02C48"),
        PROOF / "DEFECT_001_R02_C46_C48_before_after.png",
    )
    side_by_side(
        stitch([18, 19, 20], [45, 46, 47], False, "DEFECT-002 BEFORE: R18..R20 / C45..C47"),
        stitch([18, 19, 20], [45, 46, 47], True, "DEFECT-002 AFTER: patched R18..R20/C45..C47"),
        PROOF / "DEFECT_002_R18_R20_C45_C47_before_after.png",
    )
    tile_size = 258
    header = 40
    sheet = Image.new("RGB", (tile_size * 4, header + tile_size * len(changed_tiles)), (8, 10, 9))
    draw = ImageDraw.Draw(sheet)
    draw.text((10, 12), "Changed tile contact sheet: baseline left / patched right", fill=(255, 230, 120))
    for index, (row, col) in enumerate(changed_tiles):
        y = header + index * tile_size
        before = Image.open(baseline_tile(row, col)).convert("RGB").resize((tile_size, tile_size), Image.Resampling.BICUBIC)
        after = Image.open(package_tile(row, col)).convert("RGB").resize((tile_size, tile_size), Image.Resampling.BICUBIC)
        sheet.paste(before, (0, y))
        sheet.paste(after, (tile_size, y))
        draw.text((tile_size * 2 + 10, y + 12), tile_name(row, col), fill=(255, 255, 255))
    save_png(sheet, PROOF / "changed_tiles_contact_sheet.png")


def main() -> None:
    STAGE.mkdir(parents=True, exist_ok=True)
    DOCS.mkdir(parents=True, exist_ok=True)
    copy_baseline_package()
    defect_001_tiles = [(2, col) for col in range(46, 49)]
    defect_002_tiles = [(row, col) for row in range(18, 21) for col in range(45, 48)]
    changed_tiles = defect_001_tiles + defect_002_tiles
    for row, col in defect_001_tiles:
        save_png(repair_horizontal_seam(row, col), package_tile(row, col))
    for row, col in defect_002_tiles:
        save_png(repair_mountain_orientation(row, col), package_tile(row, col))
    package_sha = update_manifest(changed_tiles)
    create_proofs(changed_tiles)
    validation = validate_package(changed_tiles, package_sha)
    write_review_files(changed_tiles, validation)
    print(json.dumps({"package": str(PACKAGE), "stage": str(STAGE), "docs": str(DOCS), "validation": validation}, indent=2))


if __name__ == "__main__":
    main()
