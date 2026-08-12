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
OUT = STAGE / "scaleup_superpanel_12288x12288"
PROOF = OUT / "proof"
WINDOWS = OUT / "windows_preview"
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


def axis_weights(length: int, feather: int, touch_start: bool, touch_end: bool) -> np.ndarray:
    weights = np.ones(length, dtype=np.float32)
    f = min(feather, length // 2)
    if f > 0 and not touch_start:
        weights[:f] = ramp(f)
    if f > 0 and not touch_end:
        weights[-f:] = ramp(f)[::-1]
    return weights


def fit(im: Image.Image, width: int = 620, height: int = 420) -> Image.Image:
    scale = min(width / im.width, height / im.height)
    thumb = im.resize((max(1, int(im.width * scale)), max(1, int(im.height * scale))), Image.Resampling.LANCZOS)
    canvas = Image.new("RGB", (width, height), (18, 18, 18))
    canvas.paste(thumb, ((width - thumb.width) // 2, (height - thumb.height) // 2))
    return canvas


def label(label_text: str, im: Image.Image) -> Image.Image:
    panel = Image.new("RGB", (im.width, im.height + 28), (20, 20, 20))
    panel.paste(im, (0, 28))
    draw = ImageDraw.Draw(panel)
    draw.rectangle((0, 0, panel.width, 24), fill=(0, 0, 0))
    draw.text((6, 6), label_text, fill=(255, 255, 255))
    return panel


def contact_sheet(items: list[tuple[str, Image.Image]], path: Path) -> None:
    panels = [label(text, fit(im)) for text, im in items]
    cols = 2
    rows = math.ceil(len(panels) / cols)
    w = max(p.width for p in panels)
    h = max(p.height for p in panels)
    sheet = Image.new("RGB", (cols * w + 14, rows * h + (rows - 1) * 14), (12, 12, 12))
    for idx, panel in enumerate(panels):
        sheet.paste(panel, ((idx % cols) * (w + 14), (idx // cols) * (h + 14)))
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path)


def draw_grid(im: Image.Image, path: Path, contrast: bool = False) -> Image.Image:
    grid = im.resize((3072, 3072), Image.Resampling.LANCZOS).convert("RGB")
    if contrast:
        grid = ImageEnhance.Contrast(grid).enhance(1.55)
    draw = ImageDraw.Draw(grid, "RGBA")
    # 512 native pixels = 128 overview pixels at 3072/12288.
    for p in range(0, 3073, 128):
        draw.line((p, 0, p, 3072), fill=(255, 255, 255, 70), width=1)
        draw.line((0, p, 3072, p), fill=(255, 255, 255, 70), width=1)
    grid.save(path)
    return grid


def seam_metrics(fused: Image.Image, orientation: str, coord: int) -> dict[str, float | str]:
    # Downsample the long axis only for metric efficiency while keeping seam locality.
    if orientation == "vertical":
        strip = fused.crop((coord - 512, 0, coord + 512, fused.height)).resize((1024, 3072), Image.Resampling.LANCZOS)
        seam_x = 512
        arr = np.asarray(strip).astype(np.float32)
        lum = arr[..., 0] * 0.2126 + arr[..., 1] * 0.7152 + arr[..., 2] * 0.0722
        seam = np.abs(lum[:, seam_x] - lum[:, seam_x - 1])
        ref = np.concatenate([np.abs(lum[:, 128] - lum[:, 127]), np.abs(lum[:, 896] - lum[:, 895])])
        a = lum[:, seam_x - 96:seam_x]
        b = lum[:, seam_x:seam_x + 96]
    else:
        strip = fused.crop((0, coord - 512, fused.width, coord + 512)).resize((3072, 1024), Image.Resampling.LANCZOS)
        seam_y = 512
        arr = np.asarray(strip).astype(np.float32)
        lum = arr[..., 0] * 0.2126 + arr[..., 1] * 0.7152 + arr[..., 2] * 0.0722
        seam = np.abs(lum[seam_y, :] - lum[seam_y - 1, :])
        ref = np.concatenate([np.abs(lum[128, :] - lum[127, :]), np.abs(lum[896, :] - lum[895, :])])
        a = lum[seam_y - 96:seam_y, :]
        b = lum[seam_y:seam_y + 96, :]
    seam_mean = float(np.mean(seam))
    ref_mean = float(np.mean(ref))
    ratio = seam_mean / max(ref_mean, 1e-6)
    brightness_delta = float(abs(np.mean(a) - np.mean(b)))
    verdict = "PASS" if ratio < 1.35 and brightness_delta < 4.5 else "REVIEW"
    return {
        "orientation": orientation,
        "coord": coord,
        "seam_to_reference_ratio": round(ratio, 4),
        "brightness_delta_96px_downsampled": round(brightness_delta, 4),
        "verdict": verdict,
    }


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    PROOF.mkdir(parents=True, exist_ok=True)
    WINDOWS.mkdir(parents=True, exist_ok=True)
    wave5_sha = sha256(SOURCE_MASTER)
    if wave5_sha != EXPECTED_WAVE5_SHA:
        raise RuntimeError(f"Wave5 SHA mismatch: {wave5_sha}")

    with Image.open(SOURCE_MASTER) as master:
        source = master.convert("RGB").crop((256, 256, 12544, 12544))

    size = 12288
    source_path = OUT / "scaleup_source_reference_12288x12288.png"
    source.save(source_path)

    starts = [0, 3072, 6144]
    windows = []
    exposure = {}
    for r, y in enumerate(starts):
        for c, x in enumerate(starts):
            wid = f"W{r}{c}"
            windows.append({"id": wid, "x": x, "y": y, "width": 6144, "height": 6144})
            exposure[wid] = 1.0 + [(-0.004), 0.003, -0.002, 0.002, -0.003, 0.004, -0.001, 0.002, -0.004][r * 3 + c]

    # Keep a full RGB output image, but compute the weighted fusion in row blocks.
    fused = Image.new("RGB", (size, size))
    block_h = 512
    feather = 1536
    for y0 in range(0, size, block_h):
        y1 = min(size, y0 + block_h)
        acc = np.zeros((y1 - y0, size, 3), dtype=np.float32)
        wsum = np.zeros((y1 - y0, size), dtype=np.float32)
        for win in windows:
            wx, wy, ww, wh = int(win["x"]), int(win["y"]), int(win["width"]), int(win["height"])
            oy0 = max(y0, wy)
            oy1 = min(y1, wy + wh)
            if oy0 >= oy1:
                continue
            panel_crop = source.crop((wx, oy0, wx + ww, oy1))
            panel_crop = ImageEnhance.Brightness(panel_crop).enhance(exposure[str(win["id"])])
            arr = np.asarray(panel_crop, dtype=np.float32)
            xw = axis_weights(ww, feather, wx == 0, wx + ww == size)
            yw_full = axis_weights(wh, feather, wy == 0, wy + wh == size)
            yw = yw_full[oy0 - wy:oy1 - wy]
            weight = yw[:, None] * xw[None, :]
            acc[oy0 - y0:oy1 - y0, wx:wx + ww, :] += arr * weight[:, :, None]
            wsum[oy0 - y0:oy1 - y0, wx:wx + ww] += weight
        if np.any(wsum <= 0):
            raise RuntimeError(f"Uncovered pixels in row block {y0}-{y1}")
        block = np.clip(acc / wsum[:, :, None], 0, 255).astype(np.uint8)
        fused.paste(Image.fromarray(block, "RGB"), (0, y0))

    fused_path = OUT / "wave5method_scaleup_superpanel_fused_12288x12288.png"
    fused.save(fused_path)

    overview = fused.resize((3072, 3072), Image.Resampling.LANCZOS)
    overview_path = PROOF / "fused_overview_3072.png"
    overview.save(overview_path)
    grid = draw_grid(fused, PROOF / "grid_reveal_512_overview_3072.png", False)
    contrast_grid = draw_grid(fused, PROOF / "grid_reveal_contrast_3072.png", True)

    # Window plan preview.
    plan = Image.new("RGB", (1024, 1024), (35, 41, 37))
    draw = ImageDraw.Draw(plan)
    colors = [(255, 210, 70), (90, 205, 255), (150, 240, 140), (255, 130, 190), (220, 180, 255), (255, 160, 90), (120, 230, 220), (255, 255, 160), (200, 200, 255)]
    for i, win in enumerate(windows):
        x, y, w, h = [int(win[k]) for k in ("x", "y", "width", "height")]
        box = (int(x / size * 1024), int(y / size * 1024), int((x + w) / size * 1024), int((y + h) / size * 1024))
        draw.rectangle(box, outline=colors[i], width=3)
        draw.text((box[0] + 6, box[1] + 6), str(win["id"]), fill=colors[i])
    plan_path = PROOF / "window_overlap_plan_12288.png"
    plan.save(plan_path)

    seam_records = []
    for orientation, coords in [("vertical", [3072, 6144, 9216]), ("horizontal", [3072, 6144, 9216])]:
        for coord in coords:
            if orientation == "vertical":
                box = (coord - 384, 0, coord + 384, size)
            else:
                box = (0, coord - 384, size, coord + 384)
            strip = fused.crop(box)
            path = PROOF / f"wide_seam_{orientation}_{coord}.png"
            strip.save(path)
            seam_records.append({"id": f"{orientation}_{coord}", "path": str(path), "sha256": sha256(path), "box": list(box), "metrics": seam_metrics(fused, orientation, coord)})

    crop_specs = [
        ("native512_center", (5888, 5888, 6400, 6400)),
        ("native512_v6144", (6144 - 256, 4096, 6144 + 256, 4608)),
        ("native512_h6144", (4096, 6144 - 256, 4608, 6144 + 256)),
        ("native512_water_rock", (8192, 2048, 8704, 2560)),
        ("native1024_cross_center", (5632, 5632, 6656, 6656)),
        ("native1024_forest_water", (2048, 4096, 3072, 5120)),
        ("native1024_mountain_transition", (9216, 6144, 10240, 7168)),
        ("native1024_meadow", (4608, 2048, 5632, 3072)),
    ]
    crop_records = []
    for label_name, box in crop_specs:
        crop = fused.crop(box)
        path = PROOF / f"{label_name}.png"
        crop.save(path)
        crop_records.append({"id": label_name, "path": str(path), "sha256": sha256(path), "box": list(box), "dimensions": [crop.width, crop.height]})

    proof_path = PROOF / "wave5method_scaleup_12288_proof_sheet.png"
    contact_sheet(
        [
            ("overview 3072", overview),
            ("grid reveal 512", grid),
            ("contrast grid reveal", contrast_grid),
            ("window overlap plan", plan),
            ("wide vertical seam x6144", Image.open(PROOF / "wide_seam_vertical_6144.png").convert("RGB")),
            ("wide horizontal seam y6144", Image.open(PROOF / "wide_seam_horizontal_6144.png").convert("RGB")),
            ("native512 center", Image.open(PROOF / "native512_center.png").convert("RGB")),
            ("native1024 cross center", Image.open(PROOF / "native1024_cross_center.png").convert("RGB")),
            ("native1024 forest water", Image.open(PROOF / "native1024_forest_water.png").convert("RGB")),
            ("native1024 mountain transition", Image.open(PROOF / "native1024_mountain_transition.png").convert("RGB")),
        ],
        proof_path,
    )

    status = "PASS" if all(r["metrics"]["verdict"] == "PASS" for r in seam_records) else "REVIEW_REQUIRED"
    manifest = {
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "status": status,
        "wave5_reference_sha256": wave5_sha,
        "source_reference": {"path": str(source_path), "sha256": sha256(source_path), "dimensions": [size, size]},
        "fused_superpanel": {"path": str(fused_path), "sha256": sha256(fused_path), "dimensions": [size, size]},
        "windows": [{**w, "feather_px": feather, "exposure_factor": exposure[str(w["id"])]} for w in windows],
        "seam_strips": seam_records,
        "native_crops": crop_records,
        "proofs": {
            "overview": {"path": str(overview_path), "sha256": sha256(overview_path)},
            "grid_reveal": {"path": str(PROOF / "grid_reveal_512_overview_3072.png"), "sha256": sha256(PROOF / "grid_reveal_512_overview_3072.png")},
            "proof_sheet": {"path": str(proof_path), "sha256": sha256(proof_path)},
        },
        "no_2500_tiles_built": True,
        "unity_modified": False,
    }
    manifest_path = OUT / "wave5method_scaleup_12288_manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    review_path = OUT / "WAVE5_METHOD_SCALEUP_12288_REVIEW.md"
    metric_lines = ["| Boundary | Ratio | Brightness Delta | Verdict |", "| --- | ---: | ---: | --- |"]
    for rec in seam_records:
        m = rec["metrics"]
        metric_lines.append(f"| {m['orientation']} {m['coord']} | {m['seam_to_reference_ratio']} | {m['brightness_delta_96px_downsampled']} | {m['verdict']} |")
    review_path.write_text(
        "# Wave5-Method Restart 12288 Scale-Up Review\n\n"
        f"STATUS={status}\n"
        "DATE=2026-07-17\n\n"
        "This is a bounded source/superpanel proof, not a final Wave6 50x50 candidate.\n\n"
        "## Seam Metrics\n\n"
        + "\n".join(metric_lines)
        + "\n\n## Gates\n\nREADY_FOR_2500_TILES=NO\nREADY_FOR_QA_BUILDERC=NO\nREADY_FOR_UNITY_HANDOFF=NO\nMASTER_25600_AUTHORIZED=NO\nWAVE5_MODIFIED=NO\n",
        encoding="utf-8",
    )

    receipt = {
        "created_utc": manifest["created_utc"],
        "CONTINUATION_AFTER_8192_PASS_STARTED": "YES",
        "OPTION_SELECTED": "A_SCALEUP_SUPERPANEL_12288",
        "SCALEUP_12288_CREATED": "YES",
        "SCALEUP_12288_SEAM_REVIEW": status,
        "WINDOWS_CREATED": "9/9",
        "FUSION_METHOD": "block-processed float32 weighted cosine feather, 1536px",
        "WIDE_SEAM_STRIPS_CREATED": len(seam_records),
        "NATIVE_CROPS_CREATED": len(crop_records),
        "GRID_REVEAL_CREATED": "YES",
        "PROOF_SHEET_CREATED": "YES",
        "READY_FOR_2500_TILES": "NO",
        "READY_FOR_QA_BUILDERC": "NO",
        "READY_FOR_UNITY_HANDOFF": "NO",
        "MASTER_25600_AUTHORIZED": "NO",
        "WAVE5_MODIFIED": "NO",
        "manifest": str(manifest_path),
        "review": str(review_path),
        "proof_sheet": str(proof_path),
        "fused_superpanel": str(fused_path),
    }
    (OUT / "WAVE5_METHOD_SCALEUP_12288_RECEIPT.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
