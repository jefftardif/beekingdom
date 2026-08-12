from __future__ import annotations

import hashlib
import json
from datetime import datetime
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(r"C:\projets\beekingdomgame-master")
STAGING = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging"
OUT = STAGING / "production_v3aa_scale_route_from_v3z"
PROOF = OUT / "proof"
REFS = OUT / "references"
CROPS = OUT / "reference_crops"
COMM = ROOT / "Docs" / "WorldMapCommunication"

V3Z = STAGING / "production_v3z_2d_single_canvas_grid_scale_bridge"
V3Z_SOURCE = V3Z / "source" / "v3z_2d_single_canvas_native.png"
V3Z_PROOF = V3Z / "proof" / "v3z_2d_single_canvas_grid_scale_bridge_proof_sheet.png"
V3Z_RECEIPT = V3Z / "V3Z_2D_SINGLE_CANVAS_GRID_SCALE_BRIDGE_RECEIPT.json"


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


def write_route(route_path: Path) -> None:
    route_path.write_text(
        "\n".join(
            [
                "# V3AA Scale Route From V3Z",
                "",
                "## Starting Point",
                "",
                "V3Z validates the correct route at proof scale: one continuous 2D canvas, deterministic grid cut, vertical and horizontal seams pass, premium detail passes.",
                "",
                "## Production Requirement",
                "",
                "- Final target: 50 x 50 tiles.",
                "- Tile size: 512 x 512.",
                "- Required continuous source field: 25600 x 25600 native pixels.",
                "- V3Z proof source: 1254 x 1254 native pixels.",
                "- Scale gap: about 20.4x per axis, about 416.9x total pixel area.",
                "",
                "## Non-Negotiable Route",
                "",
                "The V3Y/V3Z method should continue only if the source remains one continuous native canvas before cutting. Independent quadrant generation, collage, seam painting, or simple upscale does not qualify.",
                "",
                "## Next Minimal Valid Test",
                "",
                "1. Produce one larger native square canvas in the same V3Z visual style, ideally at least 4096 x 4096; stronger target 8192 x 8192.",
                "2. Deterministically cut it into a 4x4 or 8x8 grid.",
                "3. Run vertical and horizontal seam stress crops at 512 and 1024 across every internal seam family.",
                "4. Pass detail review at 100% crops before any package expansion.",
                "5. Only after a native large-canvas test passes should a candidate 50x50 tile package be considered.",
                "",
                "## Current Block",
                "",
                "The current local image generation path has only produced proof-scale native sources for square maps. It has not demonstrated a true 4096/8192/25600 native continuous square source. Therefore V3AA defines the route and prototype evidence, but production-scale readiness remains NO.",
                "",
                "## Gates",
                "",
                "- No 2500 tiles.",
                "- No Unity handoff.",
                "- No canonical swap.",
                "- No master 25600 claim.",
                "",
            ]
        ),
        encoding="utf-8",
    )


def make_scale_proof(src: Image.Image, out_path: Path) -> None:
    sheet = Image.new("RGB", (4096, 3072), (24, 26, 24))
    draw = ImageDraw.Draw(sheet)
    label(draw, (90, 65), "V3AA scale route from V3Z", 46)
    label(draw, (90, 135), "Keep single-canvas 2D route; block production until native large canvas exists.", 27)

    overview = src.copy()
    overview.thumbnail((1100, 1100), Image.Resampling.LANCZOS)
    sheet.paste(overview, (90, 240))
    label(draw, (90, 205), f"Current validated V3Z proof source: {src.width} x {src.height}", 24)

    # Requirement diagram: proof, bridge test, production target.
    x0, y0 = 1420, 260
    sizes = [
        ("V3Z proof", 1254, 220, (110, 185, 210)),
        ("minimal bridge", 4096, 520, (210, 190, 90)),
        ("strong bridge", 8192, 780, (220, 150, 80)),
        ("50x50 target", 25600, 1120, (235, 90, 70)),
    ]
    for name, px, box, color in sizes:
        draw.rectangle((x0, y0, x0 + box, y0 + box), outline=color, width=6)
        label(draw, (x0 + 18, y0 + 18), f"{name}: {px}px", 25)
        y0 += box + 70
        if y0 > 2450:
            x0 += 1280
            y0 = 260

    label(draw, (90, 1420), "Route decision", 32)
    lines = [
        "V3AA_ROUTE_DEFINED=YES",
        "V3AA_PROTOTYPE_CREATED=YES",
        "V3AA_DETAIL_PASS=YES from V3Z proof crops",
        "V3AA_SEAM_PASS=YES from V3Z 2D internal seams",
        "V3AA_PRODUCTION_SCALE_READY=NO",
        "V3AA_FULL_TILE_PACKAGE_CREATED=NO",
        "READY_FOR_UNITY_HANDOFF=NO",
    ]
    for i, line in enumerate(lines):
        label(draw, (90, 1495 + i * 72), line, 25, fill=(255, 220, 140))

    label(draw, (90, 2180), "Next valid proof: one native 4096/8192 square canvas, then 4x4 or 8x8 deterministic grid cut.", 28)
    label(draw, (90, 2255), "Rejected: upscale-only, independent quadrants, seam repair hiding collage, old blurred route.", 28, fill=(255, 190, 160))

    out_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(out_path)


def main() -> None:
    for d in (OUT, PROOF, REFS, CROPS, COMM):
        d.mkdir(parents=True, exist_ok=True)

    src_copy = REFS / "v3z_validated_2d_single_canvas_reference.png"
    proof_copy = REFS / "v3z_validated_2d_grid_proof_sheet.png"
    receipt_copy = REFS / "v3z_validated_receipt.json"
    copy(V3Z_SOURCE, src_copy)
    copy(V3Z_PROOF, proof_copy)
    copy(V3Z_RECEIPT, receipt_copy)

    src = Image.open(src_copy).convert("RGB")
    crop_names = [
        V3Z / "vertical_seam_crops_512" / "v3z_vertical_seam512_01.png",
        V3Z / "vertical_seam_crops_512" / "v3z_vertical_seam512_02.png",
        V3Z / "vertical_seam_crops_512" / "v3z_vertical_seam512_03.png",
        V3Z / "horizontal_seam_crops_512" / "v3z_horizontal_seam512_01.png",
        V3Z / "horizontal_seam_crops_512" / "v3z_horizontal_seam512_02.png",
        V3Z / "horizontal_seam_crops_512" / "v3z_horizontal_seam512_03.png",
    ]
    copied_crops = []
    for p in crop_names:
        dst = CROPS / p.name
        copy(p, dst)
        copied_crops.append(dst)

    route_path = OUT / "route.md"
    write_route(route_path)
    proof_path = PROOF / "v3aa_scale_route_from_v3z_proof_sheet.png"
    make_scale_proof(src, proof_path)

    gates = {
        "ACTIVE_WORK_RESUMED": "YES",
        "V3AA_ROUTE_DEFINED": "YES",
        "V3AA_PROTOTYPE_CREATED": "YES",
        "V3AA_DETAIL_PASS": "YES",
        "V3AA_SEAM_PASS": "YES",
        "V3AA_PRODUCTION_SCALE_READY": "NO",
        "V3AA_FULL_TILE_PACKAGE_CREATED": "NO",
        "READY_FOR_QA_BUILDERC": "NO",
        "READY_FOR_UNITY_HANDOFF": "NO",
    }
    receipt = {
        "artifact": "V3AA_SCALE_ROUTE_FROM_V3Z",
        "created_at": datetime.now().isoformat(timespec="seconds"),
        "folder": str(OUT),
        "route": str(route_path),
        "proof_sheet": str(proof_path),
        "references": {
            "v3z_source": str(src_copy),
            "v3z_proof": str(proof_copy),
            "v3z_receipt": str(receipt_copy),
        },
        "prototype_basis": "V3Z 2D single-canvas proof scale: deterministic 2x2 grid cut from one 1254x1254 native source.",
        "reference_crops": [str(p) for p in copied_crops],
        "production_requirement": {
            "tile_grid": [50, 50],
            "tile_size": [512, 512],
            "required_native_source": [25600, 25600],
            "current_validated_native_source": [src.width, src.height],
            "axis_scale_gap": round(25600 / src.width, 3),
            "area_scale_gap": round((25600 * 25600) / (src.width * src.height), 3),
        },
        "next_valid_test": "One native 4096x4096 or 8192x8192 single canvas in V3Z style, deterministic 4x4/8x8 cut, all internal seam stress crops pass.",
        "verdict": "Route defined and proof-scale prototype retained; production-scale ready remains NO because no native scalable single-canvas source has been demonstrated.",
        "hashes": {
            "route_sha256": sha256(route_path),
            "proof_sha256": sha256(proof_path),
            "v3z_source_sha256": sha256(src_copy),
        },
        "gates": gates,
    }
    receipt_path = OUT / "V3AA_SCALE_ROUTE_FROM_V3Z_RECEIPT.json"
    receipt_path.write_text(json.dumps(receipt, indent=2), encoding="utf-8")

    checkpoint_path = OUT / "V3AA_SCALE_ROUTE_FROM_V3Z_CHECKPOINT.md"
    checkpoint_path.write_text(
        "\n".join(
            [
                "# V3AA Scale Route From V3Z",
                "",
                "V3AA preserves the V3Z single-canvas 2D route and defines the next non-fake scale bridge.",
                "No production package was created.",
                "",
                f"- Route: `{route_path}`",
                f"- Proof: `{proof_path}`",
                f"- Receipt: `{receipt_path}`",
                "",
                "## Gates",
                *[f"- {k}={v}" for k, v in gates.items()],
                "",
            ]
        ),
        encoding="utf-8",
    )

    comm_path = COMM / "WorldMapCommunication_BeeKingdomWave6_V3AAScaleRouteFromV3Z_2026-07-16.md"
    comm_path.write_text(
        "\n".join(
            [
                "# Bee Kingdom Wave6 50x50 - V3AA Scale Route From V3Z",
                "",
                "V3AA keeps the validated V3Z approach: one continuous source canvas, deterministic cuts, no independent quadrant collage.",
                "The route is defined, but production remains blocked until a native 4096/8192 bridge canvas, and eventually a 25600-scale source or equivalent verified scalable native method, exists.",
                "",
                f"- Folder: `{OUT}`",
                f"- Route: `{route_path}`",
                f"- Proof: `{proof_path}`",
                f"- Receipt: `{receipt_path}`",
                "",
                *[f"- {k}={v}" for k, v in gates.items()],
                "",
            ]
        ),
        encoding="utf-8",
    )

    print(route_path)
    print(proof_path)
    print(checkpoint_path)
    print(receipt_path)
    print(comm_path)


if __name__ == "__main__":
    main()
