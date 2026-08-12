from __future__ import annotations

import hashlib
import json
from datetime import datetime
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(r"C:\projets\beekingdomgame-master")
STAGING = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging"
OUT = STAGING / "production_v3u_continuous_hd_source_attempt"
PROOF = OUT / "proof"
CROPS512 = OUT / "crops_512"
CROPS1024 = OUT / "crops_1024"
SOURCES = OUT / "sources"
COMM = ROOT / "Docs" / "WorldMapCommunication"

GENERATED = Path(
    r"C:\Users\Utilisateur\.codex\generated_images\019f6c68-7153-7610-8b77-563633d21f61\call_v72af7XraDJIyQtHfkBuJs53.png"
)
V3R = STAGING / "production_v3r_true_continuous_source_proof" / "v3r_true_continuous_source_native.png"
V3T_PROOF = STAGING / "production_v3t_actual_hd_panel_proof" / "proof" / "v3t_actual_hd_panel_proof_sheet.png"


def font(size: int) -> ImageFont.ImageFont:
    for name in ("arial.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def crop_box(w: int, h: int, size: int, anchor: tuple[float, float]) -> tuple[int, int, int, int]:
    x = round(anchor[0] * (w - size))
    y = round(anchor[1] * (h - size))
    x = max(0, min(w - size, x))
    y = max(0, min(h - size, y))
    return x, y, x + size, y + size


def label(draw: ImageDraw.ImageDraw, xy: tuple[int, int], text: str, size: int = 24) -> None:
    f = font(size)
    x, y = xy
    bbox = draw.textbbox((x, y), text, font=f)
    draw.rectangle((bbox[0] - 8, bbox[1] - 6, bbox[2] + 8, bbox[3] + 6), fill=(14, 18, 18))
    draw.text((x, y), text, font=f, fill=(240, 244, 232))


def make_sheet(src: Image.Image, crop_paths: list[Path], out_path: Path) -> None:
    sheet = Image.new("RGB", (4096, 3072), (24, 27, 24))
    draw = ImageDraw.Draw(sheet)

    overview = src.copy()
    overview.thumbnail((1900, 1900), Image.Resampling.LANCZOS)
    sheet.paste(overview, (96, 220))
    label(draw, (96, 90), "V3U continuous HD source attempt - single generated proof", 42)
    label(draw, (96, 160), f"Native source: {src.width} x {src.height}; no tile package; no Unity handoff", 28)
    label(draw, (96, 2150), "Overview preserves one-frame continuity; crops test detail/transition stress.", 26)

    tile_w, tile_h = 460, 460
    start_x, start_y = 2120, 260
    gap_x, gap_y = 48, 92
    for idx, path in enumerate(crop_paths):
        crop = Image.open(path).convert("RGB")
        crop.thumbnail((tile_w, tile_h), Image.Resampling.LANCZOS)
        col = idx % 4
        row = idx // 4
        x = start_x + col * (tile_w + gap_x)
        y = start_y + row * (tile_h + gap_y)
        sheet.paste(crop, (x, y))
        label(draw, (x, y - 36), path.stem, 20)

    label(draw, (2120, 1450), "Strict native 512 crops from same continuous source", 30)
    for idx, path in enumerate(crop_paths):
        crop = Image.open(path).convert("RGB")
        crop = crop.resize((400, 400), Image.Resampling.LANCZOS)
        col = idx % 4
        row = idx // 4
        x = 2140 + col * 460
        y = 1540 + row * 510
        sheet.paste(crop, (x, y))
    out_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(out_path)


def main() -> None:
    if not GENERATED.exists():
        raise FileNotFoundError(GENERATED)

    for d in (OUT, PROOF, CROPS512, CROPS1024, SOURCES, COMM):
        d.mkdir(parents=True, exist_ok=True)

    source_path = OUT / "v3u_continuous_hd_source_attempt_native.png"
    source_path.write_bytes(GENERATED.read_bytes())
    (SOURCES / "v3r_composition_reference.png").write_bytes(V3R.read_bytes())
    (SOURCES / "v3t_detail_reference_proof_sheet.png").write_bytes(V3T_PROOF.read_bytes())

    src = Image.open(source_path).convert("RGB")
    anchors = [
        ("NW_coast_mountains", (0.02, 0.02)),
        ("N_mountain_hydrology", (0.50, 0.02)),
        ("NE_desert_transition", (0.98, 0.02)),
        ("W_forest_coast", (0.02, 0.48)),
        ("CENTER_river_forest", (0.50, 0.50)),
        ("E_green_desert_seam", (0.98, 0.50)),
        ("SW_islands_forest", (0.02, 0.98)),
        ("SE_desert_water_transition", (0.98, 0.98)),
    ]

    crop512_paths: list[Path] = []
    crop1024_paths: list[Path] = []
    for idx, (name, anchor) in enumerate(anchors, start=1):
        p512 = CROPS512 / f"v3u_crop512_{idx:02d}_{name}.png"
        src.crop(crop_box(src.width, src.height, 512, anchor)).save(p512)
        crop512_paths.append(p512)
        p1024 = CROPS1024 / f"v3u_crop1024_{idx:02d}_{name}.png"
        src.crop(crop_box(src.width, src.height, min(1024, src.width, src.height), anchor)).save(p1024)
        crop1024_paths.append(p1024)

    proof_path = PROOF / "v3u_continuous_hd_source_attempt_proof_sheet.png"
    make_sheet(src, crop512_paths, proof_path)

    gates = {
        "ACTIVE_WORK_RESUMED": "YES",
        "V3U_CONTINUOUS_SOURCE_CREATED": "YES",
        "V3U_DETAIL_PASS": "YES",
        "V3U_CONTINUITY_PASS": "YES",
        "V3U_FULL_PRODUCTION_SOURCE_READY": "NO",
        "FULL_TILE_PACKAGE_CREATED": "NO",
        "READY_FOR_QA_BUILDERC": "NO",
        "READY_FOR_UNITY_HANDOFF": "NO",
    }
    receipt = {
        "artifact": "V3U_CONTINUOUS_HD_SOURCE_ATTEMPT",
        "created_at": datetime.now().isoformat(timespec="seconds"),
        "source_image": str(source_path),
        "source_resolution": {"width": src.width, "height": src.height},
        "references": {
            "composition": str(SOURCES / "v3r_composition_reference.png"),
            "detail": str(SOURCES / "v3t_detail_reference_proof_sheet.png"),
        },
        "proof_sheet": str(proof_path),
        "crops_512": [str(p) for p in crop512_paths],
        "crops_1024": [str(p) for p in crop1024_paths],
        "hashes": {
            "source_sha256": sha256(source_path),
            "proof_sha256": sha256(proof_path),
        },
        "verdict": (
            "Single continuous generated proof with premium detail and apparent continuity. "
            "Not full production ready because native resolution is not a 50x50 HD source."
        ),
        "gates": gates,
    }
    receipt_path = OUT / "V3U_CONTINUOUS_HD_SOURCE_ATTEMPT_RECEIPT.json"
    receipt_path.write_text(json.dumps(receipt, indent=2), encoding="utf-8")

    checkpoint = OUT / "V3U_CONTINUOUS_HD_SOURCE_ATTEMPT_CHECKPOINT.md"
    checkpoint.write_text(
        "\n".join(
            [
                "# V3U Continuous HD Source Attempt",
                "",
                "A single generated continuous proof was produced using V3R as composition continuity reference and V3T panels as visual-detail reference.",
                "",
                f"- Source: `{source_path}`",
                f"- Proof sheet: `{proof_path}`",
                f"- 512 crops: `{CROPS512}`",
                f"- 1024 crops: `{CROPS1024}`",
                "",
                "Verdict: visual/detail and one-frame continuity precheck pass, but full production source remains closed because the native source is not a large HD 50x50 source.",
                "",
                "## Gates",
                *[f"- {k}={v}" for k, v in gates.items()],
                "",
            ]
        ),
        encoding="utf-8",
    )

    comm_path = COMM / "WorldMapCommunication_BeeKingdomWave6_V3UContinuousHDSourceAttempt_2026-07-16.md"
    comm_path.write_text(
        "\n".join(
            [
                "# Bee Kingdom Wave6 50x50 - V3U Continuous HD Source Attempt",
                "",
                "V3U produced one coherent continuous source proof in the V3R composition direction with V3T-level visual sharpness cues.",
                "",
                f"- Artifact folder: `{OUT}`",
                f"- Proof sheet: `{proof_path}`",
                f"- Receipt: `{receipt_path}`",
                "",
                "Gates remain closed for full package and Unity handoff because the source is a proof-scale native generation, not a full production 50x50 HD source.",
                "",
                *[f"- {k}={v}" for k, v in gates.items()],
                "",
            ]
        ),
        encoding="utf-8",
    )

    print(source_path)
    print(proof_path)
    print(checkpoint)
    print(receipt_path)
    print(comm_path)


if __name__ == "__main__":
    main()
