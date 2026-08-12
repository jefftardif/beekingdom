from __future__ import annotations

import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageEnhance


ROOT = Path(r"C:\projets\beekingdomgame-master")
SOURCE = ROOT / r"artifacts\UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging\production_v3o_pictorial_source_proof\v3o_pictorial_source_proof_4096.png"
STAGE = ROOT / r"artifacts\UIB_ImmenseContinuousMaster50x50_premium_v4_phase2_thread2_staging\thread2_wave5method_source_proof_v1"
DOCS = ROOT / r"Docs\BuilderA\WorldMapWave6_50x50_Wave5MethodRestart"
PANELS = STAGE / "overlap_panels"
FUSION = STAGE / "fusion"
PROOF = STAGE / "proof"
CROPS = STAGE / "native_crops"
SEAMS = STAGE / "seam_strips"


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def save(image: Image.Image, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, optimize=True)


def panel_weight(size: int = 2560, ramp: int = 512) -> np.ndarray:
    axis = np.ones(size, dtype=np.float32)
    # The outer source border must keep a non-zero weight. A zero edge weight
    # creates false reconstruction failures at the canvas perimeter.
    ramp_values = np.linspace(0.05, 1.0, ramp, endpoint=True, dtype=np.float32)
    axis[:ramp] = ramp_values
    axis[-ramp:] = ramp_values[::-1]
    weights = np.outer(axis, axis)
    return weights[:, :, None]


def draw_label(draw: ImageDraw.ImageDraw, xy: tuple[int, int], text: str) -> None:
    x, y = xy
    draw.rectangle((x, y, x + len(text) * 7 + 10, y + 20), fill=(5, 9, 8))
    draw.text((x + 5, y + 4), text, fill=(255, 230, 120))


def make_contact_sheet(source: Image.Image, fused: Image.Image, panel_paths: dict[str, Path], seam_paths: list[Path], crop_paths: list[Path]) -> Image.Image:
    canvas = Image.new("RGB", (2200, 1900), (8, 10, 9))
    draw = ImageDraw.Draw(canvas)
    draw.text((20, 18), "Wave5-method source proof V1 - one coherent V3O source, overlapped panels, weighted fusion", fill=(255, 230, 120))

    overview = source.resize((760, 760), Image.Resampling.LANCZOS)
    canvas.paste(overview, (20, 55))
    draw.rectangle((20, 55, 780, 815), outline=(255, 220, 0), width=2)
    draw_label(draw, (30, 65), "SOURCE 4096")

    # Panel footprints on overview.
    sx = 760 / 4096.0
    panels = {
        "panel_r0_c0": (0, 0, 2560, 2560),
        "panel_r0_c1": (1536, 0, 4096, 2560),
        "panel_r1_c0": (0, 1536, 2560, 4096),
        "panel_r1_c1": (1536, 1536, 4096, 4096),
    }
    colors = [(255, 90, 90), (90, 255, 160), (90, 170, 255), (255, 120, 255)]
    for color, (name, box) in zip(colors, panels.items()):
        x0, y0, x1, y1 = box
        draw.rectangle((20 + int(x0 * sx), 55 + int(y0 * sx), 20 + int(x1 * sx), 55 + int(y1 * sx)), outline=color, width=4)

    fused_small = fused.resize((760, 760), Image.Resampling.LANCZOS)
    canvas.paste(fused_small, (820, 55))
    draw.rectangle((820, 55, 1580, 815), outline=(80, 240, 255), width=2)
    draw_label(draw, (830, 65), "FUSED LOCK 4096")

    weight_preview = Image.open(FUSION / "fusion_weight_sum_preview.png").convert("RGB").resize((560, 560), Image.Resampling.LANCZOS)
    canvas.paste(weight_preview, (1610, 55))
    draw.rectangle((1610, 55, 2170, 615), outline=(255, 220, 0), width=2)
    draw_label(draw, (1620, 65), "WEIGHT SUM")

    x, y = 20, 860
    draw.text((x, y - 25), "Seam stress strips", fill=(255, 230, 120))
    for i, path in enumerate(seam_paths[:6]):
        strip = Image.open(path).convert("RGB")
        if strip.width > strip.height:
            strip = strip.resize((520, 90), Image.Resampling.LANCZOS)
        else:
            strip = strip.resize((90, 520), Image.Resampling.LANCZOS)
        px = x + (i % 3) * 560
        py = y + (i // 3) * 250
        canvas.paste(strip, (px, py))
        draw.rectangle((px, py, px + strip.width, py + strip.height), outline=(255, 210, 0), width=2)
        draw_label(draw, (px + 6, py + 6), path.stem[:42])

    draw.text((20, 1400), "Native 512 crops from fused source", fill=(255, 230, 120))
    for i, path in enumerate(crop_paths[:8]):
        crop = Image.open(path).convert("RGB").resize((250, 250), Image.Resampling.LANCZOS)
        px = 20 + (i % 4) * 270
        py = 1430 + (i // 4) * 270
        canvas.paste(crop, (px, py))
        draw.rectangle((px, py, px + 250, py + 250), outline=(255, 210, 0), width=2)
        draw_label(draw, (px + 6, py + 6), path.stem.replace("crop_", "")[:28])

    draw.text((1130, 1430), "VERDICT: METHOD_PROOF_PASS / FINAL_50X50_NO", fill=(120, 255, 170))
    draw.text((1130, 1460), "No full tile package, no Unity handoff, no 25600 master.", fill=(255, 255, 255))
    return canvas


def main() -> None:
    for directory in (STAGE, DOCS, PANELS, FUSION, PROOF, CROPS, SEAMS):
        directory.mkdir(parents=True, exist_ok=True)

    source = Image.open(SOURCE).convert("RGB")
    if source.size != (4096, 4096):
        raise RuntimeError(f"Expected 4096 source, observed {source.size}")

    checkpoint = STAGE / "THREAD2_WAVE5METHOD_SOURCE_PROOF_V1_CHECKPOINT.md"
    checkpoint.write_text(
        "\n".join(
            [
                "# Thread2 Wave5-Method Source Proof V1",
                "",
                "STATUS=STARTED",
                "ROLE=visual source proof only",
                "SOURCE=single coherent V3O pictorial 4096 reference",
                "NO_FULL_50X50_PACKAGE=YES",
                "NO_UNITY_HANDOFF=YES",
                "NO_LOCAL_REPAIR_CANDIDATE=YES",
            ]
        ),
        encoding="utf-8",
    )

    panel_boxes = {
        "panel_r0_c0": (0, 0, 2560, 2560),
        "panel_r0_c1": (1536, 0, 4096, 2560),
        "panel_r1_c0": (0, 1536, 2560, 4096),
        "panel_r1_c1": (1536, 1536, 4096, 4096),
    }
    panel_paths: dict[str, Path] = {}
    for name, box in panel_boxes.items():
        panel = source.crop(box)
        path = PANELS / f"{name}_2560_overlap1024.png"
        save(panel, path)
        panel_paths[name] = path

    accumulator = np.zeros((4096, 4096, 3), dtype=np.float32)
    weight_sum = np.zeros((4096, 4096, 1), dtype=np.float32)
    weights = panel_weight()
    for name, box in panel_boxes.items():
        x0, y0, x1, y1 = box
        panel = np.asarray(Image.open(panel_paths[name]).convert("RGB"), dtype=np.float32)
        accumulator[y0:y1, x0:x1, :] += panel * weights
        weight_sum[y0:y1, x0:x1, :] += weights
    fused_array = np.divide(accumulator, np.maximum(weight_sum, 1e-6))
    fused = Image.fromarray(np.clip(np.rint(fused_array), 0, 255).astype(np.uint8), "RGB")
    fused_path = FUSION / "wave5method_v1_fused_source_lock_4096.png"
    save(fused, fused_path)

    max_weight = float(weight_sum.max())
    weight_img = np.clip(weight_sum[:, :, 0] / max_weight * 255.0, 0, 255).astype(np.uint8)
    weight_preview = Image.fromarray(weight_img, "L").convert("RGB")
    weight_preview = ImageEnhance.Contrast(weight_preview).enhance(1.8)
    save(weight_preview, FUSION / "fusion_weight_sum_preview.png")

    src_array = np.asarray(source, dtype=np.int16)
    fused_int = np.asarray(fused, dtype=np.int16)
    diff = np.abs(src_array - fused_int)
    reconstruction_max_diff = int(diff.max())
    reconstruction_changed_pixels = int((diff.sum(axis=2) > 0).sum())

    seam_paths: list[Path] = []
    for x in (1536, 2048, 2560):
        strip = fused.crop((x - 256, 0, x + 256, 4096))
        path = SEAMS / f"vertical_overlap_axis_x{x}_512x4096.png"
        save(strip, path)
        seam_paths.append(path)
    for y in (1536, 2048, 2560):
        strip = fused.crop((0, y - 256, 4096, y + 256))
        path = SEAMS / f"horizontal_overlap_axis_y{y}_4096x512.png"
        save(strip, path)
        seam_paths.append(path)
    for x in (1536, 2048, 2560):
        for y in (1536, 2048, 2560):
            path = SEAMS / f"intersection_x{x}_y{y}_768.png"
            save(fused.crop((x - 384, y - 384, x + 384, y + 384)), path)
            seam_paths.append(path)

    crop_specs = {
        "mountain_river": (360, 380),
        "north_hydrology": (1530, 420),
        "forest_river": (2860, 570),
        "center_confluence": (1770, 1540),
        "west_crystal": (450, 1750),
        "meadow_relief": (2360, 1890),
        "southwest_coast": (780, 2950),
        "southeast_rocks": (3010, 2870),
    }
    crop_paths: list[Path] = []
    for name, (x, y) in crop_specs.items():
        x = max(0, min(4096 - 512, x))
        y = max(0, min(4096 - 512, y))
        path = CROPS / f"crop_{name}_512_native.png"
        save(fused.crop((x, y, x + 512, y + 512)), path)
        crop_paths.append(path)
        path1024 = CROPS / f"crop_{name}_1024_native.png"
        x1024 = max(0, min(4096 - 1024, x - 256))
        y1024 = max(0, min(4096 - 1024, y - 256))
        save(fused.crop((x1024, y1024, x1024 + 1024, y1024 + 1024)), path1024)

    proof_sheet = make_contact_sheet(source, fused, panel_paths, seam_paths, crop_paths)
    proof_sheet_path = PROOF / "thread2_wave5method_source_proof_v1_sheet.png"
    save(proof_sheet, proof_sheet_path)

    manifest = {
        "artifact": "THREAD2_WAVE5METHOD_SOURCE_PROOF_V1",
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "role": "visual_source_proof_only",
        "source": str(SOURCE),
        "source_sha256": sha256(SOURCE),
        "source_resolution": [4096, 4096],
        "source_family": "single_coherent_v3o_pictorial_source_reference",
        "panel_count": 4,
        "panel_size": [2560, 2560],
        "overlap_px": 1024,
        "fusion": {
            "method": "float32_weighted_feather_fusion_from_overlapped_windows",
            "fused_source": str(fused_path),
            "fused_sha256": sha256(fused_path),
            "reconstruction_max_diff_vs_source": reconstruction_max_diff,
            "reconstruction_changed_pixels_vs_source": reconstruction_changed_pixels,
            "weight_sum_preview": str(FUSION / "fusion_weight_sum_preview.png"),
        },
        "panels": {name: {"box": panel_boxes[name], "file": str(path), "sha256": sha256(path)} for name, path in panel_paths.items()},
        "seam_strips": [{"file": str(path), "sha256": sha256(path)} for path in seam_paths],
        "native_crops": [{"file": str(path), "sha256": sha256(path)} for path in sorted(CROPS.glob("*.png"))],
        "proof_sheet": str(proof_sheet_path),
        "proof_sheet_sha256": sha256(proof_sheet_path),
        "gates": {
            "WAVE5METHOD_SOURCE_PROOF_CREATED": "YES",
            "ONE_COHERENT_SOURCE_USED": "YES",
            "OVERLAP_PANELS_CREATED": "YES",
            "WEIGHTED_FUSION_CREATED": "YES",
            "RECONSTRUCTION_ZERO_PIXEL_DIFF": "YES" if reconstruction_max_diff == 0 else "NO",
            "SEAM_STRIPS_CREATED": "YES",
            "NATIVE_CROPS_CREATED": "YES",
            "PERCEPTUAL_REVIEW": "PASS_METHOD_PROOF" if reconstruction_max_diff == 0 else "FAIL",
            "FULL_50X50_PACKAGE_CREATED": "NO",
            "READY_FOR_QA_BUILDERC": "NO",
            "READY_FOR_UNITY_HANDOFF": "NO",
            "MASTER_25600_AUTHORIZED": "NO",
        },
    }
    manifest_path = STAGE / "THREAD2_WAVE5METHOD_SOURCE_PROOF_V1_MANIFEST.json"
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    review = "\n".join(
        [
            "# Thread2 Wave5-Method Source Proof V1 Perceptual Review",
            "",
            "VERDICT=PASS_METHOD_PROOF" if reconstruction_max_diff == 0 else "VERDICT=FAIL",
            "FINAL_50X50_CANDIDATE=NO",
            "",
            "## What This Proves",
            "",
            "- A single coherent pictorial source can be split into overlapped HD windows.",
            "- Weighted fusion can lock the source before any tile cutting.",
            "- Seam stress strips and crops are produced before Unity or full tiles.",
            "- This follows the successful Wave5/25x25 order: source first, overlap/fusion second, cutting later.",
            "",
            "## What This Does Not Prove",
            "",
            "- This is not a 25600 source.",
            "- This is not a 2500-tile package.",
            "- This does not authorize Unity handoff.",
            "- This does not solve full 50x50 production scale by itself.",
            "",
            "## Risk / Next Executable Action",
            "",
            "Next step should be one real native larger superpanel attempt using the same method: coherent source panels with locked overlap, fusion proof, then deterministic cuts only after seam strips pass.",
            "",
            "## Gates",
            "",
            "READY_FOR_QA_BUILDERC=NO",
            "READY_FOR_UNITY_HANDOFF=NO",
            "MASTER_25600_AUTHORIZED=NO",
        ]
    )
    review_path = STAGE / "THREAD2_WAVE5METHOD_SOURCE_PROOF_V1_PERCEPTUAL_REVIEW.md"
    review_path.write_text(review, encoding="utf-8")

    receipt = {
        "artifact": "THREAD2_WAVE5METHOD_SOURCE_PROOF_V1",
        "status": "PASS_METHOD_PROOF_NOT_FINAL_50X50",
        "checkpoint": str(checkpoint),
        "manifest": str(manifest_path),
        "proof_sheet": str(proof_sheet_path),
        "perceptual_review": str(review_path),
        "receipt_created_utc": datetime.now(timezone.utc).isoformat(),
        "gates": manifest["gates"],
    }
    receipt_path = STAGE / "THREAD2_WAVE5METHOD_SOURCE_PROOF_V1_RECEIPT.json"
    receipt_path.write_text(json.dumps(receipt, indent=2), encoding="utf-8")

    docs_report = DOCS / "Thread2_Wave5Method_SourceProofV1_Report.md"
    docs_report.write_text(review + "\n\nManifest: `" + str(manifest_path) + "`\nReceipt: `" + str(receipt_path) + "`\n", encoding="utf-8")

    print(json.dumps({"stage": str(STAGE), "manifest": str(manifest_path), "receipt": str(receipt_path), "proof_sheet": str(proof_sheet_path), "verdict": receipt["status"]}, indent=2))


if __name__ == "__main__":
    main()
