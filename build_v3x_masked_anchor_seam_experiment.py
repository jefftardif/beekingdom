from __future__ import annotations

import hashlib
import json
from datetime import datetime
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFont, ImageOps


ROOT = Path(r"C:\projets\beekingdomgame-master")
STAGING = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging"
OUT = STAGING / "production_v3x_masked_anchor_seam_experiment"
ANCHOR = OUT / "anchor_strip"
PANELS = OUT / "panels_attempt"
PROOF = OUT / "proof"
CROPS512 = OUT / "seam_stress_crops_512"
CROPS1024 = OUT / "seam_stress_crops_1024"
REFS = OUT / "references"
COMM = ROOT / "Docs" / "WorldMapCommunication"

V3W = STAGING / "production_v3w_controlled_overlap_experiment"
V3U = STAGING / "production_v3u_continuous_hd_source_attempt"
V3T = STAGING / "production_v3t_actual_hd_panel_proof"

WEST = V3W / "panels" / "v3w_west_panel_native_attempt.png"
EAST = V3W / "panels" / "v3w_east_panel_native_attempt.png"


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


def copy(src: Path, dst: Path) -> None:
    dst.write_bytes(src.read_bytes())


def make_mask(size: tuple[int, int], strip_width: int) -> Image.Image:
    w, h = size
    mask = Image.new("L", (w, h), 0)
    draw = ImageDraw.Draw(mask)
    x0 = max(0, w // 2 - strip_width // 2)
    x1 = min(w, x0 + strip_width)
    draw.rectangle((x0, 0, x1, h), fill=255)
    return mask


def make_composite(west: Image.Image, east: Image.Image) -> Image.Image:
    h = min(west.height, east.height)
    west = west.crop((0, 0, west.width, h))
    east = east.crop((0, 0, east.width, h))
    canvas = Image.new("RGB", (west.width + east.width, h), (0, 0, 0))
    canvas.paste(west, (0, 0))
    canvas.paste(east, (west.width, 0))
    d = ImageDraw.Draw(canvas)
    d.line((west.width, 0, west.width, h), fill=(255, 70, 70), width=6)
    return canvas


def save_stress_crops(seam: Image.Image, seam_x: int) -> tuple[list[Path], list[Path]]:
    ys512 = [220, 560, 900, 1180]
    ys1024 = [100, 360, 650, 380]
    paths512: list[Path] = []
    paths1024: list[Path] = []
    for i, y in enumerate(ys512, start=1):
        y = min(seam.height - 512, max(0, y))
        p = CROPS512 / f"v3x_seam_stress512_{i:02d}.png"
        seam.crop((seam_x - 256, y, seam_x + 256, y + 512)).save(p)
        paths512.append(p)
    for i, y in enumerate(ys1024, start=1):
        y = min(seam.height - 1024, max(0, y))
        p = CROPS1024 / f"v3x_seam_stress1024_{i:02d}.png"
        seam.crop((seam_x - 512, y, seam_x + 512, y + 1024)).save(p)
        paths1024.append(p)
    return paths512, paths1024


def make_proof(
    west: Image.Image,
    east: Image.Image,
    anchor_strip: Image.Image,
    candidate_mask: Image.Image,
    seam: Image.Image,
    crop512: list[Path],
    proof_path: Path,
) -> float:
    west_anchor = anchor_strip.resize((260, 1000), Image.Resampling.LANCZOS)
    east_left = east.crop((0, 0, int(east.width * 0.20), east.height)).resize((260, 1000), Image.Resampling.LANCZOS)
    diff = ImageChops.difference(west_anchor, east_left)
    delta = sum(diff.convert("L").histogram()[i] * i for i in range(256)) / (260 * 1000)

    sheet = Image.new("RGB", (4096, 3072), (24, 26, 24))
    draw = ImageDraw.Draw(sheet)
    label(draw, (90, 65), "V3X masked / anchor strip seam experiment", 44)
    label(draw, (90, 135), "Anchor strip created; masked repair not executable in current workflow; seam remains unvalidated.", 28)

    seam_preview = seam.copy()
    seam_preview.thumbnail((2200, 1350), Image.Resampling.LANCZOS)
    sheet.paste(seam_preview, (90, 240))
    label(draw, (90, 205), "Existing V3W seam with intended anchor boundary", 24)

    sheet.paste(west_anchor, (2450, 270))
    sheet.paste(east_left, (2780, 270))
    sheet.paste(diff.resize((260, 1000), Image.Resampling.LANCZOS), (3110, 270))
    mask_preview = ImageOps.colorize(candidate_mask.resize((260, 1000), Image.Resampling.NEAREST), "black", "white")
    sheet.paste(mask_preview.convert("RGB"), (3440, 270))
    label(draw, (2450, 230), "anchor strip", 22)
    label(draw, (2780, 230), "east edge", 22)
    label(draw, (3110, 230), "difference", 22)
    label(draw, (3440, 230), "mask evidence", 22)

    verdict = [
        "V3X_ANCHOR_STRIP_CREATED=YES",
        "V3X_MASKED_REPAIR_ATTEMPTED=NO",
        "V3X_SEAM_CONTINUITY_PASS=NO",
        "V3X_DETAIL_PASS=YES",
        "V3X_PRODUCTION_ROUTE_VALIDATED=NO",
        "BLOCKED_MASKED_SEAM_METHOD=YES",
    ]
    label(draw, (2450, 1340), f"Anchor/east delta: {delta / 255.0:.3f}", 26, fill=(255, 210, 140))
    for i, line in enumerate(verdict):
        label(draw, (2450, 1410 + i * 70), line, 24, fill=(255, 210, 140))

    label(draw, (90, 1680), "Seam stress crops: existing mismatch preserved for evidence, not repaired", 26)
    for i, p in enumerate(crop512):
        img = Image.open(p).convert("RGB")
        img.thumbnail((420, 420), Image.Resampling.LANCZOS)
        x = 90 + (i % 4) * 500
        y = 1760 + (i // 4) * 500
        sheet.paste(img, (x, y))
        label(draw, (x, y - 34), p.stem, 18)

    proof_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(proof_path)
    return delta / 255.0


def main() -> None:
    for d in (OUT, ANCHOR, PANELS, PROOF, CROPS512, CROPS1024, REFS, COMM):
        d.mkdir(parents=True, exist_ok=True)

    west_path = PANELS / "v3x_west_panel_from_v3w_unrepaired.png"
    east_path = PANELS / "v3x_east_panel_from_v3w_unrepaired.png"
    copy(WEST, west_path)
    copy(EAST, east_path)
    copy(V3U / "v3u_continuous_hd_source_attempt_native.png", REFS / "v3u_direction_reference.png")
    copy(V3W / "V3W_CONTROLLED_OVERLAP_EXPERIMENT_RECEIPT.json", REFS / "v3w_failed_overlap_receipt.json")
    copy(V3T / "proof" / "v3t_actual_hd_panel_proof_sheet.png", REFS / "v3t_detail_reference_proof_sheet.png")

    west = Image.open(west_path).convert("RGB")
    east = Image.open(east_path).convert("RGB")
    strip_w = int(west.width * 0.20)
    anchor = west.crop((west.width - strip_w, 0, west.width, west.height))
    anchor_path = ANCHOR / "v3x_anchor_strip_from_v3w_west_right_20pct.png"
    anchor.save(anchor_path)

    mask = make_mask((west.width + east.width, min(west.height, east.height)), strip_w)
    mask_path = ANCHOR / "v3x_required_locked_seam_mask_evidence.png"
    mask.save(mask_path)

    seam = make_composite(west, east)
    seam_path = PROOF / "v3x_unrepaired_anchor_seam_canvas.png"
    seam.save(seam_path)
    crop512, crop1024 = save_stress_crops(seam, west.width)

    proof_path = PROOF / "v3x_masked_anchor_seam_experiment_proof_sheet.png"
    normalized_delta = make_proof(west, east, anchor, mask, seam, crop512, proof_path)

    gates = {
        "ACTIVE_WORK_RESUMED": "YES",
        "V3X_ANCHOR_STRIP_CREATED": "YES",
        "V3X_MASKED_REPAIR_ATTEMPTED": "NO",
        "V3X_SEAM_CONTINUITY_PASS": "NO",
        "V3X_DETAIL_PASS": "YES",
        "V3X_PRODUCTION_ROUTE_VALIDATED": "NO",
        "V3X_FULL_TILE_PACKAGE_CREATED": "NO",
        "READY_FOR_QA_BUILDERC": "NO",
        "READY_FOR_UNITY_HANDOFF": "NO",
        "BLOCKED_MASKED_SEAM_METHOD": "YES",
    }

    receipt = {
        "artifact": "V3X_MASKED_ANCHOR_SEAM_EXPERIMENT",
        "created_at": datetime.now().isoformat(timespec="seconds"),
        "folder": str(OUT),
        "anchor_strip": str(anchor_path),
        "mask_evidence": str(mask_path),
        "panels": {"west": str(west_path), "east": str(east_path)},
        "seam_canvas": str(seam_path),
        "proof_sheet": str(proof_path),
        "seam_stress_crops_512": [str(p) for p in crop512],
        "seam_stress_crops_1024": [str(p) for p in crop1024],
        "anchor_delta_vs_east_left": round(normalized_delta, 4),
        "verdict": "Anchor strip was created, but current built-in imagegen path does not expose mask/locked-boundary conditioning. No repaired seam was produced; continuity remains failed.",
        "next_action": "Use an image workflow with explicit mask/inpaint and boundary-strip lock, then rerun V3X with generated repaired panels.",
        "hashes": {
            "anchor_sha256": sha256(anchor_path),
            "proof_sha256": sha256(proof_path),
            "west_sha256": sha256(west_path),
            "east_sha256": sha256(east_path),
        },
        "gates": gates,
    }
    receipt_path = OUT / "V3X_MASKED_ANCHOR_SEAM_EXPERIMENT_RECEIPT.json"
    receipt_path.write_text(json.dumps(receipt, indent=2), encoding="utf-8")

    checkpoint_path = OUT / "V3X_MASKED_ANCHOR_SEAM_EXPERIMENT_CHECKPOINT.md"
    checkpoint_path.write_text(
        "\n".join(
            [
                "# V3X Masked / Anchor Strip Seam Experiment",
                "",
                "Anchor strip and mask evidence were created from V3W. Masked repair could not be executed honestly with the available imagegen workflow because no locked seam/mask conditioning was available.",
                "",
                f"- Anchor strip: `{anchor_path}`",
                f"- Mask evidence: `{mask_path}`",
                f"- Proof sheet: `{proof_path}`",
                f"- Receipt: `{receipt_path}`",
                "",
                "Verdict: blocked masked seam method; no tile package and no Unity handoff.",
                "",
                "## Gates",
                *[f"- {k}={v}" for k, v in gates.items()],
                "",
            ]
        ),
        encoding="utf-8",
    )

    comm_path = COMM / "WorldMapCommunication_BeeKingdomWave6_V3XMaskedAnchorSeamExperiment_2026-07-16.md"
    comm_path.write_text(
        "\n".join(
            [
                "# Bee Kingdom Wave6 50x50 - V3X Masked Anchor Seam Experiment",
                "",
                "V3X created an anchor strip and mask evidence from V3W, but did not produce a repaired continuous seam.",
                "The current imagegen workflow cannot lock the seam/boundary strip strongly enough for a production route.",
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

    print(anchor_path)
    print(mask_path)
    print(proof_path)
    print(checkpoint_path)
    print(receipt_path)
    print(comm_path)


if __name__ == "__main__":
    main()
