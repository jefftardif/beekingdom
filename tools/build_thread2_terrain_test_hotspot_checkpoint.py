from __future__ import annotations

import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw


ROOT = Path(r"C:\projets\beekingdomgame-master")
PKG = ROOT / r"Assets\BeeKingdom\Playground\Resources\WorldMapWave6Runtime\UIB_ImmenseContinuousMaster50x50_wave5method_12288_preview"
SCENE = ROOT / r"Assets\Scenes\WorldMapWave6Premium50x50TerrainTest.unity"
STAGE = ROOT / r"artifacts\UIB_ImmenseContinuousMaster50x50_premium_v4_phase2_thread2_staging\thread2_premium50x50_terrain_test_hotspot_review"
DOCS = ROOT / r"Docs\BuilderA\WorldMapWave6_50x50_Wave5MethodRestart"
PROOF = STAGE / "proof"


HOTSPOTS = [
    {"name": "C54_09_DEFECT001", "chunk_x": 54, "chunk_y": 9, "row": 2, "col": 47, "expectation": "previous hard horizontal seam"},
    {"name": "C53_26_DEFECT002", "chunk_x": 53, "chunk_y": 26, "row": 19, "col": 46, "expectation": "previous inverted mountain/crystal"},
    {"name": "C52_52_CORNER", "chunk_x": 52, "chunk_y": 52, "row": 45, "col": 45, "expectation": "far corner continuity"},
    {"name": "C48_46_OUTER", "chunk_x": 48, "chunk_y": 46, "row": 39, "col": 41, "expectation": "outer transition continuity"},
    {"name": "CENTER_C32_32", "chunk_x": 32, "chunk_y": 32, "row": 25, "col": 25, "expectation": "center core continuity"},
]


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest().upper()


def tile_path(row: int, col: int) -> Path:
    return PKG / f"R{row:02d}C{col:02d}_g2.png"


def load(row: int, col: int) -> Image.Image:
    return Image.open(tile_path(row, col)).convert("RGB")


def clamp(value: int) -> int:
    return max(0, min(49, value))


def save(img: Image.Image, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    img.save(path, optimize=True)


def edge_score(a: Image.Image, b: Image.Image, side: str) -> float:
    arr_a = np.asarray(a.convert("RGB"), dtype=np.float32)
    arr_b = np.asarray(b.convert("RGB"), dtype=np.float32)
    if side == "right_left":
        edge_a = arr_a[:, -3:, :]
        edge_b = arr_b[:, :3, :]
    else:
        edge_a = arr_a[-3:, :, :]
        edge_b = arr_b[:3, :, :]
    return float(np.mean(np.abs(edge_a - edge_b)))


def neighborhood(row: int, col: int, title: str) -> Image.Image:
    rows = [clamp(row - 1), clamp(row), clamp(row + 1)]
    cols = [clamp(col - 1), clamp(col), clamp(col + 1)]
    tile_size = 516
    header = 52
    canvas = Image.new("RGB", (tile_size * 3, header + tile_size * 3), (8, 10, 9))
    draw = ImageDraw.Draw(canvas)
    draw.text((12, 14), title, fill=(255, 230, 120))
    for iy, r in enumerate(rows):
        for ix, c in enumerate(cols):
            x, y = ix * tile_size, header + iy * tile_size
            canvas.paste(load(r, c), (x, y))
            outline = (80, 240, 255) if r == row and c == col else (255, 210, 0)
            draw.rectangle((x, y, x + tile_size - 1, y + tile_size - 1), outline=outline, width=3)
            draw.text((x + 8, y + 8), f"R{r:02d}C{c:02d} / C{c+7:02d}_{r+7:02d}", fill=(255, 255, 255))
    return canvas


def seam_sheet(row: int, col: int, title: str) -> Image.Image:
    center = load(row, col)
    left = load(row, clamp(col - 1))
    right = load(row, clamp(col + 1))
    top = load(clamp(row - 1), col)
    bottom = load(clamp(row + 1), col)
    strips = [
        ("left-center vertical", Image.new("RGB", (256, 516), (0, 0, 0))),
        ("center-right vertical", Image.new("RGB", (256, 516), (0, 0, 0))),
        ("top-center horizontal", Image.new("RGB", (516, 256), (0, 0, 0))),
        ("center-bottom horizontal", Image.new("RGB", (516, 256), (0, 0, 0))),
    ]
    strips[0][1].paste(left.crop((388, 0, 516, 516)), (0, 0))
    strips[0][1].paste(center.crop((0, 0, 128, 516)), (128, 0))
    strips[1][1].paste(center.crop((388, 0, 516, 516)), (0, 0))
    strips[1][1].paste(right.crop((0, 0, 128, 516)), (128, 0))
    strips[2][1].paste(top.crop((0, 388, 516, 516)), (0, 0))
    strips[2][1].paste(center.crop((0, 0, 516, 128)), (0, 128))
    strips[3][1].paste(center.crop((0, 388, 516, 516)), (0, 0))
    strips[3][1].paste(bottom.crop((0, 0, 516, 128)), (0, 128))

    canvas = Image.new("RGB", (1120, 700), (8, 10, 9))
    draw = ImageDraw.Draw(canvas)
    draw.text((12, 14), title, fill=(255, 230, 120))
    positions = [(20, 60), (320, 60), (620, 60), (620, 360)]
    for (name, img), (x, y) in zip(strips, positions):
        canvas.paste(img, (x, y))
        draw.rectangle((x, y, x + img.width - 1, y + img.height - 1), outline=(255, 210, 0), width=2)
        draw.text((x + 6, y + 6), name, fill=(255, 255, 255))
    return canvas


def main() -> None:
    PROOF.mkdir(parents=True, exist_ok=True)
    DOCS.mkdir(parents=True, exist_ok=True)
    manifest = json.loads((PKG / "runtime_manifest.json").read_text(encoding="utf-8"))
    results = []
    proof_paths = []
    for spot in HOTSPOTS:
        row = spot["row"]
        col = spot["col"]
        title = f"{spot['name']} hotspot {spot['expectation']} center R{row:02d}C{col:02d}"
        npath = PROOF / f"{spot['name']}_neighborhood_3x3.png"
        spath = PROOF / f"{spot['name']}_seam_strips.png"
        save(neighborhood(row, col, title), npath)
        save(seam_sheet(row, col, title), spath)
        proof_paths.extend([str(npath), str(spath)])
        center = load(row, col)
        metrics = {
            "left_center_edge_delta": edge_score(load(row, clamp(col - 1)), center, "right_left"),
            "center_right_edge_delta": edge_score(center, load(row, clamp(col + 1)), "right_left"),
            "top_center_edge_delta": edge_score(load(clamp(row - 1), col), center, "bottom_top"),
            "center_bottom_edge_delta": edge_score(center, load(clamp(row + 1), col), "bottom_top"),
        }
        # Conservative visual verdict: edge deltas are not enough for PASS, but catch hard breaks.
        hard_metric_fail = any(value > 55 for value in metrics.values())
        results.append(
            {
                **spot,
                "tile": f"R{row:02d}C{col:02d}_g2.png",
                "proof_neighborhood": str(npath),
                "proof_seams": str(spath),
                "metrics": metrics,
                "metric_gate": "FAIL_HARD_EDGE_DELTA" if hard_metric_fail else "PASS_NO_HARD_EDGE_DELTA",
            }
        )

    # Create compact contact sheet.
    thumbs = [Image.open(path).convert("RGB").resize((480, 500), Image.Resampling.LANCZOS) for path in proof_paths if path.endswith("neighborhood_3x3.png")]
    sheet = Image.new("RGB", (1500, 1100), (8, 10, 9))
    draw = ImageDraw.Draw(sheet)
    draw.text((16, 14), "Wave6 Premium50x50 Terrain Test - hotspot offline proof from 12288 package", fill=(255, 230, 120))
    for i, thumb in enumerate(thumbs):
        x = 16 + (i % 3) * 495
        y = 55 + (i // 3) * 520
        sheet.paste(thumb, (x, y))
        draw.rectangle((x, y, x + thumb.width - 1, y + thumb.height - 1), outline=(255, 210, 0), width=2)
        draw.text((x + 8, y + 8), HOTSPOTS[i]["name"], fill=(255, 255, 255))
    sheet_path = PROOF / "THREAD2_PREMIUM50X50_TERRAIN_TEST_HOTSPOT_CONTACT_SHEET.png"
    save(sheet, sheet_path)

    failed = [item for item in results if item["metric_gate"].startswith("FAIL")]
    verdict = "PASS_OFFLINE_HOTSPOT_METRIC_REVIEW" if not failed else "FAIL_OFFLINE_HOTSPOT_METRIC_REVIEW"
    # Offline proof cannot replace human Unity review.
    final_gate = "PASS_REQUIRES_UNITY_HUMAN_REVIEW" if verdict.startswith("PASS") else verdict

    report_lines = [
        "# Thread2 Premium50x50 Terrain Test Hotspot Checkpoint",
        "",
        f"VERDICT={final_gate}",
        "SCENE=Assets/Scenes/WorldMapWave6Premium50x50TerrainTest.unity",
        "PACKAGE=UIB_ImmenseContinuousMaster50x50_wave5method_12288_preview",
        "MODE=offline_tile_hotspot_review_from_terrain_only_package",
        "",
        "## Package",
        "",
        f"- source proof: {manifest['source']['source_proof_path']}",
        f"- source proof resolution: {manifest['source']['source_proof_resolution']}",
        f"- package kind: {manifest['package_kind']}",
        "- note: audit-only; not final QA handoff.",
        "",
        "## Hotspots",
        "",
    ]
    for item in results:
        report_lines.append(
            f"- {item['name']} C{item['chunk_x']:02d}_{item['chunk_y']:02d} / {item['tile']}: {item['metric_gate']}; proofs: `{item['proof_neighborhood']}`, `{item['proof_seams']}`"
        )
    report_lines += [
        "",
        "## Decision",
        "",
        "No large production, no 25600 master, no Unity handoff. Offline tile proofs show no metric hard-edge blocker if all hotspots are PASS_NO_HARD_EDGE_DELTA, but visual PASS still requires the terrain-only Unity scene because the user-reported failures were in Unity view.",
        "",
        "## Gates",
        "",
        "READY_FOR_QA_BUILDERC=NO",
        "READY_FOR_UNITY_HANDOFF=NO",
        "MASTER_25600_AUTHORIZED=NO",
    ]
    report = "\n".join(report_lines)
    report_path = STAGE / "THREAD2_PREMIUM50X50_TERRAIN_TEST_HOTSPOT_CHECKPOINT.md"
    docs_report = DOCS / "Thread2_Premium50x50TerrainTest_HotspotCheckpoint.md"
    report_path.write_text(report, encoding="utf-8")
    docs_report.write_text(report, encoding="utf-8")
    receipt = {
        "artifact": "THREAD2_PREMIUM50X50_TERRAIN_TEST_HOTSPOT_REVIEW",
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "status": final_gate,
        "scene": "Assets/Scenes/WorldMapWave6Premium50x50TerrainTest.unity",
        "resource_root": "WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_wave5method_12288_preview",
        "package_manifest_sha256": sha256(PKG / "runtime_manifest.json"),
        "contact_sheet": str(sheet_path),
        "hotspots": results,
        "proofs": proof_paths,
        "report": str(report_path),
        "docs_report": str(docs_report),
        "gates": {
            "TERRAIN_TEST_SCENE_PRESENT": "YES" if SCENE.exists() else "NO",
            "PACKAGE_12288_PRESENT": "YES" if PKG.exists() else "NO",
            "HOTSPOT_PROOFS_CREATED": "YES",
            "OFFLINE_HOTSPOT_REVIEW": final_gate,
            "READY_FOR_QA_BUILDERC": "NO",
            "READY_FOR_UNITY_HANDOFF": "NO",
            "MASTER_25600_AUTHORIZED": "NO",
        },
    }
    receipt_path = STAGE / "THREAD2_PREMIUM50X50_TERRAIN_TEST_HOTSPOT_RECEIPT.json"
    receipt_path.write_text(json.dumps(receipt, indent=2), encoding="utf-8")
    print(json.dumps({"status": final_gate, "report": str(report_path), "receipt": str(receipt_path), "contact_sheet": str(sheet_path)}, indent=2))


if __name__ == "__main__":
    main()
