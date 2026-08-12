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
SOURCE = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_wave5method_restart_staging" / "scaleup_superpanel_8192x8192" / "wave5method_scaleup_superpanel_fused_8192x8192.png"
OUT = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_wave5method_restart_staging" / "scaleup_superpanel_8192x8192_reinforced_validation"
PROOF = OUT / "proof"


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest().upper()


def luminance(arr: np.ndarray) -> np.ndarray:
    return arr[..., 0] * 0.2126 + arr[..., 1] * 0.7152 + arr[..., 2] * 0.0722


def seam_metrics(arr: np.ndarray, orientation: str, coord: int) -> dict[str, float | str]:
    lum = luminance(arr.astype(np.float32))
    if orientation == "vertical":
        seam = np.abs(lum[:, coord] - lum[:, coord - 1])
        left_ref = np.abs(lum[:, coord - 384] - lum[:, coord - 385])
        right_ref = np.abs(lum[:, coord + 384] - lum[:, coord + 383])
        a = lum[:, coord - 128:coord]
        b = lum[:, coord:coord + 128]
    else:
        seam = np.abs(lum[coord, :] - lum[coord - 1, :])
        left_ref = np.abs(lum[coord - 384, :] - lum[coord - 385, :])
        right_ref = np.abs(lum[coord + 384, :] - lum[coord + 383, :])
        a = lum[coord - 128:coord, :]
        b = lum[coord:coord + 128, :]
    ref = np.concatenate([left_ref.reshape(-1), right_ref.reshape(-1)])
    seam_mean = float(np.mean(seam))
    ref_mean = float(np.mean(ref))
    ratio = seam_mean / max(ref_mean, 1e-6)
    brightness_delta = float(abs(np.mean(a) - np.mean(b)))
    verdict = "PASS" if ratio < 1.35 and brightness_delta < 4.5 else "REVIEW"
    return {
        "orientation": orientation,
        "coord": coord,
        "seam_mean_abs_luma_delta": round(seam_mean, 4),
        "reference_mean_abs_luma_delta": round(ref_mean, 4),
        "seam_to_reference_ratio": round(ratio, 4),
        "brightness_delta_128px": round(brightness_delta, 4),
        "verdict": verdict,
    }


def fit(im: Image.Image, width: int = 560, height: int = 360) -> Image.Image:
    scale = min(width / im.width, height / im.height)
    thumb = im.resize((max(1, int(im.width * scale)), max(1, int(im.height * scale))), Image.Resampling.LANCZOS)
    canvas = Image.new("RGB", (width, height), (18, 18, 18))
    canvas.paste(thumb.convert("RGB"), ((width - thumb.width) // 2, (height - thumb.height) // 2))
    return canvas


def label_panel(label: str, im: Image.Image) -> Image.Image:
    panel = Image.new("RGB", (im.width, im.height + 28), (20, 20, 20))
    panel.paste(im, (0, 28))
    draw = ImageDraw.Draw(panel)
    draw.rectangle((0, 0, panel.width, 24), fill=(0, 0, 0))
    draw.text((6, 6), label, fill=(255, 255, 255))
    return panel


def contact_sheet(items: list[tuple[str, Image.Image]], path: Path) -> None:
    panels = [label_panel(label, fit(im)) for label, im in items]
    cols = 2
    rows = math.ceil(len(panels) / cols)
    w = max(p.width for p in panels)
    h = max(p.height for p in panels)
    sheet = Image.new("RGB", (cols * w + 14, rows * h + (rows - 1) * 14), (12, 12, 12))
    for i, p in enumerate(panels):
        sheet.paste(p, ((i % cols) * (w + 14), (i // cols) * (h + 14)))
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path)


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    PROOF.mkdir(parents=True, exist_ok=True)
    with Image.open(SOURCE) as src:
        fused = src.convert("RGB")
    arr = np.asarray(fused)

    metrics = []
    for x in [2048, 4096, 6144]:
        metrics.append(seam_metrics(arr, "vertical", x))
    for y in [2048, 4096, 6144]:
        metrics.append(seam_metrics(arr, "horizontal", y))

    strips = []
    for m in metrics:
        coord = int(m["coord"])
        orientation = str(m["orientation"])
        if orientation == "vertical":
            box = (coord - 256, 0, coord + 256, fused.height)
        else:
            box = (0, coord - 256, fused.width, coord + 256)
        crop = fused.crop(box)
        path = PROOF / f"wide_seam_{orientation}_{coord}.png"
        crop.save(path)
        strips.append({"id": f"{orientation}_{coord}", "path": str(path), "sha256": sha256(path), "box": list(box), "metrics": m})

    pair_specs = [
        ("pair_v2048_upper", (1536, 1024, 2560, 2048)),
        ("pair_v4096_center", (3584, 3584, 4608, 4608)),
        ("pair_v6144_lower", (5632, 5632, 6656, 6656)),
        ("pair_h2048_left", (1024, 1536, 2048, 2560)),
        ("pair_h4096_center", (3584, 3584, 4608, 4608)),
        ("pair_h6144_right", (5632, 5632, 6656, 6656)),
    ]
    pair_records = []
    for label, box in pair_specs:
        crop = fused.crop(box)
        path = PROOF / f"adjacent_{label}_1024.png"
        crop.save(path)
        pair_records.append({"id": label, "path": str(path), "sha256": sha256(path), "box": list(box), "dimensions": [1024, 1024]})

    native_specs = [
        ("native512_v2048", (1792, 1792, 2304, 2304)),
        ("native512_v4096", (3840, 3840, 4352, 4352)),
        ("native512_v6144", (5888, 5888, 6400, 6400)),
        ("native1024_overview_center", (3072, 3072, 4096, 4096)),
        ("native1024_water_rocks", (6144, 2048, 7168, 3072)),
        ("native1024_forest_meadow", (1024, 4096, 2048, 5120)),
    ]
    native_records = []
    for label, box in native_specs:
        crop = fused.crop(box)
        path = PROOF / f"{label}.png"
        crop.save(path)
        native_records.append({"id": label, "path": str(path), "sha256": sha256(path), "box": list(box), "dimensions": [crop.width, crop.height]})

    grid = fused.resize((2048, 2048), Image.Resampling.LANCZOS)
    draw = ImageDraw.Draw(grid, "RGBA")
    for i in range(0, 2049, 128):
        draw.line((i, 0, i, 2048), fill=(255, 255, 255, 70), width=1)
        draw.line((0, i, 2048, i), fill=(255, 255, 255, 70), width=1)
    grid_path = PROOF / "reinforced_grid_reveal_512_overview_2048.png"
    grid.save(grid_path)

    contrast = ImageEnhance.Contrast(grid).enhance(1.55)
    contrast_path = PROOF / "reinforced_grid_reveal_contrast_2048.png"
    contrast.save(contrast_path)

    sheet_path = PROOF / "reinforced_validation_proof_sheet.png"
    contact_sheet(
        [
            ("grid reveal 512 overview", grid),
            ("contrast grid reveal", contrast),
            ("wide vertical seam x4096", Image.open(PROOF / "wide_seam_vertical_4096.png").convert("RGB")),
            ("wide horizontal seam y4096", Image.open(PROOF / "wide_seam_horizontal_4096.png").convert("RGB")),
            ("adjacent pair v4096 center", Image.open(PROOF / "adjacent_pair_v4096_center_1024.png").convert("RGB")),
            ("adjacent pair h4096 center", Image.open(PROOF / "adjacent_pair_h4096_center_1024.png").convert("RGB")),
            ("native512 seam detail", Image.open(PROOF / "native512_v4096.png").convert("RGB")),
            ("native1024 water rocks", Image.open(PROOF / "native1024_water_rocks.png").convert("RGB")),
        ],
        sheet_path,
    )

    overall = "PASS" if all(m["verdict"] == "PASS" for m in metrics) else "REVIEW_REQUIRED"
    report_md = OUT / "WAVE5_METHOD_8192_REINFORCED_SEAM_REVIEW.md"
    lines = [
        "# Wave5-Method 8192 Reinforced Seam Review",
        "",
        f"STATUS={overall}",
        "DATE=2026-07-17",
        "",
        "## Seam Metrics",
        "",
        "| Boundary | Ratio | Brightness Delta | Verdict |",
        "| --- | ---: | ---: | --- |",
    ]
    for m in metrics:
        lines.append(f"| {m['orientation']} {m['coord']} | {m['seam_to_reference_ratio']} | {m['brightness_delta_128px']} | {m['verdict']} |")
    lines.extend(
        [
            "",
            "## Gates",
            "",
            f"REINFORCED_SEAM_REVIEW={overall}",
            "READY_FOR_2500_TILES=NO",
            "READY_FOR_QA_BUILDERC=NO",
            "READY_FOR_UNITY_HANDOFF=NO",
            "MASTER_25600_AUTHORIZED=NO",
            "WAVE5_MODIFIED=NO",
        ]
    )
    report_md.write_text("\n".join(lines) + "\n", encoding="utf-8")

    manifest = {
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "source_fused_superpanel": str(SOURCE),
        "source_sha256": sha256(SOURCE),
        "status": overall,
        "seam_metrics": metrics,
        "wide_seam_strips": strips,
        "adjacent_pair_crops": pair_records,
        "native_crops": native_records,
        "grid_reveal": {"path": str(grid_path), "sha256": sha256(grid_path)},
        "contrast_grid_reveal": {"path": str(contrast_path), "sha256": sha256(contrast_path)},
        "proof_sheet": {"path": str(sheet_path), "sha256": sha256(sheet_path)},
        "no_2500_tiles_built": True,
        "unity_modified": False,
    }
    manifest_path = OUT / "wave5method_8192_reinforced_validation_manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    receipt = {
        "created_utc": manifest["created_utc"],
        "NEXT_STEP_STARTED": "YES",
        "REINFORCED_8192_VALIDATION_CREATED": "YES",
        "REINFORCED_SEAM_REVIEW": overall,
        "WIDE_SEAM_STRIPS_CREATED": len(strips),
        "ADJACENT_PAIR_CROPS_CREATED": len(pair_records),
        "NATIVE_CROPS_CREATED": len(native_records),
        "GRID_REVEAL_CREATED": "YES",
        "PROOF_SHEET_CREATED": "YES",
        "READY_FOR_2500_TILES": "NO",
        "READY_FOR_QA_BUILDERC": "NO",
        "READY_FOR_UNITY_HANDOFF": "NO",
        "MASTER_25600_AUTHORIZED": "NO",
        "WAVE5_MODIFIED": "NO",
        "manifest": str(manifest_path),
        "review": str(report_md),
        "proof_sheet": str(sheet_path),
    }
    (OUT / "WAVE5_METHOD_8192_REINFORCED_VALIDATION_RECEIPT.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
