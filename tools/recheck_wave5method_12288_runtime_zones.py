from __future__ import annotations

import hashlib
import json
import math
import warnings
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageEnhance


Image.MAX_IMAGE_PIXELS = None
warnings.simplefilter("ignore", Image.DecompressionBombWarning)

ROOT = Path(r"C:\projets\beekingdomgame-master")
RUNTIME = ROOT / "Assets" / "BeeKingdom" / "Playground" / "Resources" / "WorldMapWave6Runtime" / "UIB_ImmenseContinuousMaster50x50_wave5method_12288_preview"
SOURCE = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_wave5method_restart_staging" / "scaleup_superpanel_12288x12288" / "wave5method_scaleup_superpanel_fused_12288x12288.png"
OUT = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_wave5method_restart_staging" / "unity_preview_recheck_12288"
PROOF = OUT / "proof"

ORIGIN_CHUNK_X = 7
ORIGIN_CHUNK_Y = 7
LOGICAL = 512
GUTTER = 2
RUNTIME_SIZE = 516
WORLD = 25600

ZONES = [
    {"id": "C54_09", "chunk_x": 54, "chunk_y": 9, "known": "former DEFECT-001 / R02C47"},
    {"id": "C53_26", "chunk_x": 53, "chunk_y": 26, "known": "former DEFECT-002 / R19C46"},
    {"id": "C52_52", "chunk_x": 52, "chunk_y": 52, "known": "user-suspect zone"},
    {"id": "C48_46", "chunk_x": 48, "chunk_y": 46, "known": "user-suspect zone"},
]


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest().upper()


def tile_path(row: int, col: int) -> Path:
    return RUNTIME / f"R{row:02d}C{col:02d}_g2.png"


def core_tile(row: int, col: int) -> Image.Image:
    path = tile_path(row, col)
    if not path.exists():
        raise FileNotFoundError(path)
    im = Image.open(path).convert("RGB")
    if im.size != (RUNTIME_SIZE, RUNTIME_SIZE):
        raise RuntimeError(f"{path.name} size {im.size}, expected 516x516")
    return im.crop((GUTTER, GUTTER, GUTTER + LOGICAL, GUTTER + LOGICAL))


def runtime_mosaic(center_row: int, center_col: int) -> Image.Image:
    out = Image.new("RGB", (LOGICAL * 3, LOGICAL * 3))
    for dy in range(-1, 2):
        for dx in range(-1, 2):
            row = max(0, min(49, center_row + dy))
            col = max(0, min(49, center_col + dx))
            out.paste(core_tile(row, col), ((dx + 1) * LOGICAL, (dy + 1) * LOGICAL))
    return out


def source_mosaic(source: Image.Image, center_row: int, center_col: int) -> Image.Image:
    sw, sh = source.size
    world_x0 = max(0, (center_col - 1) * LOGICAL)
    world_y0 = max(0, (center_row - 1) * LOGICAL)
    world_x1 = min(WORLD, (center_col + 2) * LOGICAL)
    world_y1 = min(WORLD, (center_row + 2) * LOGICAL)
    sx0 = world_x0 * sw / WORLD
    sy0 = world_y0 * sh / WORLD
    sx1 = world_x1 * sw / WORLD
    sy1 = world_y1 * sh / WORLD
    return source.resize((world_x1 - world_x0, world_y1 - world_y0), Image.Resampling.LANCZOS, box=(sx0, sy0, sx1, sy1))


def luma(arr: np.ndarray) -> np.ndarray:
    return arr[..., 0] * 0.2126 + arr[..., 1] * 0.7152 + arr[..., 2] * 0.0722


def seam_metric(mosaic: Image.Image, orientation: str, coord: int) -> dict[str, float | str]:
    lum = luma(np.asarray(mosaic).astype(np.float32))
    if orientation == "vertical":
        seam = np.abs(lum[:, coord] - lum[:, coord - 1])
        refs = []
        for x in [coord - 160, coord + 160]:
            if 1 <= x < lum.shape[1]:
                refs.append(np.abs(lum[:, x] - lum[:, x - 1]))
        a = lum[:, coord - 64:coord]
        b = lum[:, coord:coord + 64]
    else:
        seam = np.abs(lum[coord, :] - lum[coord - 1, :])
        refs = []
        for y in [coord - 160, coord + 160]:
            if 1 <= y < lum.shape[0]:
                refs.append(np.abs(lum[y, :] - lum[y - 1, :]))
        a = lum[coord - 64:coord, :]
        b = lum[coord:coord + 64, :]
    ref = np.concatenate([r.reshape(-1) for r in refs]) if refs else seam
    ratio = float(np.mean(seam) / max(np.mean(ref), 1e-6))
    brightness_delta = float(abs(np.mean(a) - np.mean(b)))
    verdict = "PASS" if ratio < 1.45 and brightness_delta < 5.5 else "REVIEW"
    return {
        "orientation": orientation,
        "coord": coord,
        "ratio": round(ratio, 4),
        "brightness_delta": round(brightness_delta, 4),
        "verdict": verdict,
    }


def zone_sheet(zone_id: str, source_img: Image.Image, runtime_img: Image.Image, diff_img: Image.Image, path: Path) -> None:
    labels = [("source direct crop", source_img), ("runtime reconstructed cores", runtime_img), ("abs diff amplified", diff_img)]
    thumbs = []
    for label, im in labels:
        thumb = im.resize((512, 512), Image.Resampling.LANCZOS)
        panel = Image.new("RGB", (512, 542), (18, 18, 18))
        panel.paste(thumb, (0, 30))
        draw = ImageDraw.Draw(panel)
        draw.rectangle((0, 0, 512, 26), fill=(0, 0, 0))
        draw.text((6, 7), f"{zone_id} {label}", fill=(255, 255, 255))
        thumbs.append(panel)
    sheet = Image.new("RGB", (512 * 3 + 20, 542), (12, 12, 12))
    for i, panel in enumerate(thumbs):
        sheet.paste(panel, (i * (512 + 10), 0))
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path)


def main() -> None:
    PROOF.mkdir(parents=True, exist_ok=True)
    source_sha = sha256(SOURCE)
    manifest_path = RUNTIME / "runtime_manifest.json"
    manifest_present = manifest_path.exists()
    render_fix = ROOT / "Docs" / "WorldMapAudit" / "Wave6_50x50_Wave5Method12288" / "UnityPreviewRenderFixReceipt.json"

    with Image.open(SOURCE) as src:
        source = src.convert("RGB")

    reports = []
    for zone in ZONES:
        row = zone["chunk_y"] - ORIGIN_CHUNK_Y
        col = zone["chunk_x"] - ORIGIN_CHUNK_X
        runtime = runtime_mosaic(row, col)
        direct = source_mosaic(source, row, col)
        if direct.size != runtime.size:
            direct = direct.resize(runtime.size, Image.Resampling.LANCZOS)
        diff = ImageChops.difference(direct, runtime)
        diff_amp = ImageEnhance.Contrast(diff).enhance(5.0)
        sheet_path = PROOF / f"{zone['id']}_source_vs_runtime_3x3.png"
        zone_sheet(zone["id"], direct, runtime, diff_amp, sheet_path)

        metrics = []
        for coord in [512, 1024]:
            metrics.append(seam_metric(runtime, "vertical", coord))
            metrics.append(seam_metric(runtime, "horizontal", coord))
        arr_diff = np.asarray(diff).astype(np.float32)
        mean_diff = float(np.mean(arr_diff))
        max_diff = int(np.max(arr_diff))
        verdict = "PASS_PACKAGE_SOURCE_RUNTIME" if all(m["verdict"] == "PASS" for m in metrics) and mean_diff < 8.0 else "REVIEW_PACKAGE_SOURCE_RUNTIME"
        reports.append(
            {
                "zone": zone["id"],
                "known_context": zone["known"],
                "unity_chunk": [zone["chunk_x"], zone["chunk_y"]],
                "internal_center": f"R{row:02d}C{col:02d}",
                "runtime_tiles_reviewed": f"R{max(0,row-1):02d}..R{min(49,row+1):02d}/C{max(0,col-1):02d}..C{min(49,col+1):02d}",
                "source_vs_runtime_mean_abs_rgb_delta": round(mean_diff, 4),
                "source_vs_runtime_max_abs_rgb_delta": max_diff,
                "seam_metrics": metrics,
                "proof_sheet": str(sheet_path),
                "proof_sha256": sha256(sheet_path),
                "verdict": verdict,
            }
        )

    overall = "PASS_PACKAGE_SOURCE_RUNTIME" if all(r["verdict"] == "PASS_PACKAGE_SOURCE_RUNTIME" for r in reports) else "REVIEW_REQUIRED"
    checkpoint = OUT / "UIB_12288_UNITY_PREVIEW_RECHECK_CHECKPOINT.md"
    checkpoint.write_text(
        "# Wave5-Method 12288 Unity Preview Recheck Checkpoint\n\n"
        "STATUS=UIB_12288_UNITY_PREVIEW_RECHECK_STARTED\n"
        "DATE=2026-07-17\n\n"
        "## Scope\n\n"
        "Fresh package/source recheck only. No new 2500 tiles, no master 25600, no Unity modification, no QA/Builder-C.\n\n"
        "## Gates\n\n"
        "UIB_12288_UNITY_PREVIEW_RECHECK_STARTED=YES\n"
        "READY_FOR_QA_BUILDERC=NO\n"
        "READY_FOR_UNITY_HANDOFF=NO\n"
        "MASTER_25600_AUTHORIZED=NO\n",
        encoding="utf-8",
    )

    review_path = OUT / "UIB_12288_UNITY_PREVIEW_RECHECK_REPORT.md"
    lines = [
        "# Wave5-Method 12288 Unity Preview Recheck Report",
        "",
        f"STATUS={overall}",
        "DATE=2026-07-17",
        "",
        "## Inputs",
        "",
        f"- runtime_root: `{RUNTIME}`",
        f"- source: `{SOURCE}`",
        f"- source_sha256: `{source_sha}`",
        f"- runtime_manifest_present: `{manifest_present}`",
        f"- render_fix_receipt_present: `{render_fix.exists()}`",
        "",
        "## Zone Verdicts",
        "",
        "| Unity Zone | Internal Center | Verdict | Mean Delta | Seam Verdicts |",
        "| --- | --- | --- | ---: | --- |",
    ]
    for r in reports:
        seam_summary = ", ".join(f"{m['orientation'][0]}{m['coord']}={m['verdict']}({m['ratio']})" for m in r["seam_metrics"])
        lines.append(f"| `{r['zone']}` | `{r['internal_center']}` | `{r['verdict']}` | {r['source_vs_runtime_mean_abs_rgb_delta']} | {seam_summary} |")
    lines.extend(
        [
            "",
            "## Interpretation",
            "",
            "This check compares the continuous 12288 source against the reconstructed runtime tile cores around the reported Unity zones. If these zone sheets read clean while Unity screenshots show seams/blocs, the likely source is render path/gutter/old-scene usage rather than the 12288 source image itself. If a zone sheet shows a hard seam, that zone remains package-source suspect.",
            "",
            "## Gates",
            "",
            "READY_FOR_QA_BUILDERC=NO",
            "READY_FOR_UNITY_HANDOFF=NO",
            "MASTER_25600_AUTHORIZED=NO",
        ]
    )
    review_path.write_text("\n".join(lines) + "\n", encoding="utf-8")

    receipt = {
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "UIB_12288_UNITY_PREVIEW_RECHECK_STARTED": "YES",
        "status": overall,
        "runtime_root": str(RUNTIME),
        "source": str(SOURCE),
        "source_sha256": source_sha,
        "runtime_manifest_present": "YES" if manifest_present else "NO",
        "runtime_validation_present": "YES" if (RUNTIME / "runtime_validation.json").exists() else "NO",
        "render_fix_receipt_present": "YES" if render_fix.exists() else "NO",
        "zones_checked": reports,
        "checkpoint": str(checkpoint),
        "report": str(review_path),
        "READY_FOR_QA_BUILDERC": "NO",
        "READY_FOR_UNITY_HANDOFF": "NO",
        "MASTER_25600_AUTHORIZED": "NO",
    }
    receipt_path = OUT / "UIB_12288_UNITY_PREVIEW_RECHECK_RECEIPT.json"
    receipt_path.write_text(json.dumps(receipt, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
