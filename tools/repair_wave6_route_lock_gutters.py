from __future__ import annotations

import json
import shutil
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / "Assets" / "BeeKingdom" / "Playground" / "Resources" / "WorldMapWave6Runtime" / "UIB_ImmenseContinuousMaster50x50_route_lock_coherent_proof"
REPORT_DIR = ROOT / "Docs" / "BuilderA" / "WorldMapWave6_50x50_RouteLockCoherentProofPreview"
RECEIPT = REPORT_DIR / "WorldMapWave6_RouteLockCoherentProof_GutterRepairReceipt.json"


def load_manifest() -> dict:
    with (PACKAGE / "runtime_manifest.json").open("r", encoding="utf-8") as handle:
        return json.load(handle)


def main() -> int:
    manifest = load_manifest()
    grid = manifest["grid"]
    rows = int(grid["rows"])
    columns = int(grid["columns"])
    tile_size = int(grid["tile_size"])
    runtime_tile_size = int(grid["runtime_tile_size"])
    gutter = int(grid["gutter"])

    backup_dir = PACKAGE / f"_pre_gutter_repair_backup_{datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%SZ')}"
    backup_dir.mkdir(parents=True, exist_ok=False)

    cores: dict[tuple[int, int], Image.Image] = {}
    for row in range(rows):
        for column in range(columns):
            path = PACKAGE / f"R{row:02d}C{column:02d}_g{gutter}.png"
            shutil.copy2(path, backup_dir / path.name)
            image = Image.open(path).convert("RGBA")
            if image.size != (runtime_tile_size, runtime_tile_size):
                raise ValueError(f"Unexpected tile dimensions for {path}: {image.size}")
            cores[(row, column)] = image.crop((gutter, gutter, gutter + tile_size, gutter + tile_size))

    def core_at(row: int, column: int) -> Image.Image:
        row = min(max(row, 0), rows - 1)
        column = min(max(column, 0), columns - 1)
        return cores[(row, column)]

    repaired_count = 0
    for row in range(rows):
        for column in range(columns):
            canvas = Image.new("RGBA", (runtime_tile_size, runtime_tile_size))
            center = core_at(row, column)
            canvas.paste(center, (gutter, gutter))

            canvas.paste(core_at(row, column - 1).crop((tile_size - gutter, 0, tile_size, tile_size)), (0, gutter))
            canvas.paste(core_at(row, column + 1).crop((0, 0, gutter, tile_size)), (gutter + tile_size, gutter))
            canvas.paste(core_at(row - 1, column).crop((0, tile_size - gutter, tile_size, tile_size)), (gutter, 0))
            canvas.paste(core_at(row + 1, column).crop((0, 0, tile_size, gutter)), (gutter, gutter + tile_size))

            canvas.paste(core_at(row - 1, column - 1).crop((tile_size - gutter, tile_size - gutter, tile_size, tile_size)), (0, 0))
            canvas.paste(core_at(row - 1, column + 1).crop((0, tile_size - gutter, gutter, tile_size)), (gutter + tile_size, 0))
            canvas.paste(core_at(row + 1, column - 1).crop((tile_size - gutter, 0, tile_size, gutter)), (0, gutter + tile_size))
            canvas.paste(core_at(row + 1, column + 1).crop((0, 0, gutter, gutter)), (gutter + tile_size, gutter + tile_size))

            canvas.convert("RGB").save(PACKAGE / f"R{row:02d}C{column:02d}_g{gutter}.png")
            repaired_count += 1

    receipt = {
        "schema": "bee-kingdom.world-map.wave6-route-lock-gutter-repair.v1",
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "status": "PASS_REPAIRED",
        "package": str(PACKAGE),
        "backup_dir": str(backup_dir),
        "repaired_tile_count": repaired_count,
        "method": "Rebuilt every 2px runtime gutter from adjacent tile cores; image cores were preserved.",
        "source_master_sha256": manifest.get("source", {}).get("master_sha256"),
        "gate_decision": {
            "READY_FOR_FULL_50X50_TILE_BUILD": "NO",
            "READY_FOR_QA_BUILDERC": "NO",
            "READY_FOR_UNITY_HANDOFF": "NO",
            "READY_FOR_CANONICAL_SWAP": "NO",
            "PREMIUM_50X50_VALIDATED": "NO",
        },
    }
    REPORT_DIR.mkdir(parents=True, exist_ok=True)
    RECEIPT.write_text(json.dumps(receipt, indent=2), encoding="utf-8")
    print(json.dumps({"status": receipt["status"], "repaired_tile_count": repaired_count}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
