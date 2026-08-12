from __future__ import annotations

import hashlib
import json
from datetime import datetime
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(r"C:\projets\beekingdomgame-master")
STAGING = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging"
OUT = STAGING / "production_v3ac_source_scale_route_search"
PROOF = OUT / "proof"
REFS = OUT / "references"
COMM = ROOT / "Docs" / "WorldMapCommunication"

V3Z = STAGING / "production_v3z_2d_single_canvas_grid_scale_bridge"
V3AA = STAGING / "production_v3aa_scale_route_from_v3z"
V3AB = STAGING / "production_v3ab_native_scale_capability_probe"

CANDIDATE_PATHS = [
    STAGING / "production_v3i_sharp_source_route" / "v3i_sharp_source_candidate_8192.png",
    STAGING / "production_v3e_reduced_candidate_package" / "v3e_reduced_candidate_8192.png",
    STAGING / "production_v3h_global_filtered_tile_package" / "v3h_global_filtered_source_8192.png",
    STAGING / "production_v3d_highres_worker" / "v3d_highres_prototype_8192.png",
    STAGING / "production_v3o_pictorial_source_proof" / "v3o_pictorial_source_proof_4096.png",
    STAGING / "production_v3k_large_style_candidate_review" / "v3k_large_style_candidate_4096_review.png",
    STAGING / "production_v3l_clean_source_review" / "v3l_clean_source_review_4096.png",
]


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


def image_size(path: Path) -> list[int] | None:
    if not path.exists():
        return None
    with Image.open(path) as img:
        return [img.width, img.height]


def write_route(path: Path) -> None:
    path.write_text(
        "\n".join(
            [
                "# V3AC Source Scale Route Search",
                "",
                "## Decision",
                "",
                "Stop generating more 1254-only proof images. V3Z remains the visual and seam reference, but the current local image path does not provide native >=4096 square output.",
                "",
                "## Local Capability Finding",
                "",
                "Large historical files exist locally, including 4096/8192/25600 assets, but they are not an acceptable V3Z-style native source route:",
                "",
                "- old 25600 masters are from rejected/soft routes and are not authorized as canonical",
                "- V3E/V3H are mechanical or reduced candidates with visual HD failures",
                "- V3I/V3K/V3L/V3O predate the V3Z single-canvas route and were not accepted as production-ready scalable sources",
                "- local scripts can crop, cut, audit, or upscale, but do not create true new native detail at 4096/8192/25600",
                "",
                "Therefore V3AC_LOCAL_NATIVE_GE_4096_AVAILABLE=NO for a valid V3Z-style continuous native source.",
                "",
                "## Required External Capability",
                "",
                "Supply or connect a generation/rendering workflow with these exact capabilities:",
                "",
                "1. Native square output at minimum 4096x4096, target 8192x8192, eventual 25600x25600 or a verified lossless scalable native tiling workflow.",
                "2. One continuous global canvas before cutting, not independent quadrant generation.",
                "3. V3Z/V3AB premium painterly terrain style: sharp forests, mountains, organic rivers/lakes, coast/islands, controlled desert transition.",
                "4. Deterministic grid cutting after source generation: 4x4 for 4096, 8x8 for 8192, 50x50 only after scale bridge passes.",
                "5. Mandatory QA: 512/1024 seam stress crops across all internal vertical and horizontal seams, plus 100% detail crops.",
                "",
                "## Next Action",
                "",
                "Acquire or enable a native >=4096 square image/render path, then rerun V3AB as V3AD with one 4096/8192 V3Z-style canvas and deterministic 4x4/8x8 seam proof. Do not create 2500 tiles before that bridge passes.",
                "",
            ]
        ),
        encoding="utf-8",
    )


def make_proof(src: Image.Image, audit: list[dict], proof_path: Path) -> None:
    sheet = Image.new("RGB", (4096, 3072), (24, 26, 24))
    draw = ImageDraw.Draw(sheet)
    label(draw, (90, 65), "V3AC source scale route search", 46)
    label(draw, (90, 135), "No more 1254 loops: local V3Z-valid native >=4096 capability not found.", 27)

    overview = src.copy()
    overview.thumbnail((1000, 1000), Image.Resampling.LANCZOS)
    sheet.paste(overview, (90, 240))
    label(draw, (90, 205), f"V3Z reference remains proof-scale: {src.width} x {src.height}", 24)

    label(draw, (1220, 240), "Scale requirement", 32)
    reqs = [
        ("current valid V3Z", "1254 x 1254", (110, 185, 210)),
        ("minimum next bridge", "4096 x 4096", (230, 205, 90)),
        ("strong bridge", "8192 x 8192", (220, 150, 80)),
        ("final source", "25600 x 25600", (235, 90, 70)),
    ]
    y = 320
    for name, res, color in reqs:
        draw.rectangle((1220, y, 2050, y + 120), outline=color, width=5)
        label(draw, (1245, y + 34), f"{name}: {res}", 25)
        y += 170

    label(draw, (90, 1340), "Local large-file audit: found files are disqualified for V3Z production route", 28)
    y = 1410
    for item in audit[:7]:
        status = item["status"]
        line = f"{item['name']} | {item.get('resolution', 'missing')} | {status}"
        label(draw, (90, y), line[:120], 20, fill=(255, 210, 145))
        y += 58

    gates = [
        "V3AC_ROUTE_FOUND=YES",
        "V3AC_LOCAL_NATIVE_GE_4096_AVAILABLE=NO",
        "V3AC_EXTERNAL_CAPABILITY_REQUIRED=YES",
        "V3AC_NEXT_ACTION_DEFINED=YES",
        "V3AC_PRODUCTION_SCALE_READY=NO",
        "V3AC_FULL_TILE_PACKAGE_CREATED=NO",
        "READY_FOR_UNITY_HANDOFF=NO",
    ]
    for i, line in enumerate(gates):
        label(draw, (2240, 340 + i * 72), line, 24, fill=(255, 220, 140))

    label(draw, (2240, 1040), "Required next supply:", 28)
    next_lines = [
        "one true native >=4096 square canvas",
        "single continuous source before cuts",
        "V3Z premium painterly style",
        "4x4/8x8 deterministic seam proof",
        "no upscale-only, no quadrant collage",
    ]
    for i, line in enumerate(next_lines):
        label(draw, (2240, 1115 + i * 64), line, 23)

    proof_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(proof_path)


def main() -> None:
    for d in (OUT, PROOF, REFS, COMM):
        d.mkdir(parents=True, exist_ok=True)

    src_ref = REFS / "v3z_visual_seam_reference.png"
    route_ref = REFS / "v3aa_route_reference.md"
    probe_ref = REFS / "v3ab_capability_probe_receipt.json"
    src_ref.write_bytes((V3Z / "source" / "v3z_2d_single_canvas_native.png").read_bytes())
    route_ref.write_bytes((V3AA / "route.md").read_bytes())
    probe_ref.write_bytes((V3AB / "V3AB_NATIVE_SCALE_CAPABILITY_PROBE_RECEIPT.json").read_bytes())

    audit = []
    for path in CANDIDATE_PATHS:
        size = image_size(path)
        audit.append(
            {
                "name": path.parent.name + "/" + path.name,
                "path": str(path),
                "resolution": f"{size[0]}x{size[1]}" if size else "missing",
                "status": "DISQUALIFIED_NOT_V3Z_VALID_PRODUCTION_SOURCE",
                "reason": "historical/rejected/soft/mechanical or not accepted as current single-canvas scalable V3Z route",
            }
        )
    audit_path = OUT / "V3AC_LOCAL_LARGE_SOURCE_AUDIT.json"
    audit_path.write_text(json.dumps(audit, indent=2), encoding="utf-8")

    route_path = OUT / "route.md"
    write_route(route_path)

    src = Image.open(src_ref).convert("RGB")
    proof_path = PROOF / "v3ac_source_scale_route_search_proof_sheet.png"
    make_proof(src, audit, proof_path)

    gates = {
        "ACTIVE_WORK_RESUMED": "YES",
        "V3AC_ROUTE_FOUND": "YES",
        "V3AC_LOCAL_NATIVE_GE_4096_AVAILABLE": "NO",
        "V3AC_EXTERNAL_CAPABILITY_REQUIRED": "YES",
        "V3AC_NEXT_ACTION_DEFINED": "YES",
        "V3AC_PRODUCTION_SCALE_READY": "NO",
        "V3AC_FULL_TILE_PACKAGE_CREATED": "NO",
        "READY_FOR_QA_BUILDERC": "NO",
        "READY_FOR_UNITY_HANDOFF": "NO",
    }
    receipt = {
        "artifact": "V3AC_SOURCE_SCALE_ROUTE_SEARCH",
        "created_at": datetime.now().isoformat(timespec="seconds"),
        "folder": str(OUT),
        "route": str(route_path),
        "proof_sheet": str(proof_path),
        "local_large_source_audit": str(audit_path),
        "references": {
            "v3z_visual_seam_reference": str(src_ref),
            "v3aa_route": str(route_ref),
            "v3ab_probe_receipt": str(probe_ref),
        },
        "finding": "Current image path outputs 1254x1254; local large historical files are not valid V3Z-style native scalable sources.",
        "missing_capability": "True native single-canvas generation/rendering at >=4096 square, preferably 8192, with V3Z premium style and deterministic grid seam validation.",
        "next_action": "Supply/connect native >=4096 square generator or renderer, then run one V3Z-style canvas with 4x4 deterministic cut and 512/1024 seam/detail proof before any full tile package.",
        "hashes": {
            "route_sha256": sha256(route_path),
            "proof_sha256": sha256(proof_path),
            "audit_sha256": sha256(audit_path),
        },
        "gates": gates,
    }
    receipt_path = OUT / "V3AC_SOURCE_SCALE_ROUTE_SEARCH_RECEIPT.json"
    receipt_path.write_text(json.dumps(receipt, indent=2), encoding="utf-8")

    checkpoint_path = OUT / "V3AC_SOURCE_SCALE_ROUTE_SEARCH_CHECKPOINT.md"
    checkpoint_path.write_text(
        "\n".join(
            [
                "# V3AC Source Scale Route Search",
                "",
                "V3AC stops the 1254-only loop and defines the required source-scale capability.",
                "No local V3Z-valid native >=4096 continuous source/toolchain was found.",
                "",
                f"- Route: `{route_path}`",
                f"- Proof: `{proof_path}`",
                f"- Audit: `{audit_path}`",
                f"- Receipt: `{receipt_path}`",
                "",
                "## Gates",
                *[f"- {k}={v}" for k, v in gates.items()],
                "",
            ]
        ),
        encoding="utf-8",
    )

    comm_path = COMM / "WorldMapCommunication_BeeKingdomWave6_V3ACSourceScaleRouteSearch_2026-07-16.md"
    comm_path.write_text(
        "\n".join(
            [
                "# Bee Kingdom Wave6 50x50 - V3AC Source Scale Route Search",
                "",
                "V3AC confirms the next bottleneck is source-scale capability, not seams. V3Z remains the visual/seam reference, but local current imagegen is capped at proof scale and historical large files are not acceptable as a V3Z production source.",
                "",
                f"- Folder: `{OUT}`",
                f"- Route: `{route_path}`",
                f"- Proof: `{proof_path}`",
                f"- Receipt: `{receipt_path}`",
                "",
                "Next action: provide/connect a true native >=4096 square continuous generator/renderer, then run deterministic 4x4 seam proof.",
                "",
                *[f"- {k}={v}" for k, v in gates.items()],
                "",
            ]
        ),
        encoding="utf-8",
    )

    print(route_path)
    print(proof_path)
    print(audit_path)
    print(checkpoint_path)
    print(receipt_path)
    print(comm_path)


if __name__ == "__main__":
    main()
