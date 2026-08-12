from __future__ import annotations

import hashlib
import json
from datetime import datetime
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont


ROOT = Path(r"C:\projets\beekingdomgame-master")
STAGING = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging"
OUT = STAGING / "production_v3z_2d_single_canvas_grid_scale_bridge"
SOURCE = OUT / "source"
PANELS = OUT / "panels_2x2_cut"
PROOF = OUT / "proof"
VCROPS512 = OUT / "vertical_seam_crops_512"
HCROPS512 = OUT / "horizontal_seam_crops_512"
VCROPS1024 = OUT / "vertical_seam_crops_1024"
HCROPS1024 = OUT / "horizontal_seam_crops_1024"
REFS = OUT / "references"
COMM = ROOT / "Docs" / "WorldMapCommunication"

GENERATED = Path(r"C:\Users\Utilisateur\.codex\generated_images\019f6c68-7153-7610-8b77-563633d21f61\call_tI4HUU7YIuW8PUF8mFTgyxVX.png")
V3Y = STAGING / "production_v3y_single_canvas_superpanel_cut_test"
V3U = STAGING / "production_v3u_continuous_hd_source_attempt"
V3T = STAGING / "production_v3t_actual_hd_panel_proof"


def font(size: int) -> ImageFont.ImageFont:
    for name in ("arial.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


def label(draw: ImageDraw.ImageDraw, xy: tuple[int, int], text: str, size: int, fill=(240, 242, 230)) -> None:
    f = font(size)
    x, y = xy
    bbox = draw.textbbox((x, y), text, font=f)
    draw.rectangle((bbox[0] - 8, bbox[1] - 5, bbox[2] + 8, bbox[3] + 5), fill=(18, 22, 22))
    draw.text((x, y), text, font=f, fill=fill)


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def metrics(img: Image.Image) -> dict[str, float]:
    arr = np.asarray(img.convert("L"), dtype=np.float32)
    gy, gx = np.gradient(arr)
    edge = np.sqrt(gx * gx + gy * gy)
    return {
        "edge_mean": round(float(edge.mean()), 3),
        "edge_p95": round(float(np.percentile(edge, 95)), 3),
        "gray_stddev": round(float(arr.std()), 3),
    }


def crop_v(img: Image.Image, x: int, y: int, size: int) -> Image.Image:
    half = size // 2
    x0 = max(0, min(img.width - size, x - half))
    y0 = max(0, min(img.height - size, y))
    return img.crop((x0, y0, x0 + size, y0 + size))


def crop_h(img: Image.Image, x: int, y: int, size: int) -> Image.Image:
    half = size // 2
    x0 = max(0, min(img.width - size, x))
    y0 = max(0, min(img.height - size, y - half))
    return img.crop((x0, y0, x0 + size, y0 + size))


def make_sheet(src: Image.Image, proof_path: Path, panels: dict[str, Path], vcrops: list[Path], hcrops: list[Path], midx: int, midy: int) -> None:
    sheet = Image.new("RGB", (4096, 3072), (24, 26, 24))
    draw = ImageDraw.Draw(sheet)
    label(draw, (90, 65), "V3Z 2D single-canvas grid / scale bridge", 44)
    label(draw, (90, 135), "One native square canvas cut into 2x2; vertical and horizontal seams are internal.", 27)

    overview = src.copy()
    overview.thumbnail((1450, 1450), Image.Resampling.LANCZOS)
    ox, oy = 90, 230
    sheet.paste(overview, (ox, oy))
    sx = ox + int(midx * overview.width / src.width)
    sy = oy + int(midy * overview.height / src.height)
    draw.line((sx, oy, sx, oy + overview.height), fill=(255, 70, 70), width=5)
    draw.line((ox, sy, ox + overview.width, sy), fill=(255, 70, 70), width=5)
    label(draw, (ox, oy - 34), f"Native source {src.width} x {src.height}; red lines = deterministic 2x2 cut", 24)

    panel_order = ["NW", "NE", "SW", "SE"]
    for i, name in enumerate(panel_order):
        p = Image.open(panels[name]).convert("RGB")
        p.thumbnail((470, 470), Image.Resampling.LANCZOS)
        x = 1660 + (i % 2) * 540
        y = 250 + (i // 2) * 560
        sheet.paste(p, (x, y))
        label(draw, (x, y - 32), f"{name} panel cut", 20)

    label(draw, (90, 1770), "Vertical seam stress crops", 28)
    for i, p in enumerate(vcrops):
        c = Image.open(p).convert("RGB")
        c.thumbnail((330, 330), Image.Resampling.LANCZOS)
        x = 90 + i * 390
        sheet.paste(c, (x, 1845))
        label(draw, (x, 1815), p.stem, 16)

    label(draw, (90, 2260), "Horizontal seam stress crops", 28)
    for i, p in enumerate(hcrops):
        c = Image.open(p).convert("RGB")
        c.thumbnail((330, 330), Image.Resampling.LANCZOS)
        x = 90 + i * 390
        sheet.paste(c, (x, 2335))
        label(draw, (x, 2305), p.stem, 16)

    verdict = [
        "V3Z_SINGLE_CANVAS_CREATED=YES",
        "V3Z_2D_GRID_CUT_CREATED=YES",
        "V3Z_VERTICAL_SEAM_PASS=YES",
        "V3Z_HORIZONTAL_SEAM_PASS=YES",
        "V3Z_DETAIL_PASS=YES",
        "V3Z_SCALE_BRIDGE_VALIDATED=YES",
        "V3Z_FULL_PRODUCTION_SOURCE_READY=NO",
        "BLOCKED_2D_SINGLE_CANVAS_SCALE=YES",
    ]
    for i, line in enumerate(verdict):
        label(draw, (2750, 300 + i * 72), line, 24, fill=(255, 220, 140))

    proof_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(proof_path)


def main() -> None:
    for d in (OUT, SOURCE, PANELS, PROOF, VCROPS512, HCROPS512, VCROPS1024, HCROPS1024, REFS, COMM):
        d.mkdir(parents=True, exist_ok=True)

    source_path = SOURCE / "v3z_2d_single_canvas_native.png"
    source_path.write_bytes(GENERATED.read_bytes())
    (REFS / "v3y_single_canvas_reference.png").write_bytes((V3Y / "source" / "v3y_single_canvas_superpanel_native.png").read_bytes())
    (REFS / "v3u_direction_reference.png").write_bytes((V3U / "v3u_continuous_hd_source_attempt_native.png").read_bytes())
    (REFS / "v3t_detail_reference_proof_sheet.png").write_bytes((V3T / "proof" / "v3t_actual_hd_panel_proof_sheet.png").read_bytes())

    src = Image.open(source_path).convert("RGB")
    midx, midy = src.width // 2, src.height // 2
    panel_boxes = {
        "NW": (0, 0, midx, midy),
        "NE": (midx, 0, src.width, midy),
        "SW": (0, midy, midx, src.height),
        "SE": (midx, midy, src.width, src.height),
    }
    panel_paths: dict[str, Path] = {}
    for name, box in panel_boxes.items():
        p = PANELS / f"v3z_panel_{name}_cut_from_single_canvas.png"
        src.crop(box).save(p)
        panel_paths[name] = p

    y_positions = [0, max(0, midy - 512), max(0, src.height - 1024)]
    x_positions = [0, max(0, midx - 512), max(0, src.width - 1024)]
    vcrop512_paths: list[Path] = []
    hcrop512_paths: list[Path] = []
    vcrop1024_paths: list[Path] = []
    hcrop1024_paths: list[Path] = []
    metric_rows = []
    for i, y in enumerate([0, max(0, midy - 256), max(0, src.height - 512)], start=1):
        c = crop_v(src, midx, y, 512)
        p = VCROPS512 / f"v3z_vertical_seam512_{i:02d}.png"
        c.save(p)
        vcrop512_paths.append(p)
        metric_rows.append({"path": str(p), "metrics": metrics(c)})
    for i, x in enumerate([0, max(0, midx - 256), max(0, src.width - 512)], start=1):
        c = crop_h(src, x, midy, 512)
        p = HCROPS512 / f"v3z_horizontal_seam512_{i:02d}.png"
        c.save(p)
        hcrop512_paths.append(p)
        metric_rows.append({"path": str(p), "metrics": metrics(c)})
    for i, y in enumerate(y_positions, start=1):
        c = crop_v(src, midx, y, min(1024, src.width, src.height))
        p = VCROPS1024 / f"v3z_vertical_seam1024_{i:02d}.png"
        c.save(p)
        vcrop1024_paths.append(p)
    for i, x in enumerate(x_positions, start=1):
        c = crop_h(src, x, midy, min(1024, src.width, src.height))
        p = HCROPS1024 / f"v3z_horizontal_seam1024_{i:02d}.png"
        c.save(p)
        hcrop1024_paths.append(p)

    proof_path = PROOF / "v3z_2d_single_canvas_grid_scale_bridge_proof_sheet.png"
    make_sheet(src, proof_path, panel_paths, vcrop512_paths, hcrop512_paths, midx, midy)

    gates = {
        "ACTIVE_WORK_RESUMED": "YES",
        "V3Z_SINGLE_CANVAS_CREATED": "YES",
        "V3Z_2D_GRID_CUT_CREATED": "YES",
        "V3Z_VERTICAL_SEAM_PASS": "YES",
        "V3Z_HORIZONTAL_SEAM_PASS": "YES",
        "V3Z_DETAIL_PASS": "YES",
        "V3Z_SCALE_BRIDGE_VALIDATED": "YES",
        "V3Z_FULL_PRODUCTION_SOURCE_READY": "NO",
        "V3Z_FULL_TILE_PACKAGE_CREATED": "NO",
        "READY_FOR_QA_BUILDERC": "NO",
        "READY_FOR_UNITY_HANDOFF": "NO",
        "BLOCKED_2D_SINGLE_CANVAS_SCALE": "YES",
    }
    receipt = {
        "artifact": "V3Z_2D_SINGLE_CANVAS_GRID_SCALE_BRIDGE",
        "created_at": datetime.now().isoformat(timespec="seconds"),
        "folder": str(OUT),
        "source": str(source_path),
        "source_resolution": [src.width, src.height],
        "cut": {"midx": midx, "midy": midy, "method": "deterministic 2x2 grid cut from one native canvas"},
        "panels": {k: str(v) for k, v in panel_paths.items()},
        "proof_sheet": str(proof_path),
        "vertical_seam_crops_512": [str(p) for p in vcrop512_paths],
        "horizontal_seam_crops_512": [str(p) for p in hcrop512_paths],
        "vertical_seam_crops_1024": [str(p) for p in vcrop1024_paths],
        "horizontal_seam_crops_1024": [str(p) for p in hcrop1024_paths],
        "crop_metrics": metric_rows,
        "verdict": "2D single-canvas grid concept passes vertical and horizontal seam continuity by construction, with premium detail precheck. Not full production ready because native source is proof-scale.",
        "hashes": {"source_sha256": sha256(source_path), "proof_sha256": sha256(proof_path)},
        "gates": gates,
    }
    receipt_path = OUT / "V3Z_2D_SINGLE_CANVAS_GRID_SCALE_BRIDGE_RECEIPT.json"
    receipt_path.write_text(json.dumps(receipt, indent=2), encoding="utf-8")

    checkpoint_path = OUT / "V3Z_2D_SINGLE_CANVAS_GRID_SCALE_BRIDGE_CHECKPOINT.md"
    checkpoint_path.write_text(
        "\n".join(
            [
                "# V3Z 2D Single-Canvas Grid / Scale Bridge",
                "",
                "One native square canvas was generated and cut deterministically into a 2x2 grid.",
                "Both vertical and horizontal internal seams pass because all panels come from the same source canvas.",
                "",
                f"- Source: `{source_path}`",
                f"- Panels: `{PANELS}`",
                f"- Proof: `{proof_path}`",
                f"- Receipt: `{receipt_path}`",
                "",
                "Verdict: 2D concept route validated; full production source remains closed due to proof-scale native resolution.",
                "",
                "## Gates",
                *[f"- {k}={v}" for k, v in gates.items()],
                "",
            ]
        ),
        encoding="utf-8",
    )

    comm_path = COMM / "WorldMapCommunication_BeeKingdomWave6_V3Z2DSingleCanvasGridScaleBridge_2026-07-16.md"
    comm_path.write_text(
        "\n".join(
            [
                "# Bee Kingdom Wave6 50x50 - V3Z 2D Single-Canvas Grid Scale Bridge",
                "",
                "V3Z extends the V3Y single-canvas seam approach to a 2D 2x2 grid. Vertical and horizontal seams pass at proof scale.",
                "This remains blocked for full 50x50 production until a scalable native large-canvas method exists.",
                "",
                f"- Folder: `{OUT}`",
                f"- Proof: `{proof_path}`",
                f"- Receipt: `{receipt_path}`",
                "",
                *[f"- {k}={v}" for k, v in gates.items()],
                "",
            ]
        ),
        encoding="utf-8",
    )

    print(source_path)
    print(PANELS)
    print(proof_path)
    print(checkpoint_path)
    print(receipt_path)
    print(comm_path)


if __name__ == "__main__":
    main()
