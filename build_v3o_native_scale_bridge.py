from __future__ import annotations

import hashlib
import json
from datetime import datetime
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(r"C:\projets\beekingdomgame-master")
STAGING = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging"
OUT = STAGING / "production_v3o_native_scale_bridge"
PROOF = OUT / "proof"
REFS = OUT / "references"
COMM = ROOT / "Docs" / "WorldMapCommunication"

DECISION = ROOT / "Docs" / "BuilderA" / "WorldMapWave6_50x50_CandidateGateDecision" / "WorldMapWave6_50x50_CandidateGateDecision_20260717.md"
V2I_FAIL = ROOT / "Docs" / "BuilderA" / "WorldMapWave6_50x50_V2IRepairAuditPreview" / "WorldMapWave6_V2IRepairAuditPreview_PerceptualFail_GlobalPatchwork_20260716.md"

V3O = STAGING / "production_v3o_pictorial_source_proof"
V3Z = STAGING / "production_v3z_2d_single_canvas_grid_scale_bridge"
V3AA = STAGING / "production_v3aa_scale_route_from_v3z"
V3Y = STAGING / "production_v3y_single_canvas_superpanel_cut_test"


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


def maybe_image(path: Path) -> dict:
    item = {"path": str(path), "exists": path.exists()}
    if path.exists():
        with Image.open(path) as img:
            item["resolution"] = [img.width, img.height]
        item["sha256"] = sha256(path)
    return item


def copy(src: Path, dst: Path) -> None:
    if src.exists():
        dst.write_bytes(src.read_bytes())


def write_route(path: Path) -> None:
    path.write_text(
        "\n".join(
            [
                "# V3O Native Scale Bridge",
                "",
                "## Fresh Decisions Applied",
                "",
                "- V3E/V3D/V3H/V3M reduced or upscale routes are revoked as final premium candidates.",
                "- V2I/V2R repair route is rejected because Unity inspection shows global rectangular patchwork.",
                "- V3O is the current best style and continuity reference only; it is not a final 50x50 source.",
                "",
                "## Target",
                "",
                "Create a route toward a true premium 50x50 map that preserves 512 real source pixels per final tile.",
                "",
                "Required final source field:",
                "",
                "- 50 x 50 tiles",
                "- 512 x 512 pixels per tile",
                "- 25600 x 25600 native coherent source, or an equivalent validated native multi-panel workflow",
                "",
                "## Why V3O Is Not Directly Available As Native Source",
                "",
                "The local V3O proof is 4096x4096. It is valuable as a reduced visual/continuity reference, but it only provides about 82 source pixels per final 512-pixel tile if stretched to 25600. That fails the 512-real-pixels-per-tile requirement.",
                "",
                "## Executable Bridge Route",
                "",
                "1. Use V3O as the style and continuity target.",
                "2. Use V3Z/V3Y only for the successful single-canvas deterministic-cut principle.",
                "3. Obtain or generate a native coherent source window at real production density:",
                "   - minimum bridge: one 4096x4096 native window covering exactly 8x8 final tiles at 512 px/tile, or",
                "   - stronger bridge: 8192x8192 native window covering 16x16 final tiles.",
                "4. The bridge source must be one coherent native canvas before cuts, not independent quadrant collage.",
                "5. Deterministically cut the bridge into 512 tiles only inside that small window.",
                "6. Produce vertical/horizontal seam stress crops at 512/1024 and 100% detail crops.",
                "7. Only after the native window passes human review should the same workflow scale to broader regions.",
                "",
                "## Current Block",
                "",
                "No current local source is both V3O-style and native-scale for final 512 px/tile. Current imagegen path previously proved capped at 1254 for new single-canvas images; existing 4096 V3O is a reduced proof, not a native final-density production source.",
                "",
                "## Forbidden",
                "",
                "- no V2I/V2R seam repair",
                "- no V3E/V3H upscale/sharpen route",
                "- no 2500 tiles",
                "- no 25600 master authorization",
                "- no Unity handoff",
                "",
            ]
        ),
        encoding="utf-8",
    )


def make_proof(inventory: dict, proof_path: Path) -> None:
    sheet = Image.new("RGB", (4096, 3072), (24, 26, 24))
    draw = ImageDraw.Draw(sheet)
    label(draw, (90, 65), "V3O native scale bridge", 46)
    label(draw, (90, 135), "V3O style target; native final-density source is not yet available.", 28)

    v3o_path = Path(inventory["references"]["V3O_4096_proof"]["path"])
    if v3o_path.exists():
        img = Image.open(v3o_path).convert("RGB")
        img.thumbnail((1120, 1120), Image.Resampling.LANCZOS)
        sheet.paste(img, (90, 240))
    label(draw, (90, 205), "V3O reduced pictorial proof: reference only", 24)

    label(draw, (1400, 240), "Resolution requirement", 32)
    rows = [
        ("V3O reduced proof", "4096 x 4096", "reference only", (110, 185, 210)),
        ("native bridge window", "4096 x 4096", "8x8 final tiles at 512 px", (230, 205, 90)),
        ("strong bridge", "8192 x 8192", "16x16 final tiles at 512 px", (220, 150, 80)),
        ("full target", "25600 x 25600", "50x50 final tiles", (235, 90, 70)),
    ]
    y = 325
    for name, res, note, color in rows:
        draw.rectangle((1400, y, 2780, y + 122), outline=color, width=5)
        label(draw, (1425, y + 20), f"{name}: {res}", 25)
        label(draw, (1425, y + 67), note, 20)
        y += 165

    gates = [
        "ACTIVE_WORK_RESUMED=YES",
        "V3O_NATIVE_SOURCE_AVAILABLE=NO",
        "READY_FOR_FULL_50X50_TILE_BUILD=NO",
        "READY_FOR_QA_BUILDERC=NO",
        "READY_FOR_UNITY_HANDOFF=NO",
        "MASTER_25600_AUTHORIZED=NO",
    ]
    for i, line in enumerate(gates):
        label(draw, (2920, 330 + i * 72), line, 24, fill=(255, 220, 140))

    label(draw, (90, 1430), "Reference inventory", 30)
    lines = [
        "V3O: style/continuity target, 4096 reduced proof only",
        "V3Z: 2D single-canvas seam principle, 1254 proof-scale",
        "V3AA: scale route requirement, no production source",
        "V3Y: single-canvas cut principle, wide proof-scale",
        "V2I/V2R: rejected, do not repair",
        "V3E/V3H: revoked as final premium route",
    ]
    for i, line in enumerate(lines):
        label(draw, (90, 1505 + i * 64), line, 24)

    label(draw, (90, 2040), "Next valid test", 32)
    next_lines = [
        "Produce one coherent V3O-style native 4096 window representing an 8x8 final-tile area.",
        "Cut only that window into 512 tiles for seam/detail proof.",
        "Reject any upscale-only or independent quadrant source.",
        "Promote only after human review confirms no patchwork and true 512 px/tile detail.",
    ]
    for i, line in enumerate(next_lines):
        label(draw, (90, 2120 + i * 70), line, 24, fill=(255, 230, 180))

    proof_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(proof_path)


def main() -> None:
    for d in (OUT, PROOF, REFS, COMM):
        d.mkdir(parents=True, exist_ok=True)

    refs = {
        "V3O_4096_proof": maybe_image(V3O / "v3o_pictorial_source_proof_4096.png"),
        "V3O_reference": maybe_image(V3O / "v3o_generated_pictorial_reference.png"),
        "V3O_proof_sheet": maybe_image(V3O / "proof" / "v3o_pictorial_source_proof_sheet.png"),
        "V3Z_single_canvas": maybe_image(V3Z / "source" / "v3z_2d_single_canvas_native.png"),
        "V3Z_proof_sheet": maybe_image(V3Z / "proof" / "v3z_2d_single_canvas_grid_scale_bridge_proof_sheet.png"),
        "V3Y_single_canvas": maybe_image(V3Y / "source" / "v3y_single_canvas_superpanel_native.png"),
        "V3Y_proof_sheet": maybe_image(V3Y / "proof" / "v3y_single_canvas_superpanel_cut_test_proof_sheet.png"),
        "V3AA_receipt": {"path": str(V3AA / "V3AA_SCALE_ROUTE_FROM_V3Z_RECEIPT.json"), "exists": (V3AA / "V3AA_SCALE_ROUTE_FROM_V3Z_RECEIPT.json").exists()},
    }

    copy(DECISION, REFS / "WorldMapWave6_50x50_CandidateGateDecision_20260717.md")
    copy(V2I_FAIL, REFS / "WorldMapWave6_V2IRepairAuditPreview_PerceptualFail_GlobalPatchwork_20260716.md")
    copy(V3O / "V3O_PICTORIAL_SOURCE_PROOF_RECEIPT.json", REFS / "V3O_PICTORIAL_SOURCE_PROOF_RECEIPT.json")
    copy(V3Z / "V3Z_2D_SINGLE_CANVAS_GRID_SCALE_BRIDGE_RECEIPT.json", REFS / "V3Z_2D_SINGLE_CANVAS_GRID_SCALE_BRIDGE_RECEIPT.json")
    copy(V3AA / "V3AA_SCALE_ROUTE_FROM_V3Z_RECEIPT.json", REFS / "V3AA_SCALE_ROUTE_FROM_V3Z_RECEIPT.json")
    copy(V3Y / "V3Y_SINGLE_CANVAS_SUPERPANEL_CUT_TEST_RECEIPT.json", REFS / "V3Y_SINGLE_CANVAS_SUPERPANEL_CUT_TEST_RECEIPT.json")

    route_path = OUT / "route.md"
    write_route(route_path)

    inventory = {
        "artifact": "V3O_NATIVE_SCALE_BRIDGE_INVENTORY",
        "created_at": datetime.now().isoformat(timespec="seconds"),
        "references": refs,
        "decision": {
            "V3E_route_revoked": True,
            "V2I_V2R_rejected_patchwork": True,
            "V3O_style_reference_only": True,
            "V3O_NATIVE_SOURCE_AVAILABLE": "NO",
        },
    }
    inventory_path = OUT / "V3O_NATIVE_SCALE_BRIDGE_SOURCE_INVENTORY.json"
    inventory_path.write_text(json.dumps(inventory, indent=2), encoding="utf-8")

    proof_path = PROOF / "v3o_native_scale_bridge_proof_sheet.png"
    make_proof(inventory, proof_path)

    gates = {
        "ACTIVE_WORK_RESUMED": "YES",
        "V3O_NATIVE_SOURCE_AVAILABLE": "NO",
        "READY_FOR_FULL_50X50_TILE_BUILD": "NO",
        "READY_FOR_QA_BUILDERC": "NO",
        "READY_FOR_UNITY_HANDOFF": "NO",
        "MASTER_25600_AUTHORIZED": "NO",
    }
    receipt = {
        "artifact": "V3O_NATIVE_SCALE_BRIDGE",
        "created_at": datetime.now().isoformat(timespec="seconds"),
        "folder": str(OUT),
        "route": str(route_path),
        "inventory": str(inventory_path),
        "proof_sheet": str(proof_path),
        "source_decision": "V3O is style/continuity reference only; no native final-density V3O source is available locally.",
        "required_next_test": "One coherent V3O-style native 4096x4096 window that represents exactly 8x8 final 512px tiles, deterministic 512 cuts, seam stress, and 100% detail proof.",
        "blocked_reason": "Existing V3O 4096 is reduced proof/reference, not a 25600-scale or 512-real-pixels-per-final-tile production source; current image path has not proven native scale.",
        "forbidden_routes": [
            "V2I/V2R local seam repair",
            "V3E/V3H upscale or sharpening",
            "independent quadrant collage",
            "2500 tiles before native bridge proof",
            "master 25600 without authorization",
            "Unity handoff",
        ],
        "hashes": {
            "route_sha256": sha256(route_path),
            "inventory_sha256": sha256(inventory_path),
            "proof_sha256": sha256(proof_path),
        },
        "gates": gates,
    }
    receipt_path = OUT / "V3O_NATIVE_SCALE_BRIDGE_RECEIPT.json"
    receipt_path.write_text(json.dumps(receipt, indent=2), encoding="utf-8")

    checkpoint_path = OUT / "V3O_NATIVE_SCALE_BRIDGE_CHECKPOINT.md"
    checkpoint_path.write_text(
        "\n".join(
            [
                "# V3O Native Scale Bridge Checkpoint",
                "",
                "ACTIVE_WORK_RESUMED=YES",
                "",
                "V3E/V3H upscale routes are revoked as final premium candidates. V2I/V2R repair route is rejected for global patchwork.",
                "V3O is retained as style and continuity target only.",
                "",
                f"- Route: `{route_path}`",
                f"- Inventory: `{inventory_path}`",
                f"- Proof sheet: `{proof_path}`",
                f"- Receipt: `{receipt_path}`",
                "",
                "## Gates",
                *[f"- {k}={v}" for k, v in gates.items()],
                "",
            ]
        ),
        encoding="utf-8",
    )

    comm_path = COMM / "WorldMapCommunication_BeeKingdomWave6_V3ONativeScaleBridge_2026-07-17.md"
    comm_path.write_text(
        "\n".join(
            [
                "# Bee Kingdom Wave6 50x50 - V3O Native Scale Bridge",
                "",
                "V3O is now the style/continuity target, not a final source. V3E/V3H upscale and V2I/V2R repair routes remain closed.",
                "No full package, no 25600 master, no Unity handoff.",
                "",
                f"- Folder: `{OUT}`",
                f"- Route: `{route_path}`",
                f"- Inventory: `{inventory_path}`",
                f"- Proof: `{proof_path}`",
                f"- Receipt: `{receipt_path}`",
                "",
                "Next valid action: produce one coherent native V3O-style 4096x4096 window representing 8x8 final tiles at 512 px/tile, then cut/stress-test only that window.",
                "",
                *[f"- {k}={v}" for k, v in gates.items()],
                "",
            ]
        ),
        encoding="utf-8",
    )

    print(route_path)
    print(inventory_path)
    print(proof_path)
    print(checkpoint_path)
    print(receipt_path)
    print(comm_path)


if __name__ == "__main__":
    main()
