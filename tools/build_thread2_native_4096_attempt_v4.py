from __future__ import annotations

import hashlib
import json
import shutil
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageEnhance


ROOT = Path(r"C:\projets\beekingdomgame-master")
GENERATED = Path(r"C:\Users\Utilisateur\.codex\generated_images\019f6854-0251-7840-8022-48c46c06c55a\call_RQWtBROlq2KElLizDp3u4fBc.png")
STAGE = ROOT / r"artifacts\UIB_ImmenseContinuousMaster50x50_premium_v4_phase2_thread2_staging\thread2_native_4096_attempt_v4"
DOCS = ROOT / r"Docs\BuilderA\WorldMapWave6_50x50_Wave5MethodRestart"
SOURCE_DIR = STAGE / "source"
PANELS = STAGE / "overlap_panels"
FUSION = STAGE / "fusion"
SEAMS = STAGE / "seam_strips"
CROPS = STAGE / "native_crops"
PROOF = STAGE / "proof"


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest().upper()


def save(img: Image.Image, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    img.save(path, optimize=True)


def label(draw: ImageDraw.ImageDraw, xy: tuple[int, int], text: str) -> None:
    x, y = xy
    draw.rectangle((x, y, x + min(620, len(text) * 7 + 12), y + 22), fill=(4, 7, 6))
    draw.text((x + 6, y + 5), text[:88], fill=(255, 230, 120))


def weight(size: int, ramp: int) -> np.ndarray:
    axis = np.ones(size, dtype=np.float32)
    values = np.linspace(0.05, 1.0, ramp, dtype=np.float32)
    axis[:ramp] = values
    axis[-ramp:] = values[::-1]
    return np.outer(axis, axis)[:, :, None]


def grid_reveal(src: Image.Image) -> Image.Image:
    out = src.copy()
    draw = ImageDraw.Draw(out)
    width, height = out.size
    for step, color, line_width in ((512, (255, 220, 0), 2), (1024, (80, 240, 255), 3)):
        x = 0
        while x < width:
            draw.line((x, 0, x, height), fill=color, width=line_width)
            x += step
        y = 0
        while y < height:
            draw.line((0, y, width, y), fill=color, width=line_width)
            y += step
    label(draw, (10, 10), "native 4096 attempt grid reveal 512/1024")
    return out


def proof_sheet(source: Image.Image, fused: Image.Image, grid: Image.Image, seams: list[Path], crops: list[Path], verdict: str) -> Image.Image:
    canvas = Image.new("RGB", (2200, 1700), (8, 10, 9))
    draw = ImageDraw.Draw(canvas)
    draw.text((18, 16), "Thread2 V4 native 4096 attempt - generated coherent source, measured output, harness proof", fill=(255, 230, 120))
    for idx, (title, img) in enumerate((("GENERATED SOURCE", source), ("FUSED LOCK", fused), ("GRID REVEAL", grid))):
        small = img.resize((540, 540), Image.Resampling.LANCZOS)
        x = 20 + idx * 580
        canvas.paste(small, (x, 55))
        draw.rectangle((x, 55, x + 540, 595), outline=(255, 220, 0) if idx != 1 else (80, 240, 255), width=2)
        label(draw, (x + 8, 64), title)
    wprev = Image.open(FUSION / "fusion_weight_sum_preview.png").convert("RGB").resize((360, 360), Image.Resampling.LANCZOS)
    canvas.paste(wprev, (1780, 135))
    draw.rectangle((1780, 135, 2140, 495), outline=(255, 220, 0), width=2)
    label(draw, (1790, 145), "WEIGHT SUM")

    draw.text((20, 640), "Seam strips and corners", fill=(255, 230, 120))
    for i, path in enumerate(seams[:10]):
        img = Image.open(path).convert("RGB")
        if "vertical" in path.name:
            img = img.resize((120, 420), Image.Resampling.LANCZOS)
        elif "horizontal" in path.name:
            img = img.resize((420, 120), Image.Resampling.LANCZOS)
        else:
            img = img.resize((240, 240), Image.Resampling.LANCZOS)
        x = 20 + (i % 5) * 420
        y = 675 + (i // 5) * 330
        canvas.paste(img, (x, y))
        draw.rectangle((x, y, x + img.width, y + img.height), outline=(255, 210, 0), width=2)
        label(draw, (x + 4, y + 4), path.stem)

    draw.text((20, 1350), "Native crops", fill=(255, 230, 120))
    for i, path in enumerate(crops):
        img = Image.open(path).convert("RGB")
        img.thumbnail((220, 220), Image.Resampling.LANCZOS)
        x = 20 + i * 260
        y = 1385
        canvas.paste(img, (x, y))
        draw.rectangle((x, y, x + img.width, y + img.height), outline=(255, 210, 0), width=2)
        label(draw, (x + 4, y + 4), path.stem.replace("crop_", ""))
    draw.text((1050, 1598), f"VERDICT: {verdict}", fill=(255, 180, 120) if verdict.startswith("BLOCKED") else (120, 255, 170))
    draw.text((1050, 1630), "No 2500 tiles, no Unity, no QA/Builder-C, no 25600 master.", fill=(255, 255, 255))
    return canvas


def main() -> None:
    for d in (STAGE, DOCS, SOURCE_DIR, PANELS, FUSION, SEAMS, CROPS, PROOF):
        d.mkdir(parents=True, exist_ok=True)

    source_path = SOURCE_DIR / "thread2_native_4096_attempt_v4_generated_source.png"
    shutil.copy2(GENERATED, source_path)
    source = Image.open(source_path).convert("RGB")
    width, height = source.size

    # Harness runs at observed native size. If this is below 4096, we still prove
    # the block with exact dimensions and avoid any fake upscale claim.
    panel_size = min(width, height, 1024)
    stride = max(1, panel_size // 2)
    xs = sorted(set([0, max(0, width - panel_size), min(max(0, width - panel_size), stride)]))
    ys = sorted(set([0, max(0, height - panel_size), min(max(0, height - panel_size), stride)]))
    boxes: dict[str, tuple[int, int, int, int]] = {}
    for r, y in enumerate(ys):
        for c, x in enumerate(xs):
            boxes[f"panel_r{r}_c{c}"] = (x, y, x + panel_size, y + panel_size)

    accumulator = np.zeros((height, width, 3), dtype=np.float32)
    wsum = np.zeros((height, width, 1), dtype=np.float32)
    wt = weight(panel_size, min(256, panel_size // 4))
    panel_paths = {}
    for name, (x0, y0, x1, y1) in boxes.items():
        panel = source.crop((x0, y0, x1, y1))
        path = PANELS / f"{name}_{panel_size}_overlap.png"
        save(panel, path)
        panel_paths[name] = path
        arr = np.asarray(panel, dtype=np.float32)
        accumulator[y0:y1, x0:x1, :] += arr * wt
        wsum[y0:y1, x0:x1, :] += wt

    fused_arr = np.divide(accumulator, np.maximum(wsum, 1e-6))
    fused = Image.fromarray(np.clip(np.rint(fused_arr), 0, 255).astype(np.uint8), "RGB")
    fused_path = FUSION / "thread2_native_4096_attempt_v4_fused_lock.png"
    save(fused, fused_path)
    wprev = Image.fromarray(np.clip(wsum[:, :, 0] / float(wsum.max()) * 255.0, 0, 255).astype(np.uint8), "L").convert("RGB")
    save(ImageEnhance.Contrast(wprev).enhance(1.8), FUSION / "fusion_weight_sum_preview.png")

    diff_full = np.abs(np.asarray(source, dtype=np.int16) - np.asarray(fused, dtype=np.int16))
    max_diff = int(diff_full.max())
    changed_pixels = int((diff_full.sum(axis=2) > 0).sum())

    grid_path = PROOF / "thread2_native_4096_attempt_v4_grid_reveal.png"
    save(grid_reveal(fused), grid_path)

    seam_paths: list[Path] = []
    for x in xs[1:]:
        p = SEAMS / f"vertical_axis_x{x}_native.png"
        save(fused.crop((max(0, x - 128), 0, min(width, x + 128), height)), p)
        seam_paths.append(p)
    for y in ys[1:]:
        p = SEAMS / f"horizontal_axis_y{y}_native.png"
        save(fused.crop((0, max(0, y - 128), width, min(height, y + 128))), p)
        seam_paths.append(p)
    for x in xs[1:]:
        for y in ys[1:]:
            p = SEAMS / f"corner_intersection_x{x}_y{y}_512.png"
            save(fused.crop((max(0, x - 256), max(0, y - 256), min(width, x + 256), min(height, y + 256))), p)
            seam_paths.append(p)

    crop_specs = {
        "water_bank_512": (90, 140, 512),
        "forest_center_512": (480, 430, 512),
        "mountain_crystal_512": (740, 210, 512),
        "full_center_1024": (max(0, width // 2 - 512), max(0, height // 2 - 512), min(1024, width, height)),
    }
    crop_paths: list[Path] = []
    for name, (x, y, size) in crop_specs.items():
        x = max(0, min(width - size, x))
        y = max(0, min(height - size, y))
        p = CROPS / f"crop_{name}.png"
        save(fused.crop((x, y, x + size, y + size)), p)
        crop_paths.append(p)

    if width >= 4096 and height >= 4096 and max_diff == 0:
        verdict = "PASS_NATIVE_4096_SOURCE_PROOF_NOT_FINAL_50X50"
        ready = "YES_REQUIRES_HUMAN_REVIEW"
        reason = "Generated output reached >=4096 and passed overlap/fusion reconstruction."
    else:
        verdict = "BLOCKED_NATIVE_4096_SOURCE_REQUIRED"
        ready = "NO"
        reason = f"Image environment returned {width}x{height}, below required native >=4096. No upscale or fake 4096 claim was made."

    psheet = PROOF / "thread2_native_4096_attempt_v4_proof_sheet.png"
    save(proof_sheet(source, fused, Image.open(grid_path).convert("RGB"), seam_paths, crop_paths, verdict), psheet)

    manifest = {
        "artifact": "THREAD2_NATIVE_4096_ATTEMPT_V4",
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "source": str(source_path),
        "source_sha256": sha256(source_path),
        "source_resolution_observed": [width, height],
        "native_4096_requested": "YES",
        "native_4096_obtained": "YES" if width >= 4096 and height >= 4096 else "NO",
        "max_native_dimensions_obtained": [width, height],
        "source_is_scale_bridge": "NO",
        "panel_size": panel_size,
        "panel_count": len(panel_paths),
        "panels": {name: {"box": boxes[name], "file": str(path), "sha256": sha256(path)} for name, path in panel_paths.items()},
        "fusion": {
            "method": "overlap_weighted_fusion_on_observed_native_source",
            "fused_lock": str(fused_path),
            "fused_sha256": sha256(fused_path),
            "reconstruction_max_diff_vs_source": max_diff,
            "reconstruction_changed_pixels_vs_source": changed_pixels,
            "weight_sum_preview": str(FUSION / "fusion_weight_sum_preview.png"),
        },
        "proof_sheet": str(psheet),
        "proof_sheet_sha256": sha256(psheet),
        "grid_reveal": str(grid_path),
        "seam_strips": [{"file": str(p), "sha256": sha256(p)} for p in seam_paths],
        "native_crops": [{"file": str(p), "sha256": sha256(p)} for p in crop_paths],
        "decision": {
            "verdict": verdict,
            "source_candidate_ready_for_50x50": ready,
            "reason": reason,
            "recommendation_for_next_agent": "Use an image path/tool that can export genuine native >=4096 coherent source images. Reuse this exact prompt family and proof harness. Do not upscale 1254 outputs and do not build tiles before a >=4096 source passes.",
        },
        "gates": {
            "NATIVE_4096_ATTEMPT_STARTED": "YES",
            "NATIVE_4096_SOURCE_REQUESTED": "YES",
            "NATIVE_4096_SOURCE_OBTAINED": "YES" if width >= 4096 and height >= 4096 else "NO",
            "BLOCKED_NATIVE_4096_SOURCE_REQUIRED": "YES" if width < 4096 or height < 4096 else "NO",
            "OVERLAP_FUSION_CREATED": "YES",
            "RECONSTRUCTION_ZERO_PIXEL_DIFF": "YES" if max_diff == 0 else "NO",
            "SEAM_STRIPS_CREATED": "YES",
            "CROPS_512_1024_CREATED": "YES",
            "PERCEPTUAL_REVIEW": verdict,
            "SOURCE_CANDIDATE_READY_FOR_50X50": ready,
            "FULL_50X50_PACKAGE_CREATED": "NO",
            "READY_FOR_QA_BUILDERC": "NO",
            "READY_FOR_UNITY_HANDOFF": "NO",
            "MASTER_25600_AUTHORIZED": "NO",
        },
    }
    manifest_path = STAGE / "THREAD2_NATIVE_4096_ATTEMPT_V4_MANIFEST.json"
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    review_path = STAGE / "THREAD2_NATIVE_4096_ATTEMPT_V4_PERCEPTUAL_REVIEW.md"
    review_path.write_text(
        "\n".join(
            [
                "# Thread2 Native 4096 Attempt V4 Perceptual Review",
                "",
                f"VERDICT={verdict}",
                "FINAL_50X50_CANDIDATE=NO",
                f"SOURCE_CANDIDATE_READY_FOR_50X50={ready}",
                "",
                "## Result",
                "",
                f"- Native 4096 requested: YES.",
                f"- Observed image export: {width}x{height}.",
                f"- Native 4096 obtained: {'YES' if width >= 4096 and height >= 4096 else 'NO'}.",
                f"- Reconstruction max diff after overlap/fusion: {max_diff}.",
                "",
                "## Blocker",
                "",
                reason,
                "",
                "## Recommendation",
                "",
                "Route the next attempt to an image/export path capable of genuine native >=4096 coherent sources. Keep this visual direction and proof harness. Do not upscale this output, do not create 2500 tiles, and do not hand off Unity.",
                "",
                "## Gates",
                "",
                "READY_FOR_QA_BUILDERC=NO",
                "READY_FOR_UNITY_HANDOFF=NO",
                "MASTER_25600_AUTHORIZED=NO",
            ]
        ),
        encoding="utf-8",
    )
    receipt = {
        "artifact": "THREAD2_NATIVE_4096_ATTEMPT_V4",
        "status": verdict,
        "source_candidate_ready_for_50x50": ready,
        "source_resolution_observed": [width, height],
        "max_native_dimensions_obtained": [width, height],
        "checkpoint": str(STAGE / "THREAD2_NATIVE_4096_ATTEMPT_V4_CHECKPOINT.md"),
        "manifest": str(manifest_path),
        "proof_sheet": str(psheet),
        "perceptual_review": str(review_path),
        "receipt_created_utc": datetime.now(timezone.utc).isoformat(),
        "gates": manifest["gates"],
    }
    receipt_path = STAGE / "THREAD2_NATIVE_4096_ATTEMPT_V4_RECEIPT.json"
    receipt_path.write_text(json.dumps(receipt, indent=2), encoding="utf-8")
    docs_path = DOCS / "Thread2_Native4096AttemptV4_Report.md"
    docs_path.write_text(review_path.read_text(encoding="utf-8") + f"\n\nProof sheet: `{psheet}`\nManifest: `{manifest_path}`\nReceipt: `{receipt_path}`\n", encoding="utf-8")
    print(json.dumps({"stage": str(STAGE), "source_resolution_observed": [width, height], "proof_sheet": str(psheet), "receipt": str(receipt_path), "verdict": verdict}, indent=2))


if __name__ == "__main__":
    main()
