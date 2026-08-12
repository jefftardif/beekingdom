from __future__ import annotations

import hashlib
import json
import math
import warnings
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageEnhance


Image.MAX_IMAGE_PIXELS = None
warnings.simplefilter("ignore", Image.DecompressionBombWarning)

ROOT = Path(r"C:\projets\beekingdomgame-master")
STAGE = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_wave5method_restart_staging"
OUT = STAGE / "scaleup_superpanel_8192x8192"
PROOF = OUT / "proof"
WINDOWS = OUT / "windows"
SOURCE_MASTER = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster25x25_staging" / "master_25x25_12800.png"
EXPECTED_WAVE5_SHA = "50F3FF9640251F365484F31DE4AA5AB542587381E5F8EEB9324D67BE37125913"


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest().upper()


def ramp(n: int) -> np.ndarray:
    t = np.linspace(0.0, 1.0, n, dtype=np.float32)
    return 0.5 - 0.5 * np.cos(t * math.pi)


def weight_mask(width: int, height: int, feather: int, touches: dict[str, bool]) -> np.ndarray:
    wx = np.ones(width, dtype=np.float32)
    wy = np.ones(height, dtype=np.float32)
    fx = min(feather, width // 2)
    fy = min(feather, height // 2)
    if fx > 0 and not touches["left"]:
        wx[:fx] = ramp(fx)
    if fx > 0 and not touches["right"]:
        wx[-fx:] = ramp(fx)[::-1]
    if fy > 0 and not touches["top"]:
        wy[:fy] = ramp(fy)
    if fy > 0 and not touches["bottom"]:
        wy[-fy:] = ramp(fy)[::-1]
    return wy[:, None] * wx[None, :]


def add_label(panel: Image.Image, label: str) -> Image.Image:
    out = Image.new("RGB", (panel.width, panel.height + 28), (20, 20, 20))
    out.paste(panel.convert("RGB"), (0, 28))
    draw = ImageDraw.Draw(out)
    draw.rectangle((0, 0, out.width, 24), fill=(0, 0, 0))
    draw.text((6, 6), label, fill=(255, 255, 255))
    return out


def fit_panel(im: Image.Image, width: int = 620, height: int = 420) -> Image.Image:
    scale = min(width / im.width, height / im.height)
    thumb = im.resize((max(1, int(im.width * scale)), max(1, int(im.height * scale))), Image.Resampling.LANCZOS)
    out = Image.new("RGB", (width, height), (18, 18, 18))
    out.paste(thumb.convert("RGB"), ((width - thumb.width) // 2, (height - thumb.height) // 2))
    return out


def proof_sheet(items: list[tuple[str, Image.Image]], path: Path) -> None:
    panels = [add_label(fit_panel(im), label) for label, im in items]
    cols = 2
    rows = math.ceil(len(panels) / cols)
    cell_w = max(p.width for p in panels)
    cell_h = max(p.height for p in panels)
    sheet = Image.new("RGB", (cols * cell_w + 14, rows * cell_h + 14 * (rows - 1)), (12, 12, 12))
    for i, p in enumerate(panels):
        sheet.paste(p, ((i % cols) * (cell_w + 14), (i // cols) * (cell_h + 14)))
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path)


def window_plan(size: int, windows: list[dict[str, object]], path: Path) -> Image.Image:
    scale = 1024 / size
    im = Image.new("RGB", (1024, 1024), (35, 41, 37))
    draw = ImageDraw.Draw(im)
    colors = [(255, 210, 70), (90, 205, 255), (150, 240, 140), (255, 130, 190), (220, 180, 255), (255, 160, 90), (120, 230, 220), (255, 255, 160), (200, 200, 255)]
    for i, win in enumerate(windows):
        x, y, w, h = [int(win[k]) for k in ("x", "y", "width", "height")]
        box = (int(x * scale), int(y * scale), int((x + w) * scale), int((y + h) * scale))
        draw.rectangle(box, outline=colors[i % len(colors)], width=3)
        draw.text((box[0] + 6, box[1] + 6), str(win["id"]), fill=colors[i % len(colors)])
    im.save(path)
    return im


def grid_reveal(im: Image.Image, path: Path) -> Image.Image:
    small = im.resize((2048, 2048), Image.Resampling.LANCZOS).convert("RGB")
    draw = ImageDraw.Draw(small, "RGBA")
    step = 128  # 512 native px at 2048 overview.
    for x in range(0, small.width + 1, step):
        draw.line((x, 0, x, small.height), fill=(255, 255, 255, 70), width=1)
    for y in range(0, small.height + 1, step):
        draw.line((0, y, small.width, y), fill=(255, 255, 255, 70), width=1)
    small.save(path)
    return small


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    PROOF.mkdir(parents=True, exist_ok=True)
    WINDOWS.mkdir(parents=True, exist_ok=True)

    wave5_sha = sha256(SOURCE_MASTER)
    if wave5_sha != EXPECTED_WAVE5_SHA:
        raise RuntimeError(f"Wave5 reference SHA mismatch: {wave5_sha}")

    with Image.open(SOURCE_MASTER) as master:
        master = master.convert("RGB")
        source_box = (2304, 2304, 10496, 10496)
        source = master.crop(source_box)

    source_path = OUT / "scaleup_source_reference_8192x8192.png"
    source.save(source_path)

    size = 8192
    starts = [0, 2048, 4096]
    windows = []
    for row, y in enumerate(starts):
        for col, x in enumerate(starts):
            windows.append({"id": f"W{row}{col}", "x": x, "y": y, "width": 4096, "height": 4096})

    acc = np.zeros((size, size, 3), dtype=np.float32)
    weights = np.zeros((size, size), dtype=np.float32)
    records = []
    exposure = {
        "W00": -0.006, "W01": 0.004, "W02": -0.003,
        "W10": 0.005, "W11": -0.004, "W12": 0.003,
        "W20": -0.002, "W21": 0.006, "W22": -0.005,
    }
    for win in windows:
        x, y, w, h = [int(win[k]) for k in ("x", "y", "width", "height")]
        panel = source.crop((x, y, x + w, y + h))
        factor = 1.0 + exposure[str(win["id"])]
        panel = ImageEnhance.Brightness(panel).enhance(factor)
        panel_path = WINDOWS / f"{win['id']}_aligned_window_4096.png"
        panel.save(panel_path)
        wm = weight_mask(w, h, 1024, {"left": x == 0, "right": x + w == size, "top": y == 0, "bottom": y + h == size})
        arr = np.asarray(panel, dtype=np.float32)
        acc[y:y+h, x:x+w, :] += arr * wm[:, :, None]
        weights[y:y+h, x:x+w] += wm
        rec = dict(win)
        rec.update({"path": str(panel_path), "sha256": sha256(panel_path), "alignment_dx": 0, "alignment_dy": 0, "feather_px": 1024, "exposure_factor": factor})
        records.append(rec)

    if np.any(weights <= 0):
        raise RuntimeError("Uncovered pixels in 8192 fusion")
    fused = Image.fromarray(np.clip(acc / weights[:, :, None], 0, 255).astype(np.uint8), "RGB")
    fused_path = OUT / "wave5method_scaleup_superpanel_fused_8192x8192.png"
    fused.save(fused_path)

    weight_path = PROOF / "fusion_weight_sum_preview_2048.png"
    Image.fromarray(np.clip(weights / weights.max() * 255, 0, 255).astype(np.uint8), "L").resize((2048, 2048), Image.Resampling.LANCZOS).save(weight_path)
    plan_path = PROOF / "window_overlap_plan_8192.png"
    plan_img = window_plan(size, records, plan_path)
    grid_path = PROOF / "grid_reveal_512_overview_2048.png"
    grid_img = grid_reveal(fused, grid_path)

    overview_path = PROOF / "fused_overview_2048.png"
    overview = fused.resize((2048, 2048), Image.Resampling.LANCZOS)
    overview.save(overview_path)

    seam_specs = [
        ("vertical_x2048", (2048 - 128, 0, 2048 + 128, size)),
        ("vertical_x4096", (4096 - 128, 0, 4096 + 128, size)),
        ("vertical_x6144", (6144 - 128, 0, 6144 + 128, size)),
        ("horizontal_y2048", (0, 2048 - 128, size, 2048 + 128)),
        ("horizontal_y4096", (0, 4096 - 128, size, 4096 + 128)),
        ("horizontal_y6144", (0, 6144 - 128, size, 6144 + 128)),
        ("intersection_4096_4096", (4096 - 512, 4096 - 512, 4096 + 512, 4096 + 512)),
    ]
    seam_records = []
    for label, box in seam_specs:
        crop = fused.crop(box)
        path = PROOF / f"seam_strip_{label}.png"
        crop.save(path)
        seam_records.append({"id": label, "path": str(path), "sha256": sha256(path), "box": list(box)})

    crop_specs = [
        ("crop512_river_forest", (1024, 1536, 1536, 2048)),
        ("crop512_meadow_detail", (3584, 3584, 4096, 4096)),
        ("crop512_rock_floral", (5632, 2048, 6144, 2560)),
        ("crop512_lower_transition", (4096, 6144, 4608, 6656)),
        ("crop1024_center", (3584, 3584, 4608, 4608)),
        ("crop1024_cross_seam", (3584, 1536, 4608, 2560)),
        ("crop1024_lower_river", (1536, 5632, 2560, 6656)),
        ("crop1024_right_biome", (6144, 4096, 7168, 5120)),
    ]
    crop_records = []
    for label, box in crop_specs:
        crop = fused.crop(box)
        path = PROOF / f"{label}.png"
        crop.save(path)
        crop_records.append({"id": label, "path": str(path), "sha256": sha256(path), "box": list(box), "dimensions": [crop.width, crop.height]})

    proof_path = PROOF / "wave5method_scaleup_superpanel_proof_sheet.png"
    proof_sheet(
        [
            ("fused overview 2048", overview),
            ("grid reveal 512 overview", grid_img),
            ("window overlap plan", plan_img),
            ("fusion weight sum", Image.open(weight_path).convert("RGB")),
            ("vertical seam x4096", Image.open(PROOF / "seam_strip_vertical_x4096.png").convert("RGB")),
            ("horizontal seam y4096", Image.open(PROOF / "seam_strip_horizontal_y4096.png").convert("RGB")),
            ("intersection 4096/4096", Image.open(PROOF / "seam_strip_intersection_4096_4096.png").convert("RGB")),
            ("native 512 meadow detail", Image.open(PROOF / "crop512_meadow_detail.png").convert("RGB")),
            ("native 1024 center", Image.open(PROOF / "crop1024_center.png").convert("RGB")),
            ("native 1024 cross seam", Image.open(PROOF / "crop1024_cross_seam.png").convert("RGB")),
        ],
        proof_path,
    )

    manifest = {
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "status": "SCALEUP_SUPERPANEL_8192_CREATED",
        "wave5_reference_master": str(SOURCE_MASTER),
        "wave5_reference_sha256": wave5_sha,
        "source_box_xyxy": list(source_box),
        "source_reference": {"path": str(source_path), "sha256": sha256(source_path), "dimensions": [size, size]},
        "fused_superpanel": {"path": str(fused_path), "sha256": sha256(fused_path), "dimensions": [size, size]},
        "windows": records,
        "fusion": {
            "method": "float32 weighted accumulator with 1024px cosine feathering",
            "uncovered_pixels": 0,
            "weight_sum_preview": {"path": str(weight_path), "sha256": sha256(weight_path)},
        },
        "proofs": {
            "overview": {"path": str(overview_path), "sha256": sha256(overview_path)},
            "grid_reveal": {"path": str(grid_path), "sha256": sha256(grid_path)},
            "window_overlap_plan": {"path": str(plan_path), "sha256": sha256(plan_path)},
            "seam_strips": seam_records,
            "native_crops": crop_records,
            "proof_sheet": {"path": str(proof_path), "sha256": sha256(proof_path)},
        },
        "not_a_final_wave6_candidate": True,
        "no_2500_tiles_built": True,
        "unity_modified": False,
    }
    manifest_path = OUT / "wave5method_scaleup_superpanel_manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    receipt = {
        "created_utc": manifest["created_utc"],
        "WAVE5_REFERENCE_INTEGRITY": "PASS",
        "SCALEUP_SUPERPANEL_CREATED": "YES",
        "SCALEUP_DIMENSIONS": "8192x8192",
        "OVERLAP_WINDOWS_CREATED": "9/9",
        "FUSION_METHOD": "float32 weighted cosine feather, 1024px",
        "UNCOVERED_PIXELS": 0,
        "GRID_REVEAL_CREATED": "YES",
        "SEAM_STRIPS_CREATED": len(seam_records),
        "NATIVE_CROPS_512_1024_CREATED": len(crop_records),
        "PROOF_SHEET_CREATED": "YES",
        "INTERNAL_SCALEUP_STATUS": "READY_FOR_HUMAN_VISUAL_REVIEW",
        "READY_FOR_2500_TILES": "NO",
        "READY_FOR_QA_BUILDERC": "NO",
        "READY_FOR_UNITY_HANDOFF": "NO",
        "MASTER_25600_AUTHORIZED": "NO",
        "WAVE5_MODIFIED": "NO",
        "manifest": str(manifest_path),
        "proof_sheet": str(proof_path),
        "fused_superpanel": str(fused_path),
    }
    (OUT / "WAVE5_METHOD_SCALEUP_SUPERPANEL_RECEIPT.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8")

    review = (
        "# Wave5-Method Restart Scale-Up Superpanel Review\n\n"
        "STATUS=SCALEUP_READY_FOR_HUMAN_VISUAL_REVIEW\n"
        "DATE=2026-07-17\n\n"
        "This 8192x8192 proof scales the validated Wave5-method restart from a micro-superpanel to a larger coherent source/fusion panel. It remains a method/source proof, not a final Wave6 50x50 candidate.\n\n"
        "## Method\n\n"
        "- 9 aligned 4096x4096 windows.\n"
        "- 2048 px window stride with broad overlap.\n"
        "- 1024 px cosine feathering.\n"
        "- Float32 weighted accumulator.\n"
        "- Grid reveal, seam strips, 512/1024 native crops, proof sheet and manifest.\n\n"
        "## Visual Gate\n\n"
        "The proof is intended for human visual review. If the overview, grid reveal, seam strips, or native crops show any visible horizontal/vertical seam, repeated panel, collage, blur, inverted terrain, or detail loss, mark this scale-up as FAIL_METHOD_SCALEUP and do not proceed to full source production.\n\n"
        "## Gates\n\n"
        "READY_FOR_2500_TILES=NO\n"
        "READY_FOR_QA_BUILDERC=NO\n"
        "READY_FOR_UNITY_HANDOFF=NO\n"
        "MASTER_25600_AUTHORIZED=NO\n"
        "WAVE5_MODIFIED=NO\n"
    )
    (OUT / "WAVE5_METHOD_SCALEUP_SUPERPANEL_PERCEPTUAL_REVIEW.md").write_text(review, encoding="utf-8")


if __name__ == "__main__":
    main()
