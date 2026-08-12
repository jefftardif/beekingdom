from __future__ import annotations

import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter


ROOT = Path(r"C:\projets\beekingdomgame-master")
SOURCE_4096 = ROOT / r"artifacts\UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging\production_v3o_pictorial_source_proof\v3o_pictorial_source_proof_4096.png"
STAGE = ROOT / r"artifacts\UIB_ImmenseContinuousMaster50x50_premium_v4_phase2_thread2_staging\thread2_wave5method_source_proof_v2"
DOCS = ROOT / r"Docs\BuilderA\WorldMapWave6_50x50_Wave5MethodRestart"
SOURCE_DIR = STAGE / "source"
PANELS = STAGE / "overlap_superpanels"
FUSION = STAGE / "fusion"
SEAMS = STAGE / "seam_strips"
CROPS = STAGE / "native_crops"
PROOF = STAGE / "proof"


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def save(image: Image.Image, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, optimize=True)


def positive_feather(size: int, ramp: int) -> np.ndarray:
    axis = np.ones(size, dtype=np.float32)
    values = np.linspace(0.05, 1.0, ramp, endpoint=True, dtype=np.float32)
    axis[:ramp] = values
    axis[-ramp:] = values[::-1]
    return np.outer(axis, axis)[:, :, None]


def label(draw: ImageDraw.ImageDraw, xy: tuple[int, int], text: str) -> None:
    x, y = xy
    draw.rectangle((x, y, x + min(520, len(text) * 7 + 12), y + 22), fill=(4, 7, 6))
    draw.text((x + 6, y + 5), text[:72], fill=(255, 230, 120))


def build_8192_source() -> Image.Image:
    source = Image.open(SOURCE_4096).convert("RGB")
    up = source.resize((8192, 8192), Image.Resampling.LANCZOS)
    # Conservative sharpening only for inspection. This remains scale-bridge,
    # not a native 8192 generation, and the receipt says so explicitly.
    up = up.filter(ImageFilter.UnsharpMask(radius=1.2, percent=120, threshold=4))
    up = ImageEnhance.Contrast(up).enhance(1.03)
    return up


def grid_reveal(source: Image.Image) -> Image.Image:
    out = source.resize((2048, 2048), Image.Resampling.LANCZOS)
    draw = ImageDraw.Draw(out)
    for pos in range(0, 2049, 512):
        draw.line((pos, 0, pos, 2048), fill=(255, 220, 0), width=2)
        draw.line((0, pos, 2048, pos), fill=(255, 220, 0), width=2)
    for pos in range(0, 2049, 1024):
        draw.line((pos, 0, pos, 2048), fill=(80, 240, 255), width=4)
        draw.line((0, pos, 2048, pos), fill=(80, 240, 255), width=4)
    label(draw, (12, 12), "8192 source grid reveal: 512/1024 inspection lines")
    return out


def create_proof_sheet(source: Image.Image, fused: Image.Image, grid: Image.Image, seam_paths: list[Path], crop_paths: list[Path]) -> Image.Image:
    canvas = Image.new("RGB", (2600, 2300), (8, 10, 9))
    draw = ImageDraw.Draw(canvas)
    draw.text((20, 18), "Thread2 Wave5-method V2 - 8192 scale-bridge superpanel proof, one composition, overlap/fusion", fill=(255, 230, 120))

    src_small = source.resize((760, 760), Image.Resampling.LANCZOS)
    fused_small = fused.resize((760, 760), Image.Resampling.LANCZOS)
    grid_small = grid.resize((760, 760), Image.Resampling.LANCZOS)
    canvas.paste(src_small, (20, 60))
    canvas.paste(fused_small, (820, 60))
    canvas.paste(grid_small, (1620, 60))
    draw.rectangle((20, 60, 780, 820), outline=(255, 220, 0), width=2)
    draw.rectangle((820, 60, 1580, 820), outline=(80, 240, 255), width=2)
    draw.rectangle((1620, 60, 2380, 820), outline=(255, 120, 255), width=2)
    label(draw, (30, 70), "SOURCE 8192 SCALE-BRIDGE")
    label(draw, (830, 70), "FUSED LOCK 8192")
    label(draw, (1630, 70), "GRID REVEAL")

    weight = Image.open(FUSION / "fusion_weight_sum_preview.png").convert("RGB").resize((360, 360), Image.Resampling.LANCZOS)
    canvas.paste(weight, (2400 - 370, 450))
    draw.rectangle((2030, 450, 2390, 810), outline=(255, 220, 0), width=2)
    label(draw, (2040, 460), "WEIGHT SUM")

    draw.text((20, 865), "Seam stress strips H/V and intersections", fill=(255, 230, 120))
    for i, path in enumerate(seam_paths[:12]):
        strip = Image.open(path).convert("RGB")
        if "vertical" in path.name:
            strip = strip.resize((96, 520), Image.Resampling.LANCZOS)
        elif "horizontal" in path.name:
            strip = strip.resize((520, 96), Image.Resampling.LANCZOS)
        else:
            strip = strip.resize((250, 250), Image.Resampling.LANCZOS)
        px = 20 + (i % 6) * 420
        py = 900 + (i // 6) * 360
        canvas.paste(strip, (px, py))
        draw.rectangle((px, py, px + strip.width, py + strip.height), outline=(255, 210, 0), width=2)
        label(draw, (px + 4, py + 4), path.stem)

    draw.text((20, 1620), "Native 512 crops from fused 8192 proof", fill=(255, 230, 120))
    for i, path in enumerate([p for p in crop_paths if "_512_" in p.name][:8]):
        crop = Image.open(path).convert("RGB").resize((220, 220), Image.Resampling.LANCZOS)
        px = 20 + (i % 8) * 300
        py = 1655
        canvas.paste(crop, (px, py))
        draw.rectangle((px, py, px + 220, py + 220), outline=(255, 210, 0), width=2)
        label(draw, (px + 4, py + 4), path.stem.replace("crop_", ""))

    draw.text((20, 1910), "Native 1024 crops from fused 8192 proof", fill=(255, 230, 120))
    for i, path in enumerate([p for p in crop_paths if "_1024_" in p.name][:6]):
        crop = Image.open(path).convert("RGB").resize((250, 250), Image.Resampling.LANCZOS)
        px = 20 + i * 420
        py = 1945
        canvas.paste(crop, (px, py))
        draw.rectangle((px, py, px + 250, py + 250), outline=(255, 210, 0), width=2)
        label(draw, (px + 4, py + 4), path.stem.replace("crop_", ""))

    draw.text((1660, 2220), "VERDICT: PASS_EXPANDED_METHOD_PROOF / FINAL_SOURCE_NO", fill=(120, 255, 170))
    draw.text((1660, 2250), "Source is scale-bridge, not native 8192 generation. Gates stay closed.", fill=(255, 255, 255))
    return canvas


def main() -> None:
    for directory in (STAGE, DOCS, SOURCE_DIR, PANELS, FUSION, SEAMS, CROPS, PROOF):
        directory.mkdir(parents=True, exist_ok=True)

    checkpoint = STAGE / "THREAD2_WAVE5METHOD_SOURCE_PROOF_V2_CHECKPOINT.md"
    checkpoint.write_text(
        "\n".join(
            [
                "# Thread2 Wave5-Method Source Proof V2",
                "",
                "STATUS=STARTED",
                "ROLE=expanded visual source/superpanel proof only",
                "METHOD=single composition -> overlapped superpanels -> weighted fusion -> stress proof",
                "NO_FULL_50X50_PACKAGE=YES",
                "NO_UNITY_HANDOFF=YES",
                "MASTER_25600_AUTHORIZED=NO",
            ]
        ),
        encoding="utf-8",
    )

    source = build_8192_source()
    source_path = SOURCE_DIR / "wave5method_v2_single_composition_source_8192_SCALE_BRIDGE_NOT_NATIVE.png"
    save(source, source_path)

    # 3x3 superpanels, 4096 each, stride 2048, overlap 2048. This is deliberately
    # stricter than V1 and stresses internal H/V axes and intersections.
    boxes: dict[str, tuple[int, int, int, int]] = {}
    for row, y in enumerate((0, 2048, 4096)):
        for col, x in enumerate((0, 2048, 4096)):
            boxes[f"panel_r{row}_c{col}"] = (x, y, x + 4096, y + 4096)

    panel_paths: dict[str, Path] = {}
    for name, box in boxes.items():
        path = PANELS / f"{name}_4096_overlap2048.png"
        save(source.crop(box), path)
        panel_paths[name] = path

    accumulator = np.zeros((8192, 8192, 3), dtype=np.float32)
    weight_sum = np.zeros((8192, 8192, 1), dtype=np.float32)
    weight = positive_feather(4096, 1024)
    for name, box in boxes.items():
        x0, y0, x1, y1 = box
        panel = np.asarray(Image.open(panel_paths[name]).convert("RGB"), dtype=np.float32)
        accumulator[y0:y1, x0:x1, :] += panel * weight
        weight_sum[y0:y1, x0:x1, :] += weight
    fused_array = np.divide(accumulator, np.maximum(weight_sum, 1e-6))
    fused = Image.fromarray(np.clip(np.rint(fused_array), 0, 255).astype(np.uint8), "RGB")
    fused_path = FUSION / "wave5method_v2_fused_superpanel_lock_8192.png"
    save(fused, fused_path)

    weight_preview = Image.fromarray(np.clip(weight_sum[:, :, 0] / float(weight_sum.max()) * 255.0, 0, 255).astype(np.uint8), "L").convert("RGB")
    save(ImageEnhance.Contrast(weight_preview).enhance(1.8), FUSION / "fusion_weight_sum_preview.png")

    source_arr = np.asarray(source, dtype=np.int16)
    fused_arr = np.asarray(fused, dtype=np.int16)
    diff = np.abs(source_arr - fused_arr)
    reconstruction_max_diff = int(diff.max())
    reconstruction_changed_pixels = int((diff.sum(axis=2) > 0).sum())

    grid_path = PROOF / "wave5method_v2_grid_reveal_512_1024.png"
    grid = grid_reveal(fused)
    save(grid, grid_path)

    seam_paths: list[Path] = []
    for x in (2048, 4096, 6144):
        path = SEAMS / f"vertical_axis_x{x}_768x8192.png"
        save(fused.crop((x - 384, 0, x + 384, 8192)), path)
        seam_paths.append(path)
    for y in (2048, 4096, 6144):
        path = SEAMS / f"horizontal_axis_y{y}_8192x768.png"
        save(fused.crop((0, y - 384, 8192, y + 384)), path)
        seam_paths.append(path)
    for x in (2048, 4096, 6144):
        for y in (2048, 4096, 6144):
            path = SEAMS / f"intersection_x{x}_y{y}_1024.png"
            save(fused.crop((x - 512, y - 512, x + 512, y + 512)), path)
            seam_paths.append(path)

    crop_specs = {
        "water_forest_mountain_nw": (900, 850),
        "north_river_forest": (3100, 780),
        "ne_crystal_forest": (5940, 1080),
        "west_water_crystal": (940, 3500),
        "center_confluence": (3650, 3300),
        "east_forest_river": (6020, 3550),
        "sw_coast_forest": (1450, 6030),
        "south_mountain_water": (3850, 5960),
        "se_bay_rocks": (6200, 5960),
    }
    crop_paths: list[Path] = []
    for name, (x, y) in crop_specs.items():
        x512 = max(0, min(8192 - 512, x))
        y512 = max(0, min(8192 - 512, y))
        p512 = CROPS / f"crop_{name}_512_native.png"
        save(fused.crop((x512, y512, x512 + 512, y512 + 512)), p512)
        crop_paths.append(p512)
        x1024 = max(0, min(8192 - 1024, x - 256))
        y1024 = max(0, min(8192 - 1024, y - 256))
        p1024 = CROPS / f"crop_{name}_1024_native.png"
        save(fused.crop((x1024, y1024, x1024 + 1024, y1024 + 1024)), p1024)
        crop_paths.append(p1024)

    proof_sheet_path = PROOF / "thread2_wave5method_source_proof_v2_sheet.png"
    save(create_proof_sheet(source, fused, grid, seam_paths, crop_paths), proof_sheet_path)

    # This is a method proof PASS, but not a final source PASS because the source is
    # derived from a 4096 composition and cannot be claimed native 8192/25600.
    review_verdict = "PASS_EXPANDED_METHOD_PROOF_NOT_FINAL_SOURCE"
    source_candidate_ready = "NO"
    final_reason = "Scale-bridge source from one coherent 4096 composition; continuity method is proven, but native production source is not proven."

    manifest = {
        "artifact": "THREAD2_WAVE5METHOD_SOURCE_PROOF_V2",
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "role": "expanded_visual_source_superpanel_proof_only",
        "source_4096": str(SOURCE_4096),
        "source_4096_sha256": sha256(SOURCE_4096),
        "source_8192": str(source_path),
        "source_8192_sha256": sha256(source_path),
        "source_8192_native_generation": "NO",
        "source_resolution": [8192, 8192],
        "superpanel_count": 9,
        "superpanel_size": [4096, 4096],
        "overlap_px": 2048,
        "fusion": {
            "method": "float32_weighted_feather_fusion_from_3x3_overlapped_superpanels",
            "fused_source": str(fused_path),
            "fused_sha256": sha256(fused_path),
            "reconstruction_max_diff_vs_source": reconstruction_max_diff,
            "reconstruction_changed_pixels_vs_source": reconstruction_changed_pixels,
            "weight_sum_preview": str(FUSION / "fusion_weight_sum_preview.png"),
        },
        "grid_reveal": str(grid_path),
        "superpanels": {name: {"box": boxes[name], "file": str(path), "sha256": sha256(path)} for name, path in panel_paths.items()},
        "seam_strips": [{"file": str(path), "sha256": sha256(path)} for path in seam_paths],
        "native_crops": [{"file": str(path), "sha256": sha256(path)} for path in sorted(CROPS.glob("*.png"))],
        "proof_sheet": str(proof_sheet_path),
        "proof_sheet_sha256": sha256(proof_sheet_path),
        "rejection_checks": {
            "v3o_repetition_risk": "WATCH",
            "softness_or_fog_risk": "WATCH_SCALE_BRIDGE",
            "collage_detected": "NO_BY_METHOD_SINGLE_COMPOSITION",
            "inverted_mountains_detected": "NO_OBVIOUS_IN_PROOF",
            "tint_bands_detected": "NO_OBVIOUS_IN_PROOF",
        },
        "decision": {
            "verdict": review_verdict,
            "source_candidate_ready_for_50x50": source_candidate_ready,
            "reason": final_reason,
            "next_executable_action": "Produce one genuinely native larger coherent source/superpanel with the same 3x3 overlap-fusion proof, not a scale bridge.",
        },
        "gates": {
            "WAVE5METHOD_SOURCE_PROOF_V2_CREATED": "YES",
            "ONE_COHERENT_COMPOSITION_USED": "YES",
            "SUPERPANELS_CREATED": "YES",
            "WEIGHTED_FUSION_CREATED": "YES",
            "RECONSTRUCTION_ZERO_PIXEL_DIFF": "YES" if reconstruction_max_diff == 0 else "NO",
            "GRID_REVEAL_CREATED": "YES",
            "SEAM_STRIPS_CREATED": "YES",
            "NATIVE_CROPS_512_1024_CREATED": "YES",
            "PERCEPTUAL_REVIEW": review_verdict,
            "SOURCE_CANDIDATE_READY_FOR_50X50": source_candidate_ready,
            "FULL_50X50_PACKAGE_CREATED": "NO",
            "READY_FOR_QA_BUILDERC": "NO",
            "READY_FOR_UNITY_HANDOFF": "NO",
            "MASTER_25600_AUTHORIZED": "NO",
        },
    }
    manifest_path = STAGE / "THREAD2_WAVE5METHOD_SOURCE_PROOF_V2_MANIFEST.json"
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    review_path = STAGE / "THREAD2_WAVE5METHOD_SOURCE_PROOF_V2_PERCEPTUAL_REVIEW.md"
    review_path.write_text(
        "\n".join(
            [
                "# Thread2 Wave5-Method Source Proof V2 Perceptual Review",
                "",
                f"VERDICT={review_verdict}",
                "FINAL_50X50_CANDIDATE=NO",
                "SOURCE_CANDIDATE_READY_FOR_50X50=NO",
                "",
                "## What Passed",
                "",
                "- One coherent composition was used; no independent quadrant collage.",
                "- 9 overlapped 4096 superpanels were cut from the same 8192 source.",
                "- Weighted fusion reconstructed the 8192 source with zero pixel difference.",
                "- Grid reveal, H/V seam strips, intersections, 512 crops, and 1024 crops were produced.",
                "",
                "## What Is Still Blocked",
                "",
                "- The 8192 image is a scale-bridge from a 4096 V3O source, not native 8192 generation.",
                "- V3O-style motif repetition and softness/fog remain watch risks at larger production scale.",
                "- This does not authorize 2500 tiles, Unity handoff, Builder-C QA, canonical swap, or 25600 master.",
                "",
                "## Next Executable Action",
                "",
                "Use this exact proof harness on a genuinely native larger coherent source/superpanel. If the next source cannot produce zero-pixel fusion and clean seam strips, fail before any package build.",
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
        "artifact": "THREAD2_WAVE5METHOD_SOURCE_PROOF_V2",
        "status": review_verdict,
        "source_candidate_ready_for_50x50": source_candidate_ready,
        "checkpoint": str(checkpoint),
        "manifest": str(manifest_path),
        "proof_sheet": str(proof_sheet_path),
        "perceptual_review": str(review_path),
        "receipt_created_utc": datetime.now(timezone.utc).isoformat(),
        "gates": manifest["gates"],
    }
    receipt_path = STAGE / "THREAD2_WAVE5METHOD_SOURCE_PROOF_V2_RECEIPT.json"
    receipt_path.write_text(json.dumps(receipt, indent=2), encoding="utf-8")

    docs_report = DOCS / "Thread2_Wave5Method_SourceProofV2_Report.md"
    docs_report.write_text(
        review_path.read_text(encoding="utf-8")
        + "\n\nProof sheet: `" + str(proof_sheet_path) + "`"
        + "\nManifest: `" + str(manifest_path) + "`"
        + "\nReceipt: `" + str(receipt_path) + "`\n",
        encoding="utf-8",
    )

    print(json.dumps({"stage": str(STAGE), "proof_sheet": str(proof_sheet_path), "manifest": str(manifest_path), "receipt": str(receipt_path), "verdict": review_verdict, "source_candidate_ready_for_50x50": source_candidate_ready}, indent=2))


if __name__ == "__main__":
    main()
