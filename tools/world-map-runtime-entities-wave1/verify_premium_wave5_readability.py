from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(r"C:\projets\beekingdomgame-master")
PREMIUM = ROOT / "artifacts" / "WorldMapRuntimeEntitiesWave1" / "premium"
REPORT = ROOT / "Docs" / "WorldMapRuntimeEntitiesWave1" / "PremiumWave5Readability_Report.md"
TILE = ROOT / "Assets" / "BeeKingdom" / "Playground" / "Resources" / "WorldMapWave5Runtime" / "UIB_ImmenseContinuousMaster25x25_v1" / "R12C12_g2.png"


LOTS = ["H1", "H2", "H3", "R1", "R2", "R3", "M1"]


def rgba(h: str, a: int = 255):
    h = h.lstrip("#")
    return tuple(int(h[i : i + 2], 16) for i in (0, 2, 4)) + (a,)


def load_entries():
    entries = []
    for lot in LOTS:
        manifest = json.loads((PREMIUM / lot / f"manifest_{lot}.json").read_text())
        for asset in manifest["assets"]:
            entries.append({"lot": lot, **asset})
    return entries


def make_sheet(entries):
    terrain = Image.open(TILE).convert("RGBA")
    crop = terrain.crop((96, 96, 416, 416)).resize((168, 168), Image.Resampling.LANCZOS)
    cols, cell_w, cell_h = 7, 190, 250
    rows = (len(entries) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * cell_w, rows * cell_h), rgba("#111812"))
    draw = ImageDraw.Draw(sheet)
    failures = []
    for i, entry in enumerate(entries):
        x = (i % cols) * cell_w
        y = (i // cols) * cell_h
        draw.rectangle((x + 5, y + 6, x + cell_w - 5, y + cell_h - 6), outline=rgba("#6d8f60"), width=2)
        sheet.alpha_composite(crop, (x + 11, y + 16))
        asset = Image.open(PREMIUM / entry["file"]).convert("RGBA")
        for scale, ox, oy in [(1.0, 32, 40), (0.5, 124, 46), (0.25, 152, 92)]:
            size = int(112 * scale)
            preview = asset.resize((size, size), Image.Resampling.LANCZOS)
            sheet.alpha_composite(preview, (x + ox, y + oy))
        alpha = asset.getchannel("A")
        bbox = alpha.getbbox()
        if not bbox or asset.getpixel((0, 0))[3] != 0:
            failures.append(entry["file"])
        draw.text((x + cell_w // 2, y + 200), entry["id"], fill=rgba("#f5e8be"), anchor="mm")
        draw.text((x + cell_w // 2, y + 222), f"{entry['lot']} on Wave5 100/50/25", fill=rgba("#b8c7a4"), anchor="mm")
    path = PREMIUM / "readability_wave5_all_lots.png"
    sheet.convert("RGB").save(path)
    return path, failures


def main():
    entries = load_entries()
    sheet, failures = make_sheet(entries)
    manifest = {
        "status": "PASS" if not failures else "FAIL",
        "asset_count": len(entries),
        "wave5_tile_source": str(TILE),
        "readability_sheet": str(sheet),
        "scales": ["100%", "50%", "25%"],
        "failures": failures,
        "sha256": hashlib.sha256(sheet.read_bytes()).hexdigest(),
    }
    (PREMIUM / "manifest_wave5_readability.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    REPORT.write_text(
        "# WorldMap Runtime Entities Wave1 - Premium Wave5 Readability Report\n\n"
        "## Statut\n\n"
        f"{manifest['status']}\n\n"
        "## Verification\n\n"
        f"- Assets verifies: {manifest['asset_count']}\n"
        "- Echelles: 100 %, 50 %, 25 %.\n"
        f"- Fond Wave5 lu en source: `{TILE}`\n"
        f"- Planche: `{sheet}`\n"
        f"- SHA-256 planche: `{manifest['sha256']}`\n\n"
        "## Contraintes\n\n"
        "- Aucune tuile Wave5 modifiee.\n"
        "- Aucun master terrain modifie.\n"
        "- Aucun BearDen modifie.\n"
        "- Aucun APK, serveur, remote ou donnees reelles.\n\n"
        "## Resultat\n\n"
        + ("PASS: tous les assets ont alpha non vide et coin transparent; planche 100/50/25 produite sur fond Wave5.\n" if not failures else "FAIL: " + ", ".join(failures) + "\n"),
        encoding="utf-8",
    )
    print(json.dumps({"asset_count": len(entries), "status": manifest["status"], "sheet": str(sheet)}, indent=2))


if __name__ == "__main__":
    main()
