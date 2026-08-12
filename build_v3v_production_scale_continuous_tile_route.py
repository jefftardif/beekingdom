from __future__ import annotations

import hashlib
import json
from datetime import datetime
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(r"C:\projets\beekingdomgame-master")
STAGING = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging"
OUT = STAGING / "production_v3v_production_scale_continuous_tile_route"
PROOF = OUT / "proof"
CROPS512 = OUT / "representative_crops_512"
CROPS1024 = OUT / "representative_crops_1024"
REFS = OUT / "references"
COMM = ROOT / "Docs" / "WorldMapCommunication"

V3U = STAGING / "production_v3u_continuous_hd_source_attempt"
V3T = STAGING / "production_v3t_actual_hd_panel_proof"
V3R = STAGING / "production_v3r_true_continuous_source_proof"

V3U_SOURCE = V3U / "v3u_continuous_hd_source_attempt_native.png"
V3U_PROOF = V3U / "proof" / "v3u_continuous_hd_source_attempt_proof_sheet.png"
V3T_PROOF = V3T / "proof" / "v3t_actual_hd_panel_proof_sheet.png"
V3R_SOURCE = V3R / "v3r_true_continuous_source_native.png"


def font(size: int) -> ImageFont.ImageFont:
    for name in ("arial.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            pass
    return ImageFont.load_default()


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def crop_box(w: int, h: int, size: int, fx: float, fy: float) -> tuple[int, int, int, int]:
    x = round(fx * (w - size))
    y = round(fy * (h - size))
    return max(0, min(w - size, x)), max(0, min(h - size, y)), max(size, min(w, x + size)), max(size, min(h, y + size))


def label(draw: ImageDraw.ImageDraw, xy: tuple[int, int], text: str, size: int, fill=(242, 244, 230)) -> None:
    f = font(size)
    x, y = xy
    bbox = draw.textbbox((x, y), text, font=f)
    draw.rectangle((bbox[0] - 8, bbox[1] - 5, bbox[2] + 8, bbox[3] + 5), fill=(18, 22, 22))
    draw.text((x, y), text, font=f, fill=fill)


def write_route(route_path: Path) -> None:
    route_path.write_text(
        "\n".join(
            [
                "# V3V Production-Scale Continuous Tile Route",
                "",
                "## Verdict",
                "",
                "V3U is accepted as the current visual and continuity direction, but V3V is blocked for production-scale 50x50 output.",
                "The current local references demonstrate a strong 1254x1254 continuous proof and sharp independent panels, not an honest 25600x25600 continuous source or a verified seamless regional production route.",
                "",
                "## Production Requirement",
                "",
                "- Target package: 50 x 50 tiles.",
                "- Tile size: 512 x 512.",
                "- Implied full continuous pixel field: 25600 x 25600.",
                "- Current best continuous source: V3U at 1254 x 1254.",
                "- Current best detailed panels: V3T, but independently generated and not continuity-feasible.",
                "",
                "## Honest Route",
                "",
                "1. Lock V3U as composition/style direction only, not as source pixels.",
                "2. Obtain a native large-source production method with one of these verifiable capabilities:",
                "   - direct continuous generation/editing at a multi-region scale with preserved global hydrology, or",
                "   - deterministic overlapping regional generation with edge-locked conditioning and repeatable seam repair.",
                "3. Before any 2500-tile package, produce a small production-scale proof: at least a 3x3 or 5x5 native regional set with 15-20 percent overlaps, shared river/shore/mountain features crossing boundaries, and 512/1024 seam stress crops.",
                "4. Only if the overlap proof passes visual and mechanical seam checks, expand to the full 50x50 candidate package.",
                "",
                "## Smallest Viable Next Action",
                "",
                "Run one controlled overlap experiment from V3U: generate two adjacent native panels with an explicit shared boundary strip and verify whether river, forest, and mountain features continue across the seam without manual collage.",
                "",
                "## Closed Gates",
                "",
                "No tile package, no Unity handoff, no canonical swap. V3V remains blocked until native production-scale continuity is demonstrated.",
                "",
            ]
        ),
        encoding="utf-8",
    )


def make_proof_sheet(source: Image.Image, crop_paths: list[Path], out_path: Path) -> None:
    sheet = Image.new("RGB", (4096, 3072), (24, 26, 24))
    draw = ImageDraw.Draw(sheet)

    label(draw, (96, 70), "V3V production-scale continuous tile route", 46)
    label(draw, (96, 140), "Accepted direction: V3U. Production scale: BLOCKED until native continuity is proven.", 28)

    overview = source.copy()
    overview.thumbnail((1150, 1150), Image.Resampling.LANCZOS)
    sheet.paste(overview, (110, 250))
    label(draw, (110, 210), "V3U continuous proof, native 1254 x 1254", 26)

    req_x, req_y = 1450, 250
    draw.rectangle((req_x, req_y, req_x + 1150, req_y + 1150), outline=(230, 210, 90), width=6)
    for i in range(1, 10):
        x = req_x + i * 115
        y = req_y + i * 115
        draw.line((x, req_y, x, req_y + 1150), fill=(78, 82, 70), width=2)
        draw.line((req_x, y, req_x + 1150, y), fill=(78, 82, 70), width=2)
    label(draw, (req_x + 24, req_y + 24), "Required field: 50 x 50 tiles", 30)
    label(draw, (req_x + 24, req_y + 80), "25600 x 25600 continuous pixels", 26)
    label(draw, (req_x + 24, req_y + 136), "Current native source is not enough", 26, fill=(255, 196, 120))

    route_x, route_y = 2760, 250
    draw.rectangle((route_x, route_y, route_x + 1120, route_y + 1150), outline=(110, 190, 210), width=5)
    label(draw, (route_x + 24, route_y + 24), "Required next proof", 30)
    label(draw, (route_x + 24, route_y + 82), "2+ adjacent native panels", 25)
    label(draw, (route_x + 24, route_y + 132), "locked overlap strip", 25)
    label(draw, (route_x + 24, route_y + 182), "seam stress crops pass", 25)
    label(draw, (route_x + 24, route_y + 232), "then 3x3 / 5x5 route", 25)
    for x in (route_x + 120, route_x + 560, route_x + 1000):
        draw.line((x, route_y + 360, x, route_y + 1020), fill=(80, 120, 130), width=4)
    for y in (route_y + 360, route_y + 690, route_y + 1020):
        draw.line((route_x + 120, y, route_x + 1000, y), fill=(80, 120, 130), width=4)
    draw.rectangle((route_x + 505, route_y + 360, route_x + 615, route_y + 1020), fill=(130, 75, 65))
    label(draw, (route_x + 450, route_y + 1040), "stress seam", 24, fill=(255, 190, 170))

    label(draw, (110, 1530), "Representative native crops from V3U direction proof", 30)
    for idx, path in enumerate(crop_paths):
        img = Image.open(path).convert("RGB")
        img.thumbnail((390, 390), Image.Resampling.LANCZOS)
        col = idx % 4
        row = idx // 4
        x = 110 + col * 480
        y = 1600 + row * 520
        sheet.paste(img, (x, y))
        label(draw, (x, y - 34), path.stem.replace("v3v_", ""), 18)

    label(draw, (2240, 1580), "Gate verdict", 34)
    verdict_lines = [
        "V3V_ROUTE_CREATED=YES",
        "V3V_PROOF_CREATED=YES",
        "V3V_DETAIL_PASS=YES at proof scale",
        "V3V_CONTINUITY_PASS=YES at proof scale",
        "V3V_FULL_PRODUCTION_SOURCE_READY=NO",
        "V3V_FULL_TILE_PACKAGE_CREATED=NO",
        "BLOCKED_PRODUCTION_SCALE=YES",
    ]
    for i, line in enumerate(verdict_lines):
        label(draw, (2240, 1660 + i * 70), line, 25, fill=(255, 226, 150) if line.endswith("NO") or line.endswith("YES") else (242, 244, 230))

    out_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(out_path)


def main() -> None:
    for d in (OUT, PROOF, CROPS512, CROPS1024, REFS, COMM):
        d.mkdir(parents=True, exist_ok=True)

    source_copy = REFS / "v3u_direction_reference_native_1254.png"
    proof_copy = REFS / "v3u_direction_reference_proof_sheet.png"
    detail_copy = REFS / "v3t_detail_reference_proof_sheet.png"
    continuity_copy = REFS / "v3r_continuity_reference_native_1254.png"
    source_copy.write_bytes(V3U_SOURCE.read_bytes())
    proof_copy.write_bytes(V3U_PROOF.read_bytes())
    detail_copy.write_bytes(V3T_PROOF.read_bytes())
    continuity_copy.write_bytes(V3R_SOURCE.read_bytes())

    img = Image.open(source_copy).convert("RGB")
    anchors = [
        ("NW_coast", 0.02, 0.02),
        ("N_mountain", 0.50, 0.02),
        ("NE_transition", 0.98, 0.02),
        ("W_forest_water", 0.02, 0.50),
        ("CENTER_hydrology", 0.50, 0.50),
        ("E_green_desert", 0.98, 0.50),
        ("SW_islands", 0.02, 0.98),
        ("SE_desert_water", 0.98, 0.98),
    ]
    crop512_paths: list[Path] = []
    crop1024_paths: list[Path] = []
    for idx, (name, fx, fy) in enumerate(anchors, start=1):
        p512 = CROPS512 / f"v3v_crop512_{idx:02d}_{name}.png"
        img.crop(crop_box(img.width, img.height, 512, fx, fy)).save(p512)
        crop512_paths.append(p512)
        p1024 = CROPS1024 / f"v3v_crop1024_{idx:02d}_{name}.png"
        img.crop(crop_box(img.width, img.height, min(1024, img.width, img.height), fx, fy)).save(p1024)
        crop1024_paths.append(p1024)

    route_path = OUT / "route.md"
    write_route(route_path)
    proof_path = PROOF / "v3v_production_scale_route_proof_sheet.png"
    make_proof_sheet(img, crop512_paths, proof_path)

    gates = {
        "ACTIVE_WORK_RESUMED": "YES",
        "V3V_ROUTE_CREATED": "YES",
        "V3V_PROOF_CREATED": "YES",
        "V3V_DETAIL_PASS": "YES",
        "V3V_CONTINUITY_PASS": "YES",
        "V3V_FULL_PRODUCTION_SOURCE_READY": "NO",
        "V3V_FULL_TILE_PACKAGE_CREATED": "NO",
        "READY_FOR_QA_BUILDERC": "NO",
        "READY_FOR_UNITY_HANDOFF": "NO",
        "BLOCKED_PRODUCTION_SCALE": "YES",
    }
    receipt = {
        "artifact": "V3V_PRODUCTION_SCALE_CONTINUOUS_TILE_ROUTE",
        "created_at": datetime.now().isoformat(timespec="seconds"),
        "folder": str(OUT),
        "route": str(route_path),
        "proof_sheet": str(proof_path),
        "references": {
            "v3u_direction": str(source_copy),
            "v3u_proof": str(proof_copy),
            "v3t_detail": str(detail_copy),
            "v3r_continuity": str(continuity_copy),
        },
        "representative_crops_512": [str(p) for p in crop512_paths],
        "representative_crops_1024": [str(p) for p in crop1024_paths],
        "production_requirement": {
            "tiles": [50, 50],
            "tile_size": [512, 512],
            "required_continuous_source": [25600, 25600],
            "current_best_continuous_source": [img.width, img.height],
        },
        "verdict": "BLOCKED at production scale: V3U direction passes proof-scale detail/continuity, but no native production-scale continuous source or seam-verified regional route exists yet.",
        "smallest_viable_next_action": "Generate two adjacent native panels with a shared locked overlap strip from V3U and verify seam stress crops before any 50x50 package.",
        "hashes": {
            "v3u_source_sha256": sha256(source_copy),
            "proof_sha256": sha256(proof_path),
            "route_sha256": sha256(route_path),
        },
        "gates": gates,
    }
    receipt_path = OUT / "V3V_PRODUCTION_SCALE_CONTINUOUS_TILE_ROUTE_RECEIPT.json"
    receipt_path.write_text(json.dumps(receipt, indent=2), encoding="utf-8")

    checkpoint_path = OUT / "V3V_PRODUCTION_SCALE_CONTINUOUS_TILE_ROUTE_CHECKPOINT.md"
    checkpoint_path.write_text(
        "\n".join(
            [
                "# V3V Production-Scale Continuous Tile Route Checkpoint",
                "",
                f"- Route: `{route_path}`",
                f"- Proof: `{proof_path}`",
                f"- Receipt: `{receipt_path}`",
                "",
                "Verdict: blocked at production scale. No 2500 tiles were created.",
                "",
                "## Gates",
                *[f"- {k}={v}" for k, v in gates.items()],
                "",
            ]
        ),
        encoding="utf-8",
    )

    comm_path = COMM / "WorldMapCommunication_BeeKingdomWave6_V3VProductionScaleContinuousTileRoute_2026-07-16.md"
    comm_path.write_text(
        "\n".join(
            [
                "# Bee Kingdom Wave6 50x50 - V3V Production-Scale Continuous Tile Route",
                "",
                "V3U is validated as the visual and continuity direction, but V3V remains blocked for production-scale output.",
                "The current source is 1254x1254 and cannot honestly produce 2500 native 512px tiles without fake upscale.",
                "",
                f"- Folder: `{OUT}`",
                f"- Route: `{route_path}`",
                f"- Proof: `{proof_path}`",
                f"- Receipt: `{receipt_path}`",
                "",
                "Next action: two-panel locked-overlap native proof, then 3x3/5x5 seam validation before any full package.",
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
