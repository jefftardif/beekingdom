from __future__ import annotations

import hashlib
import json
import math
import random
import shutil
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageFilter, ImageFont, ImageStat


ROOT = Path(r"C:\projets\beekingdomgame-master")
SRC = ROOT / "Assets" / "BeeKingdom" / "Playground" / "Resources" / "WorldMapWave6Runtime" / "UIB_ImmenseContinuousMaster50x50_v2i_repair_audit_preview"
STAGE = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging" / "local_repair_selected_hd_candidate_20260717"


def tile_name(row: int, col: int) -> str:
    return f"R{row:02d}C{col:02d}_g2.png"


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest().upper()


def load_tile(row: int, col: int) -> Image.Image:
    path = SRC / tile_name(row, col)
    if not path.exists():
        raise FileNotFoundError(path)
    return Image.open(path).convert("RGBA")


def mosaic(rows: list[int], cols: list[int]) -> tuple[Image.Image, int, int]:
    first = load_tile(rows[0], cols[0])
    w, h = first.size
    out = Image.new("RGBA", (w * len(cols), h * len(rows)))
    for ri, row in enumerate(rows):
        for ci, col in enumerate(cols):
            im = load_tile(row, col)
            if im.size != (w, h):
                raise ValueError(f"Unexpected tile size for {tile_name(row, col)}: {im.size}, expected {(w, h)}")
            out.paste(im, (ci * w, ri * h))
    return out, w, h


def split_and_write(m: Image.Image, rows: list[int], cols: list[int], changed: set[tuple[int, int]], out_dir: Path) -> dict[str, dict[str, str]]:
    out_dir.mkdir(parents=True, exist_ok=True)
    w = m.width // len(cols)
    h = m.height // len(rows)
    records: dict[str, dict[str, str]] = {}
    for ri, row in enumerate(rows):
        for ci, col in enumerate(cols):
            if (row, col) not in changed:
                continue
            name = tile_name(row, col)
            tile = m.crop((ci * w, ri * h, (ci + 1) * w, (ri + 1) * h))
            path = out_dir / name
            tile.save(path)
            records[name] = {"path": str(path), "sha256": sha256(path)}
    return records


def feather_mask(size: tuple[int, int], rect: tuple[int, int, int, int], blur: int) -> Image.Image:
    mask = Image.new("L", size, 0)
    draw = ImageDraw.Draw(mask)
    draw.rounded_rectangle(rect, radius=max(8, blur), fill=255)
    return mask.filter(ImageFilter.GaussianBlur(blur))


def local_texture_repair(region: Image.Image, blur_radius: float, sharpness: float, contrast: float) -> Image.Image:
    soft = region.filter(ImageFilter.GaussianBlur(blur_radius))
    detail = ImageChops.subtract(region, region.filter(ImageFilter.GaussianBlur(2.0)), scale=1.0, offset=128)
    repaired = Image.blend(region, soft, 0.44)
    repaired = ImageChops.overlay(repaired, detail)
    repaired = ImageEnhance.Contrast(repaired).enhance(contrast)
    repaired = ImageEnhance.Sharpness(repaired).enhance(sharpness)
    return repaired


def vertical_transition_repair(region: Image.Image) -> Image.Image:
    w, h = region.size
    # Directional low-pass only on Y: it removes the straight horizontal cut
    # while keeping most of the lateral rock/forest structure intact.
    compressed_h = max(8, h // 28)
    y_smooth = region.resize((w, compressed_h), Image.Resampling.BICUBIC).resize((w, h), Image.Resampling.BICUBIC)
    detail = ImageChops.subtract(region, region.filter(ImageFilter.GaussianBlur(1.8)), scale=1.0, offset=128)
    repaired = Image.blend(region, y_smooth, 0.90)
    repaired = ImageChops.overlay(repaired, detail)
    repaired = ImageEnhance.Contrast(repaired).enhance(1.10)
    repaired = ImageEnhance.Sharpness(repaired).enhance(1.45)
    return repaired


def bridge_horizontal_cut(region: Image.Image, donor: Image.Image) -> Image.Image:
    base = Image.blend(region, donor, 0.58)
    detail_region = ImageChops.subtract(region, region.filter(ImageFilter.GaussianBlur(1.6)), scale=1.0, offset=128)
    detail_donor = ImageChops.subtract(donor, donor.filter(ImageFilter.GaussianBlur(1.4)), scale=1.0, offset=128)
    repaired = ImageChops.overlay(base, Image.blend(detail_region, detail_donor, 0.55))
    repaired = ImageEnhance.Contrast(repaired).enhance(1.08)
    repaired = ImageEnhance.Sharpness(repaired).enhance(1.55)
    return repaired


def irregular_top_edge_mask(width: int, height: int, x_margin: int = 72, seed: int = 5409) -> Image.Image:
    rng = random.Random(seed)
    mask = Image.new("L", (width, height), 0)
    pix = mask.load()
    phases = [rng.uniform(0, math.tau) for _ in range(4)]
    for y in range(height):
        base = max(0.0, 1.0 - y / float(height - 1))
        waviness = (
            0.12 * math.sin(y * 0.041 + phases[0])
            + 0.08 * math.sin(y * 0.097 + phases[1])
            + 0.05 * math.sin(y * 0.181 + phases[2])
        )
        for x in range(width):
            xfade = min(1.0, max(0.0, (x - x_margin) / 80.0), max(0.0, (width - x_margin - x) / 80.0))
            lateral = 0.08 * math.sin(x * 0.017 + phases[3]) + 0.05 * math.sin((x + y) * 0.031)
            value = max(0.0, min(1.0, base + waviness + lateral)) * xfade
            pix[x, y] = int(value * 255)
    return mask.filter(ImageFilter.GaussianBlur(13))


def extend_native_material_across_boundary(above: Image.Image, below: Image.Image) -> Image.Image:
    donor = ImageEnhance.Contrast(above).enhance(1.03)
    donor = ImageEnhance.Sharpness(donor).enhance(1.18)
    base = Image.blend(below, donor, 0.82)
    detail = ImageChops.subtract(donor, donor.filter(ImageFilter.GaussianBlur(1.2)), scale=1.0, offset=128)
    repaired = ImageChops.overlay(base, detail)
    repaired = ImageEnhance.Sharpness(repaired).enhance(1.28)
    return repaired


def repair_defect_001(out_dir: Path) -> dict[str, object]:
    rows = [1, 2, 3]
    cols = [46, 47, 48]
    before, w, h = mosaic(rows, cols)
    after = before.copy()
    seam_y = 2 * h
    strip_h = 256
    above = before.crop((0, seam_y - strip_h, 3 * w, seam_y))
    below_box = (0, seam_y, 3 * w, seam_y + strip_h)
    below = after.crop(below_box)

    # Extend the exact native material that touches the seam downward into R03.
    # The mask starts at the row boundary, then fades irregularly so there is no
    # rectangular read inside the tile.
    repaired = extend_native_material_across_boundary(above, below)
    mask = irregular_top_edge_mask(repaired.width, repaired.height)
    after.paste(repaired, below_box, mask)

    changed = {(2, 46), (2, 47), (2, 48), (3, 46), (3, 47), (3, 48)}
    records = split_and_write(after, rows, cols, changed, out_dir / "patched_tiles")
    return {
        "defect": "DEFECT-001",
        "unity_area": "C54_09",
        "internal_area": "R02C47 neighborhood",
        "rows": rows,
        "cols": cols,
        "tile_size": [w, h],
        "before": before,
        "after": after,
        "changed_tiles": records,
        "repair": "localized horizontal feather blend across R02/R03 boundary, with detail re-injection",
    }


def repair_defect_002(out_dir: Path) -> dict[str, object]:
    rows = [18, 19, 20]
    cols = [45, 46, 47]
    before, w, h = mosaic(rows, cols)
    after = before.copy()

    target = (0, h + 24, 3 * w, h + 392)
    original = after.crop(target)
    lower_material = before.crop((0, 2 * h + 18, 3 * w, 2 * h + 18 + (target[3] - target[1])))
    upper_material = before.crop((0, h + 54, 3 * w, h + 54 + (target[3] - target[1])))

    # Re-orient the incoherent peak band by replacing the downward-looking strip
    # with native terrain material from the same neighborhood, then restore detail.
    base = Image.blend(upper_material, lower_material, 0.58)
    base = Image.blend(original, base, 0.62)
    repaired = local_texture_repair(base, blur_radius=5.0, sharpness=1.75, contrast=1.11)
    mask = feather_mask(original.size, (76, 18, original.width - 76, original.height - 22), 30)
    after.paste(repaired, target, mask)

    changed = {(19, 45), (19, 46), (19, 47)}
    records = split_and_write(after, rows, cols, changed, out_dir / "patched_tiles")
    return {
        "defect": "DEFECT-002",
        "unity_area": "C53_26",
        "internal_area": "R19C46 neighborhood",
        "rows": rows,
        "cols": cols,
        "tile_size": [w, h],
        "before": before,
        "after": after,
        "changed_tiles": records,
        "repair": "localized opaque native-material replacement of inverted mountain/crystal band, feathered inside neighborhood",
    }


def draw_label(draw: ImageDraw.ImageDraw, xy: tuple[int, int], text: str) -> None:
    draw.rectangle((xy[0] - 4, xy[1] - 3, xy[0] + len(text) * 7 + 8, xy[1] + 16), fill=(0, 0, 0, 185))
    draw.text(xy, text, fill=(255, 255, 255, 255))


def contact_sheet(defects: list[dict[str, object]], path: Path) -> None:
    panels = []
    for d in defects:
        before = d["before"].convert("RGB")
        after = d["after"].convert("RGB")
        scale = 0.50
        before_small = before.resize((int(before.width * scale), int(before.height * scale)), Image.Resampling.LANCZOS)
        after_small = after.resize((int(after.width * scale), int(after.height * scale)), Image.Resampling.LANCZOS)
        panel = Image.new("RGB", (before_small.width * 2 + 24, before_small.height + 48), (28, 28, 28))
        panel.paste(before_small, (8, 34))
        panel.paste(after_small, (before_small.width + 16, 34))
        draw = ImageDraw.Draw(panel)
        draw_label(draw, (12, 10), f"{d['defect']} BEFORE")
        draw_label(draw, (before_small.width + 20, 10), f"{d['defect']} AFTER PATCH")
        panels.append(panel)

    width = max(p.width for p in panels)
    height = sum(p.height for p in panels) + 16 * (len(panels) - 1)
    sheet = Image.new("RGB", (width, height), (18, 18, 18))
    y = 0
    for p in panels:
        sheet.paste(p, (0, y))
        y += p.height + 16
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path)


def seam_stress_sheet(defects: list[dict[str, object]], path: Path) -> None:
    strips = []
    for d in defects:
        before = d["before"].convert("RGB")
        after = d["after"].convert("RGB")
        w, h = d["tile_size"]
        if d["defect"] == "DEFECT-001":
            y = 2 * h
            box = (0, y - 96, before.width, y + 96)
        else:
            y = h + 200
            box = (0, y - 144, before.width, y + 144)
        b = before.crop(box)
        a = after.crop(box)
        panel = Image.new("RGB", (b.width * 2 + 24, b.height + 42), (28, 28, 28))
        panel.paste(b, (8, 32))
        panel.paste(a, (b.width + 16, 32))
        draw = ImageDraw.Draw(panel)
        draw_label(draw, (12, 9), f"{d['defect']} STRESS BEFORE")
        draw_label(draw, (b.width + 20, 9), f"{d['defect']} STRESS AFTER")
        strips.append(panel)
    width = max(s.width for s in strips)
    height = sum(s.height for s in strips) + 14 * (len(strips) - 1)
    sheet = Image.new("RGB", (width, height), (18, 18, 18))
    y = 0
    for s in strips:
        sheet.paste(s, (0, y))
        y += s.height + 14
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path)


def edge_exact(tile_a: Image.Image, tile_b: Image.Image) -> bool:
    return ImageChops.difference(tile_a, tile_b).getbbox() is None


def edge_report(patched_dir: Path, changed_names: list[str]) -> dict[str, object]:
    report = {}
    for name in changed_names:
        src = Image.open(SRC / name).convert("RGBA")
        patched = Image.open(patched_dir / name).convert("RGBA")
        w, h = src.size
        edges = {
            "top": edge_exact(src.crop((0, 0, w, 1)), patched.crop((0, 0, w, 1))),
            "bottom": edge_exact(src.crop((0, h - 1, w, h)), patched.crop((0, h - 1, w, h))),
            "left": edge_exact(src.crop((0, 0, 1, h)), patched.crop((0, 0, 1, h))),
            "right": edge_exact(src.crop((w - 1, 0, w, h)), patched.crop((w - 1, 0, w, h))),
        }
        diff = ImageChops.difference(src, patched).convert("L")
        stat = ImageStat.Stat(diff)
        report[name] = {
            "dimension_preserved": list(src.size) == list(patched.size),
            "outer_edges_exact_vs_source": edges,
            "changed_pixel_bbox": diff.getbbox(),
            "mean_delta": round(stat.mean[0], 4),
            "sha256_source": sha256(SRC / name),
            "sha256_patched": sha256(patched_dir / name),
        }
    return report


def copy_source_context() -> None:
    dest = STAGE / "source_tiles_readonly_context"
    dest.mkdir(parents=True, exist_ok=True)
    needed = set()
    for r in [1, 2, 3]:
        for c in [46, 47, 48]:
            needed.add((r, c))
    for r in [18, 19, 20]:
        for c in [45, 46, 47]:
            needed.add((r, c))
    for r, c in sorted(needed):
        src_path = SRC / tile_name(r, c)
        shutil.copy2(src_path, dest / src_path.name)


def main() -> None:
    if not SRC.exists():
        raise FileNotFoundError(SRC)
    (STAGE / "proof").mkdir(parents=True, exist_ok=True)
    (STAGE / "patched_tiles").mkdir(parents=True, exist_ok=True)
    copy_source_context()

    d1 = repair_defect_001(STAGE)
    d2 = repair_defect_002(STAGE)
    defects = [d1, d2]

    contact_path = STAGE / "proof" / "local_repair_before_after_contact_sheet.png"
    stress_path = STAGE / "proof" / "local_repair_seam_stress_sheet.png"
    contact_sheet(defects, contact_path)
    seam_stress_sheet(defects, stress_path)

    patched_dir = STAGE / "patched_tiles"
    changed_names = sorted({name for d in defects for name in d["changed_tiles"].keys()})
    edges = edge_report(patched_dir, changed_names)
    manifest = {
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "source_package": str(SRC),
        "staging": str(STAGE),
        "scope": "local patch only; no full map regeneration; no Unity/runtime modification",
        "defects": [
            {
                "id": d["defect"],
                "unity_area": d["unity_area"],
                "internal_area": d["internal_area"],
                "repair": d["repair"],
                "changed_tiles": d["changed_tiles"],
            }
            for d in defects
        ],
        "proofs": {
            "before_after_contact_sheet": {"path": str(contact_path), "sha256": sha256(contact_path)},
            "seam_stress_sheet": {"path": str(stress_path), "sha256": sha256(stress_path)},
        },
        "edge_compatibility": edges,
    }
    manifest_path = STAGE / "LOCAL_REPAIR_SELECTED_HD_CANDIDATE_MANIFEST.json"
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    receipt = {
        "created_utc": manifest["created_utc"],
        "LOCAL_REPAIR_SELECTED_HD_CANDIDATE_CREATED": "YES",
        "DEFECT_001_R02C47_PATCH_CREATED": "YES",
        "DEFECT_002_R19C46_PATCH_CREATED": "YES",
        "INTERNAL_VISUAL_REVIEW": "FAIL",
        "INTERNAL_VISUAL_REVIEW_REASON": "DEFECT-001 local automated bridge removes part of the hard seam but introduces a visible rectangular repair read; not acceptable as final patch without manual/native source replacement.",
        "PATCHED_TILE_COUNT": len(changed_names),
        "PATCHED_TILES": changed_names,
        "EDGE_COMPATIBLE_OUTER_BORDERS": "YES" if all(all(v for v in rec["outer_edges_exact_vs_source"].values()) for rec in edges.values()) else "REVIEW",
        "NATIVE_DIMENSIONS_PRESERVED": "YES" if all(rec["dimension_preserved"] for rec in edges.values()) else "NO",
        "FULL_MAP_REGENERATED": "NO",
        "UNITY_RUNTIME_MODIFIED": "NO",
        "READY_FOR_LOCAL_UNITY_RETEST": "NO",
        "READY_FOR_QA_BUILDERC": "NO",
        "READY_FOR_UNITY_HANDOFF": "NO",
        "MASTER_25600_AUTHORIZED": "NO",
        "manifest": str(manifest_path),
        "proof_contact_sheet": str(contact_path),
        "proof_seam_stress_sheet": str(stress_path),
    }
    receipt_path = STAGE / "LOCAL_REPAIR_SELECTED_HD_CANDIDATE_RECEIPT.json"
    receipt_path.write_text(json.dumps(receipt, indent=2), encoding="utf-8")

    review_path = STAGE / "LOCAL_REPAIR_SELECTED_HD_CANDIDATE_REVIEW.md"
    review_path.write_text(
        "# Local Repair Selected HD Candidate Review\n\n"
        "STATUS=LOCAL_PATCH_ATTEMPT_VISUAL_FAIL\n\n"
        "Scope: local patch package only. No full 50x50 regeneration, no Unity/runtime write, no QA/Builder-C request.\n\n"
        "## Gates\n\n"
        "- DEFECT_001_R02C47_PATCH_CREATED=YES\n"
        "- DEFECT_002_R19C46_PATCH_CREATED=YES\n"
        "- INTERNAL_VISUAL_REVIEW=FAIL\n"
        "- INTERNAL_VISUAL_REVIEW_REASON=DEFECT-001 automated bridge leaves a visible rectangular repair read; DEFECT-002 is improved but the package cannot pass while DEFECT-001 remains visible.\n"
        f"- PATCHED_TILE_COUNT={len(changed_names)}\n"
        f"- EDGE_COMPATIBLE_OUTER_BORDERS={receipt['EDGE_COMPATIBLE_OUTER_BORDERS']}\n"
        f"- NATIVE_DIMENSIONS_PRESERVED={receipt['NATIVE_DIMENSIONS_PRESERVED']}\n"
        "- FULL_MAP_REGENERATED=NO\n"
        "- UNITY_RUNTIME_MODIFIED=NO\n"
        "- READY_FOR_LOCAL_UNITY_RETEST=NO\n"
        "- READY_FOR_QA_BUILDERC=NO\n"
        "- READY_FOR_UNITY_HANDOFF=NO\n"
        "- MASTER_25600_AUTHORIZED=NO\n\n"
        "## Next Minimal Action\n\n"
        "Use a native/manual source replacement for the DEFECT-001 R02/R03 transition band, or select a sharper local donor with proven seam continuity. Do not retest Unity with this automated patch attempt as a candidate.\n\n"
        "## Proofs\n\n"
        f"- Before/after contact sheet: `{contact_path}`\n"
        f"- Seam stress sheet: `{stress_path}`\n"
        f"- Manifest: `{manifest_path}`\n"
        f"- Receipt: `{receipt_path}`\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
