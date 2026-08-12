from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(r"C:\projets\beekingdomgame-master")
RUNTIME = ROOT / "Assets" / "BeeKingdom" / "Playground" / "Resources" / "WorldMapWave6Runtime"
OUT = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging" / "local_repair_selected_hd_candidate_20260717" / "proof"

PACKAGES = [
    "UIB_ImmenseContinuousMaster50x50_v2i_repair_audit_preview",
    "UIB_ImmenseContinuousMaster50x50_v2i_thread2_native_repair_audit",
    "UIB_ImmenseContinuousMaster50x50_v2i_native_audit_preview",
    "UIB_ImmenseContinuousMaster50x50_v3e_candidate",
    "UIB_ImmenseContinuousMaster50x50_v3o_reduced_audit_preview",
    "UIB_ImmenseContinuousMaster50x50_route_lock_coherent_proof",
    "UIB_ImmenseContinuousMaster50x50_route_lock_8192_scale_bridge_proof",
]


def name(r: int, c: int) -> str:
    return f"R{r:02d}C{c:02d}_g2.png"


def load_mosaic(pkg: str, rows: list[int], cols: list[int]) -> Image.Image | None:
    root = RUNTIME / pkg
    tiles = []
    for r in rows:
        row_tiles = []
        for c in cols:
            path = root / name(r, c)
            if not path.exists():
                return None
            row_tiles.append(Image.open(path).convert("RGB"))
        tiles.append(row_tiles)
    w, h = tiles[0][0].size
    out = Image.new("RGB", (w * len(cols), h * len(rows)))
    for ri, row in enumerate(tiles):
        for ci, im in enumerate(row):
            out.paste(im, (ci * w, ri * h))
    return out


def draw_label(draw: ImageDraw.ImageDraw, text: str, x: int, y: int) -> None:
    draw.rectangle((x, y, x + len(text) * 7 + 10, y + 18), fill=(0, 0, 0))
    draw.text((x + 4, y + 3), text, fill=(255, 255, 255))


def make_sheet(label: str, rows: list[int], cols: list[int]) -> None:
    panels = []
    for pkg in PACKAGES:
        mos = load_mosaic(pkg, rows, cols)
        if mos is None:
            continue
        small = mos.resize((mos.width // 2, mos.height // 2), Image.Resampling.LANCZOS)
        panel = Image.new("RGB", (small.width, small.height + 24), (25, 25, 25))
        panel.paste(small, (0, 24))
        draw = ImageDraw.Draw(panel)
        draw_label(draw, pkg.replace("UIB_ImmenseContinuousMaster50x50_", ""), 4, 3)
        panels.append(panel)
    if not panels:
        return
    sheet = Image.new("RGB", (panels[0].width, sum(p.height for p in panels) + 10 * (len(panels) - 1)), (16, 16, 16))
    y = 0
    for p in panels:
        sheet.paste(p, (0, y))
        y += p.height + 10
    OUT.mkdir(parents=True, exist_ok=True)
    sheet.save(OUT / f"{label}_package_comparison_sheet.png")


def main() -> None:
    make_sheet("defect001_R02C47", [1, 2, 3], [46, 47, 48])
    make_sheet("defect002_R19C46", [18, 19, 20], [45, 46, 47])


if __name__ == "__main__":
    main()
