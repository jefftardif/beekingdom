from __future__ import annotations

import hashlib
import json
import math
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageEnhance


ROOT = Path(r"C:\projets\beekingdomgame-master")
STAGE = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_wave5method_restart_staging"
OUT = STAGE / "micro_superpanel_4096x3072"
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


def cosine_weight(width: int, height: int, feather: int, touches: dict[str, bool]) -> np.ndarray:
    wx = np.ones(width, dtype=np.float32)
    wy = np.ones(height, dtype=np.float32)

    def ramp(n: int) -> np.ndarray:
        t = np.linspace(0.0, 1.0, n, dtype=np.float32)
        return 0.5 - 0.5 * np.cos(t * math.pi)

    f_x = min(feather, width // 2)
    f_y = min(feather, height // 2)
    if not touches["left"] and f_x > 0:
        wx[:f_x] = ramp(f_x)
    if not touches["right"] and f_x > 0:
        wx[-f_x:] = ramp(f_x)[::-1]
    if not touches["top"] and f_y > 0:
        wy[:f_y] = ramp(f_y)
    if not touches["bottom"] and f_y > 0:
        wy[-f_y:] = ramp(f_y)[::-1]
    return wy[:, None] * wx[None, :]


def save_contact_sheet(images: list[tuple[str, Image.Image]], path: Path, thumb_w: int = 512, thumb_h: int = 384) -> None:
    panels = []
    for label, im in images:
        scale = min(thumb_w / im.width, thumb_h / im.height)
        thumb = im.resize((max(1, int(im.width * scale)), max(1, int(im.height * scale))), Image.Resampling.LANCZOS)
        panel = Image.new("RGB", (thumb_w, thumb_h + 28), (22, 22, 22))
        panel.paste(thumb.convert("RGB"), ((thumb_w - thumb.width) // 2, 28 + (thumb_h - thumb.height) // 2))
        draw = ImageDraw.Draw(panel)
        draw.rectangle((0, 0, thumb_w, 24), fill=(0, 0, 0))
        draw.text((6, 6), label, fill=(255, 255, 255))
        panels.append(panel)
    cols = 2
    rows = math.ceil(len(panels) / cols)
    cell_w = max(p.width for p in panels)
    cell_h = max(p.height for p in panels)
    sheet = Image.new("RGB", (cols * cell_w + (cols - 1) * 12, rows * cell_h + (rows - 1) * 12), (14, 14, 14))
    for i, p in enumerate(panels):
        x = (i % cols) * (cell_w + 12)
        y = (i // cols) * (cell_h + 12)
        sheet.paste(p, (x, y))
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path)


def draw_window_plan(size: tuple[int, int], windows: list[dict[str, object]], path: Path) -> None:
    scale = 0.18
    im = Image.new("RGB", (int(size[0] * scale), int(size[1] * scale)), (36, 42, 38))
    draw = ImageDraw.Draw(im)
    palette = [(255, 210, 80), (90, 190, 255), (160, 240, 130), (255, 120, 190), (220, 170, 255), (255, 150, 90)]
    for idx, win in enumerate(windows):
        x, y, w, h = [int(win[k]) for k in ("x", "y", "width", "height")]
        box = (int(x * scale), int(y * scale), int((x + w) * scale), int((y + h) * scale))
        draw.rectangle(box, outline=palette[idx % len(palette)], width=3)
        draw.text((box[0] + 5, box[1] + 5), str(win["id"]), fill=palette[idx % len(palette)])
    path.parent.mkdir(parents=True, exist_ok=True)
    im.save(path)


def main() -> None:
    PROOF.mkdir(parents=True, exist_ok=True)
    WINDOWS.mkdir(parents=True, exist_ok=True)

    wave5_sha = sha256(SOURCE_MASTER)
    if wave5_sha != EXPECTED_WAVE5_SHA:
        raise RuntimeError(f"Wave5 SHA mismatch: {wave5_sha}")

    with Image.open(SOURCE_MASTER) as master:
        master = master.convert("RGB")
        # A varied native area from the locked Wave5 premium master: forest,
        # water, rock and biome transitions. This is a method proof, not a
        # Wave6 final source.
        source_box = (4096, 4608, 8192, 7680)
        source = master.crop(source_box)

    source_path = OUT / "wave5method_source_reference_crop_4096x3072.png"
    source.save(source_path)

    width, height = source.size
    window_specs = [
        {"id": "W00", "x": 0, "y": 0, "width": 2048, "height": 2048},
        {"id": "W01", "x": 1536, "y": 0, "width": 2048, "height": 2048},
        {"id": "W02", "x": 2048, "y": 0, "width": 2048, "height": 2048},
        {"id": "W03", "x": 0, "y": 1024, "width": 2048, "height": 2048},
        {"id": "W04", "x": 1536, "y": 1024, "width": 2048, "height": 2048},
        {"id": "W05", "x": 2048, "y": 1024, "width": 2048, "height": 2048},
    ]

    accumulator = np.zeros((height, width, 3), dtype=np.float32)
    weights = np.zeros((height, width), dtype=np.float32)
    window_records = []
    for spec in window_specs:
        x, y, w, h = spec["x"], spec["y"], spec["width"], spec["height"]
        panel = source.crop((x, y, x + w, y + h))
        # Tiny deterministic exposure variations prove the feathered fusion can
        # absorb panel-level drift without drawing boundary lines.
        exposure = 1.0 + {"W00": -0.01, "W01": 0.006, "W02": -0.004, "W03": 0.008, "W04": -0.006, "W05": 0.004}[spec["id"]]
        panel = ImageEnhance.Brightness(panel).enhance(exposure)
        panel_path = WINDOWS / f"{spec['id']}_aligned_window.png"
        panel.save(panel_path)
        touches = {
            "left": x == 0,
            "right": x + w == width,
            "top": y == 0,
            "bottom": y + h == height,
        }
        weight = cosine_weight(w, h, 512, touches)
        arr = np.asarray(panel, dtype=np.float32)
        accumulator[y : y + h, x : x + w, :] += arr * weight[:, :, None]
        weights[y : y + h, x : x + w] += weight
        record = dict(spec)
        record.update(
            {
                "path": str(panel_path),
                "sha256": sha256(panel_path),
                "alignment_dx": 0,
                "alignment_dy": 0,
                "feather_px": 512,
                "exposure_factor": exposure,
            }
        )
        window_records.append(record)

    if np.any(weights <= 0):
        raise RuntimeError("Fusion has uncovered pixels")
    fused_arr = np.clip(accumulator / weights[:, :, None], 0, 255).astype(np.uint8)
    fused = Image.fromarray(fused_arr, "RGB")
    fused_path = OUT / "wave5method_micro_superpanel_fused_4096x3072.png"
    fused.save(fused_path)

    weight_preview = np.clip(weights / weights.max() * 255, 0, 255).astype(np.uint8)
    weight_path = PROOF / "fusion_weight_sum_preview.png"
    Image.fromarray(weight_preview, "L").save(weight_path)

    plan_path = PROOF / "window_overlap_plan.png"
    draw_window_plan((width, height), window_records, plan_path)

    strips = []
    strip_specs = [
        ("vertical_overlap_x1536", (1536 - 96, 0, 1536 + 96, height)),
        ("vertical_overlap_x2048", (2048 - 96, 0, 2048 + 96, height)),
        ("horizontal_overlap_y1024", (0, 1024 - 96, width, 1024 + 96)),
        ("intersection_x1536_y1024", (1536 - 256, 1024 - 256, 1536 + 256, 1024 + 256)),
        ("intersection_x2048_y1024", (2048 - 256, 1024 - 256, 2048 + 256, 1024 + 256)),
    ]
    for label, box in strip_specs:
        crop = fused.crop(box)
        p = PROOF / f"seam_strip_{label}.png"
        crop.save(p)
        strips.append((label, p, sha256(p)))

    crop_specs = [
        ("native_crop_center_weave", (1792, 1280, 2304, 1792)),
        ("native_crop_left_transition", (1024, 768, 1536, 1280)),
        ("native_crop_right_transition", (2560, 1408, 3072, 1920)),
        ("native_crop_lower_biome", (1536, 2304, 2048, 2816)),
        ("native_crop_upper_biome", (2560, 512, 3072, 1024)),
        ("native_crop_intersection", (1280, 768, 1792, 1280)),
    ]
    crops = []
    for label, box in crop_specs:
        crop = fused.crop(box)
        p = PROOF / f"{label}.png"
        crop.save(p)
        crops.append((label, p, sha256(p)))

    save_contact_sheet(
        [
            ("source reference crop", source),
            ("fused micro-superpanel", fused),
            ("window overlap plan", Image.open(plan_path).convert("RGB")),
            ("fusion weight sum", Image.open(weight_path).convert("RGB")),
            ("vertical seam strip x1536", Image.open(strips[0][1]).convert("RGB")),
            ("horizontal seam strip y1024", Image.open(strips[2][1]).convert("RGB")),
            ("native crop center", Image.open(crops[0][1]).convert("RGB")),
            ("native crop intersection", Image.open(crops[5][1]).convert("RGB")),
        ],
        PROOF / "wave5method_micro_superpanel_proof_sheet.png",
    )

    manifest = {
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "status": "MICRO_SUPERPANEL_METHOD_PROOF_CREATED",
        "wave5_reference_master": str(SOURCE_MASTER),
        "wave5_reference_sha256": wave5_sha,
        "source_box_xyxy": list(source_box),
        "source_reference_crop": {"path": str(source_path), "sha256": sha256(source_path), "dimensions": [width, height]},
        "fused_micro_superpanel": {"path": str(fused_path), "sha256": sha256(fused_path), "dimensions": [width, height]},
        "windows": window_records,
        "fusion": {
            "method": "float32 weighted accumulator with 512px cosine feathering",
            "uncovered_pixels": 0,
            "weight_sum_preview": {"path": str(weight_path), "sha256": sha256(weight_path)},
        },
        "proofs": {
            "window_overlap_plan": {"path": str(plan_path), "sha256": sha256(plan_path)},
            "seam_strips": [{"id": label, "path": str(path), "sha256": digest} for label, path, digest in strips],
            "native_crops": [{"id": label, "path": str(path), "sha256": digest} for label, path, digest in crops],
            "proof_sheet": {
                "path": str(PROOF / "wave5method_micro_superpanel_proof_sheet.png"),
                "sha256": sha256(PROOF / "wave5method_micro_superpanel_proof_sheet.png"),
            },
        },
        "not_a_final_wave6_candidate": True,
        "no_2500_tiles_built": True,
        "unity_modified": False,
    }
    manifest_path = OUT / "wave5method_micro_superpanel_manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    receipt = {
        "created_utc": manifest["created_utc"],
        "WAVE5_REFERENCE_INTEGRITY": "PASS",
        "MICRO_SUPERPANEL_CREATED": "YES",
        "SOURCE_COHERENCE_PROOF_CREATED": "YES",
        "OVERLAP_WINDOWS_CREATED": "6/6",
        "FUSION_METHOD": "float32 weighted cosine feather, 512px",
        "UNCOVERED_PIXELS": 0,
        "SEAM_STRIPS_CREATED": len(strips),
        "NATIVE_CROPS_CREATED": len(crops),
        "PROOF_SHEET_CREATED": "YES",
        "INTERNAL_METHOD_PROOF_STATUS": "READY_FOR_HUMAN_VISUAL_REVIEW",
        "READY_FOR_2500_TILES": "NO",
        "READY_FOR_QA_BUILDERC": "NO",
        "READY_FOR_UNITY_HANDOFF": "NO",
        "MASTER_25600_AUTHORIZED": "NO",
        "WAVE5_MODIFIED": "NO",
        "manifest": str(manifest_path),
        "proof_sheet": str(PROOF / "wave5method_micro_superpanel_proof_sheet.png"),
        "fused_micro_superpanel": str(fused_path),
    }
    (OUT / "WAVE5_METHOD_MICRO_SUPERPANEL_RECEIPT.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
