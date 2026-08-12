from __future__ import annotations

import hashlib
import json
import shutil
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image


ROOT = Path(r"C:\projets\beekingdomgame-master")
BASELINE = ROOT / "Assets" / "BeeKingdom" / "Playground" / "Resources" / "WorldMapWave6Runtime" / "UIB_ImmenseContinuousMaster50x50_v2i_repair_audit_preview"
TARGET = ROOT / "Assets" / "BeeKingdom" / "Playground" / "Resources" / "WorldMapWave6Runtime" / "UIB_ImmenseContinuousMaster50x50_v2i_selected_hd_local_repair_review"
PATCH = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging" / "local_repair_selected_hd_candidate_20260717" / "patched_tiles"
DOCS = ROOT / "Docs" / "BuilderA" / "WorldMapWave6_50x50_SelectedHdLocalRepairReview"
SCENE_BASE = ROOT / "Assets" / "Scenes" / "WorldMapWave6V2IRepairAuditPreview.unity"
SCENE_TARGET = ROOT / "Assets" / "Scenes" / "WorldMapWave6SelectedHdLocalRepairReview.unity"

EXPECTED_PATCHED = [
    "R02C46_g2.png",
    "R02C47_g2.png",
    "R02C48_g2.png",
    "R03C46_g2.png",
    "R03C47_g2.png",
    "R03C48_g2.png",
    "R19C45_g2.png",
    "R19C46_g2.png",
    "R19C47_g2.png",
]


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest().upper()


def tile_name(row: int, col: int) -> str:
    return f"R{row:02d}C{col:02d}_g2.png"


def package_signature(tile_records: list[dict[str, object]]) -> str:
    h = hashlib.sha256()
    for record in tile_records:
        h.update(str(record["resource_name"]).encode("ascii"))
        h.update(b":")
        h.update(str(record["runtime_sha256"]).encode("ascii"))
        h.update(b"\n")
    return h.hexdigest().upper()


def copy_baseline_pngs() -> None:
    TARGET.mkdir(parents=True, exist_ok=True)
    src_files = sorted(BASELINE.glob("R??C??_g2.png"))
    if len(src_files) != 2500:
        raise RuntimeError(f"Baseline tile count is {len(src_files)}, expected 2500")
    for src in src_files:
        shutil.copy2(src, TARGET / src.name)


def apply_patches() -> list[str]:
    missing = [name for name in EXPECTED_PATCHED if not (PATCH / name).exists()]
    if missing:
        raise FileNotFoundError("Missing patch tiles: " + ", ".join(missing))
    for name in EXPECTED_PATCHED:
        shutil.copy2(PATCH / name, TARGET / name)
    return EXPECTED_PATCHED[:]


def build_manifest(changed: list[str]) -> tuple[dict[str, object], str]:
    tiles = []
    for row in range(50):
        for col in range(50):
            file_name = tile_name(row, col)
            path = TARGET / file_name
            if not path.exists():
                raise FileNotFoundError(path)
            with Image.open(path) as im:
                if im.size != (516, 516):
                    raise RuntimeError(f"{file_name} has size {im.size}, expected 516x516")
            tiles.append(
                {
                    "id": f"R{row:02d}C{col:02d}",
                    "row": row,
                    "column": col,
                    "chunk_x": 7 + col,
                    "chunk_y": 7 + row,
                    "resource_name": f"R{row:02d}C{col:02d}_g2",
                    "file": file_name,
                    "width": 516,
                    "height": 516,
                    "gutter": 2,
                    "runtime_sha256": sha256(path),
                    "source_role": "local_patch" if file_name in changed else "v2i_repair_baseline",
                }
            )
    signature = package_signature(tiles)
    manifest = {
        "schema": "bee-kingdom.world-map.wave6-unity-runtime-bundle.v1",
        "source": {
            "master_sha256": signature,
            "source": str(PATCH),
            "source_role": "Selected HD local repair review sibling; baseline copied from V2I repair audit preview, with local patches only for DEFECT-001 and DEFECT-002.",
            "baseline_resource_root": str(BASELINE),
            "monolithic_master_imported": False,
        },
        "grid": {
            "rows": 50,
            "columns": 50,
            "tile_size": 512,
            "runtime_tile_size": 516,
            "gutter": 2,
            "origin_chunk_x": 7,
            "origin_chunk_y": 7,
            "world_width": 25600,
            "world_height": 25600,
        },
        "tile_count": 2500,
        "tiles": tiles,
    }
    return manifest, signature


def write_scene() -> None:
    text = SCENE_BASE.read_text(encoding="utf-8")
    text = text.replace("  useV2IRepairAuditPreviewRuntimePackageForPlayMode: 1", "  useV2IRepairAuditPreviewRuntimePackageForPlayMode: 0")
    if "useV2ISelectedHdLocalRepairReviewRuntimePackageForPlayMode" not in text:
        text = text.replace(
            "  useV2IRepairAuditPreviewRuntimePackageForPlayMode: 0\n",
            "  useV2IRepairAuditPreviewRuntimePackageForPlayMode: 0\n  useV2ISelectedHdLocalRepairReviewRuntimePackageForPlayMode: 1\n",
        )
    else:
        text = text.replace("  useV2ISelectedHdLocalRepairReviewRuntimePackageForPlayMode: 0", "  useV2ISelectedHdLocalRepairReviewRuntimePackageForPlayMode: 1")
    text = text.replace("initialAuditChunkX: 16", "initialAuditChunkX: 54")
    text = text.replace("initialAuditChunkY: 19", "initialAuditChunkY: 9")
    text = text.replace("initialAuditZoom: 1", "initialAuditZoom: 1")
    text = text.replace(
        "initialAuditViewLabel: Audit nettete native 1x - candidat V2I repair",
        "initialAuditViewLabel: Audit local repair C54_09 / C53_26 - selected HD sibling",
    )
    SCENE_TARGET.write_text(text, encoding="utf-8")


def main() -> None:
    DOCS.mkdir(parents=True, exist_ok=True)
    copy_baseline_pngs()
    changed = apply_patches()
    manifest, signature = build_manifest(changed)
    (TARGET / "runtime_manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    validation = {
        "status": "PASS",
        "utc": datetime.now(timezone.utc).isoformat(),
        "runtime_root": str(TARGET),
        "source_master_sha256": signature,
        "tile_count": 2500,
        "rows": 50,
        "columns": 50,
        "tile_size": 512,
        "runtime_tile_size": 516,
        "gutter": 2,
        "baseline_resource_root": str(BASELINE),
        "baseline_preserved_in_place": "YES",
        "patched_tile_count": len(changed),
        "patched_tiles": changed,
        "defect_001_scope": "R02C46..R02C48 plus R03C46..R03C48, review context R01..R03/C46..C48",
        "defect_002_scope": "R19C45..R19C47, review context R18..R20/C45..C47",
        "unity_audit_preview": "YES",
        "ready_for_qa_builderc": "NO",
        "ready_for_unity_handoff": "NO",
        "master_25600_authorized": "NO",
    }
    (TARGET / "runtime_validation.json").write_text(json.dumps(validation, indent=2), encoding="utf-8")
    write_scene()

    receipt = {
        "SELECTED_HD_LOCAL_REPAIR_REVIEW_BUNDLE": "PASS",
        "utc": validation["utc"],
        "runtime_root": str(TARGET),
        "preview_scene": str(SCENE_TARGET),
        "source_master_sha256": signature,
        "tile_count": 2500,
        "runtime_tiles_516x516_g2": "PASS",
        "baseline_v2i_repair_preserved_in_place": "YES",
        "patched_tile_count": len(changed),
        "patched_tiles": changed,
        "defect_001_local_scope": validation["defect_001_scope"],
        "defect_002_local_scope": validation["defect_002_scope"],
        "ready_for_local_unity_retest": "YES",
        "ready_for_qa_builderc": "NO",
        "ready_for_unity_handoff": "NO",
        "master_25600_authorized": "NO",
        "note": "Audit sibling only. Unity must validate C54_09/R02C47 and C53_26/R19C46 before any QA promotion.",
    }
    (DOCS / "SELECTED_HD_LOCAL_REPAIR_REVIEW_RECEIPT.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8")
    (DOCS / "SELECTED_HD_LOCAL_REPAIR_REVIEW.md").write_text(
        "# Wave6 50x50 Selected HD Local Repair Review\n\n"
        "STATUS=REVIEW_BUNDLE_READY_FOR_LOCAL_UNITY_RETEST\n\n"
        f"runtime_root={TARGET}\n"
        f"preview_scene={SCENE_TARGET}\n"
        f"source_master_sha256={signature}\n\n"
        "## Scope\n\n"
        "- Baseline package `v2i_repair` remains intact.\n"
        "- This sibling package was rebuilt from that baseline and only local defect tiles were overlaid.\n"
        "- DEFECT-001 scope: R02C46..R02C48 and R03C46..R03C48, with R01..R03/C46..C48 review context.\n"
        "- DEFECT-002 scope: R19C45..R19C47, with R18..R20/C45..C47 review context.\n\n"
        "## Gates\n\n"
        "- RUNTIME_TILES_516X516_G2=PASS\n"
        "- TILE_COUNT_2500=PASS\n"
        "- BASELINE_V2I_REPAIR_PRESERVED_IN_PLACE=YES\n"
        "- READY_FOR_LOCAL_UNITY_RETEST=YES\n"
        "- READY_FOR_QA_BUILDERC=NO\n"
        "- READY_FOR_UNITY_HANDOFF=NO\n"
        "- MASTER_25600_AUTHORIZED=NO\n\n"
        "Unity must validate C54_09/R02C47 and C53_26/R19C46 before promotion.\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
