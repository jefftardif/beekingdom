from __future__ import annotations

import hashlib
import json
from datetime import datetime
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFont


ROOT = Path(r"C:\projets\beekingdomgame-master")
STAGING = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging"
OUT = STAGING / "production_v3w_controlled_overlap_experiment"
PANELS = OUT / "panels"
PROOF = OUT / "proof"
CROPS512 = OUT / "seam_stress_crops_512"
CROPS1024 = OUT / "seam_stress_crops_1024"
REFS = OUT / "references"
COMM = ROOT / "Docs" / "WorldMapCommunication"

WEST_GEN = Path(r"C:\Users\Utilisateur\.codex\generated_images\019f6c68-7153-7610-8b77-563633d21f61\call_UulIDZDDLmgXlNAgiDt1hWih.png")
EAST_GEN = Path(r"C:\Users\Utilisateur\.codex\generated_images\019f6c68-7153-7610-8b77-563633d21f61\call_g4lHizVlrojfSNxbmD09kiY1.png")

V3U = STAGING / "production_v3u_continuous_hd_source_attempt"
V3V = STAGING / "production_v3v_production_scale_continuous_tile_route"
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


def edge_strip(img: Image.Image, side: str, width_ratio: float = 0.20) -> Image.Image:
    w, h = img.size
    sw = int(w * width_ratio)
    if side == "right":
        return img.crop((w - sw, 0, w, h))
    return img.crop((0, 0, sw, h))


def crop_centered(img: Image.Image, size: int, xcenter: int, ycenter: int) -> Image.Image:
    w, h = img.size
    x = max(0, min(w - size, xcenter - size // 2))
    y = max(0, min(h - size, ycenter - size // 2))
    return img.crop((x, y, x + size, y + size))


def make_seam_canvas(west: Image.Image, east: Image.Image) -> Image.Image:
    h = min(west.height, east.height)
    west = west.crop((0, 0, west.width, h))
    east = east.crop((0, 0, east.width, h))
    seam = Image.new("RGB", (west.width + east.width, h), (0, 0, 0))
    seam.paste(west, (0, 0))
    seam.paste(east, (west.width, 0))
    draw = ImageDraw.Draw(seam)
    x = west.width
    draw.line((x, 0, x, h), fill=(255, 80, 70), width=6)
    return seam


def make_proof_sheet(west: Image.Image, east: Image.Image, proof_path: Path) -> tuple[float, float]:
    seam = make_seam_canvas(west, east)
    west_strip = edge_strip(west, "right").resize((512, 1536), Image.Resampling.LANCZOS)
    east_strip = edge_strip(east, "left").resize((512, 1536), Image.Resampling.LANCZOS)
    diff = ImageChops.difference(west_strip, east_strip)
    stat = sum(diff.convert("L").histogram()[i] * i for i in range(256)) / (512 * 1536)
    edge_delta = stat / 255.0

    sheet = Image.new("RGB", (4096, 3072), (24, 26, 24))
    draw = ImageDraw.Draw(sheet)
    label(draw, (90, 70), "V3W controlled overlap experiment", 46)
    label(draw, (90, 140), "Two generated adjacent panels: detail works; boundary locking is not proven.", 28)

    seam_preview = seam.copy()
    seam_preview.thumbnail((2500, 1450), Image.Resampling.LANCZOS)
    sheet.paste(seam_preview, (90, 240))
    label(draw, (90, 205), "Side-by-side seam proof; red line marks intended west/east boundary", 24)

    sheet.paste(west_strip.resize((300, 900), Image.Resampling.LANCZOS), (2750, 300))
    sheet.paste(east_strip.resize((300, 900), Image.Resampling.LANCZOS), (3100, 300))
    sheet.paste(diff.resize((300, 900), Image.Resampling.LANCZOS), (3450, 300))
    label(draw, (2750, 260), "west right strip", 22)
    label(draw, (3100, 260), "east left strip", 22)
    label(draw, (3450, 260), "strip difference", 22)

    verdict = [
        "V3W_OVERLAP_PANELS_CREATED=YES",
        "V3W_SHARED_BOUNDARY_TESTED=YES",
        "V3W_SEAM_CONTINUITY_PASS=NO",
        "V3W_DETAIL_PASS=YES",
        "V3W_PRODUCTION_ROUTE_VALIDATED=NO",
        "BLOCKED_OVERLAP_METHOD=YES",
    ]
    label(draw, (2750, 1300), f"Boundary strip delta: {edge_delta:.3f}", 26, fill=(255, 210, 140))
    for idx, line in enumerate(verdict):
        label(draw, (2750, 1370 + idx * 70), line, 24, fill=(255, 210, 140))

    # Stress crop previews around seam.
    crop_y = [260, 610, 960, 1310]
    for idx, y in enumerate(crop_y, start=1):
        c = seam.crop((west.width - 256, y, west.width + 256, y + 512)).resize((360, 360), Image.Resampling.LANCZOS)
        x = 90 + (idx - 1) * 430
        sheet.paste(c, (x, 1850))
        label(draw, (x, 1815), f"seam crop {idx}", 20)
    for idx, y in enumerate(crop_y, start=5):
        c = seam.crop((west.width - 512, y, west.width + 512, y + 1024)).resize((360, 360), Image.Resampling.LANCZOS)
        x = 90 + (idx - 5) * 430
        sheet.paste(c, (x, 2350))
        label(draw, (x, 2315), f"1024 stress {idx-4}", 20)

    proof_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(proof_path)
    return edge_delta, stat


def main() -> None:
    for d in (OUT, PANELS, PROOF, CROPS512, CROPS1024, REFS, COMM):
        d.mkdir(parents=True, exist_ok=True)

    west_path = PANELS / "v3w_west_panel_native_attempt.png"
    east_path = PANELS / "v3w_east_panel_native_attempt.png"
    west_path.write_bytes(WEST_GEN.read_bytes())
    east_path.write_bytes(EAST_GEN.read_bytes())

    (REFS / "v3u_direction_reference.png").write_bytes((V3U / "v3u_continuous_hd_source_attempt_native.png").read_bytes())
    (REFS / "v3v_route_reference.md").write_bytes((V3V / "route.md").read_bytes())
    (REFS / "v3t_detail_reference_proof_sheet.png").write_bytes((V3T / "proof" / "v3t_actual_hd_panel_proof_sheet.png").read_bytes())

    west = Image.open(west_path).convert("RGB")
    east = Image.open(east_path).convert("RGB")
    seam = make_seam_canvas(west, east)
    seam_path = PROOF / "v3w_side_by_side_seam_canvas.png"
    seam.save(seam_path)

    crop_y = [260, 610, 960, 1310]
    crop512_paths: list[Path] = []
    crop1024_paths: list[Path] = []
    for idx, y in enumerate(crop_y, start=1):
        p = CROPS512 / f"v3w_seam_stress512_{idx:02d}.png"
        seam.crop((west.width - 256, y, west.width + 256, y + 512)).save(p)
        crop512_paths.append(p)
    for idx, y in enumerate(crop_y, start=1):
        p = CROPS1024 / f"v3w_seam_stress1024_{idx:02d}.png"
        seam.crop((west.width - 512, y, west.width + 512, y + 1024)).save(p)
        crop1024_paths.append(p)

    proof_path = PROOF / "v3w_controlled_overlap_experiment_proof_sheet.png"
    edge_delta, raw_delta = make_proof_sheet(west, east, proof_path)

    gates = {
        "ACTIVE_WORK_RESUMED": "YES",
        "V3W_OVERLAP_PANELS_CREATED": "YES",
        "V3W_SHARED_BOUNDARY_TESTED": "YES",
        "V3W_SEAM_CONTINUITY_PASS": "NO",
        "V3W_DETAIL_PASS": "YES",
        "V3W_PRODUCTION_ROUTE_VALIDATED": "NO",
        "V3W_FULL_TILE_PACKAGE_CREATED": "NO",
        "READY_FOR_QA_BUILDERC": "NO",
        "READY_FOR_UNITY_HANDOFF": "NO",
        "BLOCKED_OVERLAP_METHOD": "YES",
    }

    receipt = {
        "artifact": "V3W_CONTROLLED_OVERLAP_EXPERIMENT",
        "created_at": datetime.now().isoformat(timespec="seconds"),
        "folder": str(OUT),
        "panels": {
            "west": str(west_path),
            "east": str(east_path),
            "west_resolution": list(west.size),
            "east_resolution": list(east.size),
        },
        "proof_sheet": str(proof_path),
        "side_by_side_seam_canvas": str(seam_path),
        "seam_stress_crops_512": [str(p) for p in crop512_paths],
        "seam_stress_crops_1024": [str(p) for p in crop1024_paths],
        "boundary_metric": {
            "right_left_strip_mean_abs_delta": round(raw_delta, 3),
            "normalized_delta": round(edge_delta, 4),
            "interpretation": "High enough to reject strict locked-boundary continuity; visual style matches, exact seam does not.",
        },
        "verdict": "Two premium/detail panels were created, but the shared boundary is not locked and does not genuinely align. Production route remains unvalidated.",
        "next_action": "Use a tool/workflow that supports masked editing or explicit boundary-strip conditioning, then rerun the two-panel seam test.",
        "hashes": {
            "west_sha256": sha256(west_path),
            "east_sha256": sha256(east_path),
            "proof_sha256": sha256(proof_path),
        },
        "gates": gates,
    }
    receipt_path = OUT / "V3W_CONTROLLED_OVERLAP_EXPERIMENT_RECEIPT.json"
    receipt_path.write_text(json.dumps(receipt, indent=2), encoding="utf-8")

    checkpoint_path = OUT / "V3W_CONTROLLED_OVERLAP_EXPERIMENT_CHECKPOINT.md"
    checkpoint_path.write_text(
        "\n".join(
            [
                "# V3W Controlled Overlap Experiment",
                "",
                "Two adjacent native panel attempts were generated from V3U/V3T direction references.",
                "The panels are visually detailed, but the shared boundary was not actually lockable through the available generation path.",
                "",
                f"- West panel: `{west_path}`",
                f"- East panel: `{east_path}`",
                f"- Proof sheet: `{proof_path}`",
                f"- Receipt: `{receipt_path}`",
                "",
                "Verdict: blocked overlap method; do not scale to 50x50.",
                "",
                "## Gates",
                *[f"- {k}={v}" for k, v in gates.items()],
                "",
            ]
        ),
        encoding="utf-8",
    )

    comm_path = COMM / "WorldMapCommunication_BeeKingdomWave6_V3WControlledOverlapExperiment_2026-07-16.md"
    comm_path.write_text(
        "\n".join(
            [
                "# Bee Kingdom Wave6 50x50 - V3W Controlled Overlap Experiment",
                "",
                "V3W tested the smallest V3V production-scale method: two adjacent panels with a shared boundary.",
                "Result: panel detail is acceptable, but the boundary is not locked or genuinely continuous.",
                "",
                f"- Folder: `{OUT}`",
                f"- Proof: `{proof_path}`",
                f"- Receipt: `{receipt_path}`",
                "",
                "Next action: masked/boundary-conditioned generation or another workflow with explicit edge control.",
                "",
                *[f"- {k}={v}" for k, v in gates.items()],
                "",
            ]
        ),
        encoding="utf-8",
    )

    print(west_path)
    print(east_path)
    print(proof_path)
    print(checkpoint_path)
    print(receipt_path)
    print(comm_path)


if __name__ == "__main__":
    main()
