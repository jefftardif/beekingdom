from __future__ import annotations

import hashlib
import json
import math
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(r"C:\projets\beekingdomgame-master")
OUT = ROOT / "artifacts" / "WorldMapRuntimeEntitiesWave1" / "premium"
SIZE = 512


def rgba(hex_color: str, alpha: int = 255) -> tuple[int, int, int, int]:
    hex_color = hex_color.lstrip("#")
    return tuple(int(hex_color[i : i + 2], 16) for i in (0, 2, 4)) + (alpha,)


def poly(cx: float, cy: float, points: list[tuple[float, float]], scale: float) -> list[tuple[float, float]]:
    return [(cx + x * scale, cy + y * scale) for x, y in points]


def draw_soft_shadow(base: Image.Image, bbox: tuple[int, int, int, int], opacity: int = 70) -> None:
    shadow = Image.new("RGBA", base.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(shadow)
    draw.ellipse(bbox, fill=(0, 0, 0, opacity))
    shadow = shadow.filter(ImageFilter.GaussianBlur(12))
    base.alpha_composite(shadow)


def line(draw: ImageDraw.ImageDraw, pts, fill, width=4):
    draw.line(pts, fill=fill, width=width, joint="curve")


def add_texture(draw: ImageDraw.ImageDraw, rng: random.Random, mask_bbox: tuple[int, int, int, int], color: tuple[int, int, int, int], count: int) -> None:
    x0, y0, x1, y1 = mask_bbox
    for _ in range(count):
        x = rng.randint(x0, x1)
        y = rng.randint(y0, y1)
        r = rng.randint(1, 4)
        a = rng.randint(26, 74)
        c = (color[0], color[1], color[2], a)
        draw.ellipse((x - r, y - r, x + r, y + r), fill=c)


def hive_neutral(level: int) -> Image.Image:
    rng = random.Random(4100 + level)
    image = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw_soft_shadow(image, (128, 332, 386, 420), 42 + level * 2)
    draw = ImageDraw.Draw(image)

    scale = {1: 0.74, 4: 0.88, 7: 1.03, 9: 1.14}[level]
    cx, cy = 256, 246
    top = poly(cx, cy, [(0, -120), (116, -58), (116, 34), (0, 96), (-116, 34), (-116, -58)], scale)
    left = poly(cx, cy, [(-116, -58), (0, 4), (0, 96), (-116, 34)], scale)
    right = poly(cx, cy, [(116, -58), (0, 4), (0, 96), (116, 34)], scale)
    face = poly(cx, cy, [(-116, -58), (0, -120), (116, -58), (0, 4)], scale)

    draw.polygon(left, fill=rgba("#9b6828", 248), outline=rgba("#4b3014", 245))
    draw.polygon(right, fill=rgba("#6e481f", 248), outline=rgba("#4b3014", 245))
    draw.polygon(face, fill=rgba("#d8942e", 250), outline=rgba("#f0bd48", 245))
    draw.polygon(top, outline=rgba("#442913", 230))

    add_texture(draw, rng, (120, 116, 392, 356), rgba("#fff0a0"), 130 + level * 12)
    add_texture(draw, rng, (112, 156, 400, 380), rgba("#3a220d"), 70 + level * 9)

    for row in range(3 + level // 3):
        for col in range(3 + (level >= 7)):
            ox = (col - 1.5) * 38 * scale + rng.randint(-4, 4)
            oy = row * 28 * scale - 38 * scale + rng.randint(-3, 3)
            rr = (10 + level * 0.55) * scale
            x, y = cx + ox, cy + oy
            draw.ellipse((x - rr, y - rr, x + rr, y + rr), fill=rgba("#211409", 235), outline=rgba("#f1bc44", 145), width=max(2, int(2 * scale)))

    if level >= 4:
        for i in range(3):
            x = int(cx - 82 * scale + i * 82 * scale)
            line(draw, [(x, cy - 128 * scale), (x + rng.randint(-18, 18), cy - 168 * scale)], rgba("#775033", 230), max(5, int(7 * scale)))
            draw.ellipse((x - 14, cy - 178 * scale, x + 14, cy - 150 * scale), fill=rgba("#55783a", 220))
    if level >= 7:
        for angle in [-36, 0, 36]:
            rad = math.radians(angle)
            x = cx + math.sin(rad) * 122 * scale
            y = cy - 74 * scale + math.cos(rad) * 18
            draw.polygon(poly(x, y, [(0, -22), (16, 8), (0, 30), (-16, 8)], 0.95), fill=rgba("#eab13e", 230), outline=rgba("#6e4319", 220))
    if level >= 9:
        line(draw, [(cx - 130 * scale, cy - 44 * scale), (cx - 164 * scale, cy - 88 * scale), (cx - 136 * scale, cy - 110 * scale)], rgba("#8f5d32", 235), 8)
        line(draw, [(cx + 130 * scale, cy - 44 * scale), (cx + 164 * scale, cy - 88 * scale), (cx + 136 * scale, cy - 110 * scale)], rgba("#8f5d32", 235), 8)

    for _ in range(16 + level * 2):
        x = rng.randint(120, 392)
        y = rng.randint(314, 394)
        draw.ellipse((x - 5, y - 2, x + 5, y + 2), fill=rgba("#6e7d3e", rng.randint(80, 145)))

    return image


def class_hive(cls: str, level: int) -> Image.Image:
    rng = random.Random(hash(cls) % 100000 + level)
    image = hive_neutral(9)
    draw = ImageDraw.Draw(image)
    cx, cy = 256, 246
    tier_scale = {10: 1.0, 20: 1.13, 35: 1.27, 50: 1.42}[level]
    accents = {
        "royal_guard": rgba("#77c7ff", 245),
        "striker": rgba("#ff654a", 245),
        "nurturer": rgba("#8df0a8", 245),
        "scout": rgba("#f1dc58", 245),
        "alchemist": rgba("#a36cff", 245),
    }
    accent = accents[cls]
    if level >= 20:
        draw.polygon(poly(cx, cy + 54, [(-164, -22), (0, -88), (164, -22), (110, 56), (0, 104), (-110, 56)], 1.0), fill=rgba("#7c5223", 210), outline=rgba("#f0bd48", 205))
    if level >= 35:
        for sx in [-138, 138]:
            draw.polygon(poly(cx + sx, cy + 4, [(0, -58), (52, -30), (52, 30), (0, 58), (-52, 30), (-52, -30)], 0.88), fill=rgba("#c9862b", 235), outline=rgba("#4b3014", 220))
            for oy in [-18, 8, 34]:
                draw.ellipse((cx + sx - 10, cy + oy - 10, cx + sx + 10, cy + oy + 10), fill=rgba("#241408", 225))
    if level >= 50:
        for sx in [-176, 176]:
            line(draw, [(cx + sx, cy - 80), (cx + sx * 1.08, cy - 156), (cx + sx * 0.82, cy - 128)], rgba("#8c5a32", 235), 10)
            draw.ellipse((cx + sx * 1.08 - 18, cy - 174, cx + sx * 1.08 + 18, cy - 138), fill=accent, outline=rgba("#f2ecff", 210), width=3)
    if cls == "royal_guard":
        for offset in [-76, 0, 76]:
            draw.polygon(poly(cx + offset * 0.35, cy - 132 * tier_scale, [(0, -28), (22, 12), (0, 34), (-22, 12)], 1), fill=accent, outline=rgba("#315c82", 220))
        line(draw, [(126, 188), (184, 110), (256, 166), (328, 110), (386, 188)], accent, int(11 * tier_scale))
    elif cls == "striker":
        for x, y, dx in [(116, 176, -48), (396, 176, 48), (174, 122, -28), (338, 122, 28)]:
            line(draw, [(x, y), (x + dx, y - 74 * tier_scale)], accent, int(11 * tier_scale))
    elif cls == "nurturer":
        for x in [154, 358]:
            draw.ellipse((x - 32, 112, x + 32, 176), fill=rgba("#5fdc83", 225), outline=rgba("#d7ffd6", 225), width=5)
        line(draw, [(132, 330), (194, 376), (256, 388), (318, 376), (380, 330)], accent, int(10 * tier_scale))
    elif cls == "scout":
        for x2, y2 in [(256, 22), (166, 60), (346, 60)]:
            line(draw, [(256, 126), (x2, y2)], accent, int(8 * tier_scale))
            draw.ellipse((x2 - 12, y2 - 12, x2 + 12, y2 + 12), fill=rgba("#fff2a0", 230))
    elif cls == "alchemist":
        for x, c in [(152, rgba("#63f6c3", 230)), (360, rgba("#a36cff", 230))]:
            draw.ellipse((x - 28, 106, x + 28, 162), fill=c, outline=rgba("#f2ecff", 225), width=4)
            line(draw, [(x, 106), (x, 78)], c, 6)
        line(draw, [(132, 134), (190, 70), (256, 96), (322, 70), (380, 134)], accent, int(9 * tier_scale))
    add_texture(draw, rng, (96, 82, 416, 392), accent, 55 + level)
    return image


def save_asset(image: Image.Image, path: Path) -> dict:
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path)
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    bbox = image.getchannel("A").getbbox()
    return {
        "file": str(path.relative_to(OUT)).replace("\\", "/"),
        "size": [image.width, image.height],
        "alpha_bbox": list(bbox) if bbox else None,
        "sha256": digest,
    }


def sheet(lot: str, entries: list[dict]) -> str:
    bg = rgba("#192619")
    cell_w, cell_h = 196, 236
    cols = min(5, len(entries))
    rows = (len(entries) + cols - 1) // cols
    canvas = Image.new("RGBA", (cols * cell_w, rows * cell_h), bg)
    draw = ImageDraw.Draw(canvas)
    for i, entry in enumerate(entries):
        asset = Image.open(OUT / entry["file"]).convert("RGBA")
        x = (i % cols) * cell_w
        y = (i // cols) * cell_h
        draw.rectangle((x + 6, y + 6, x + cell_w - 6, y + cell_h - 6), outline=rgba("#6d8f60"), width=2)
        for scale, sx in [(1.0, 22), (0.5, 128), (0.25, 166)]:
            preview = asset.resize((int(128 * scale), int(128 * scale)), Image.Resampling.LANCZOS)
            canvas.alpha_composite(preview, (x + sx, y + 34))
        draw.text((x + cell_w // 2, y + 190), entry["id"], fill=rgba("#f5e8be"), anchor="mm")
        draw.text((x + cell_w // 2, y + 210), "100 / 50 / 25", fill=rgba("#b8c7a4"), anchor="mm")
    path = OUT / lot / f"contact_{lot}.png"
    canvas.convert("RGB").save(path)
    return str(path.relative_to(OUT)).replace("\\", "/")


def export_lot(lot: str) -> dict:
    if lot == "H1":
        specs = [(f"hive_neutral_l{lvl}", hive_neutral(lvl)) for lvl in [1, 4, 7, 9]]
    elif lot == "H2":
        specs = [(f"hive_{cls}_l10", class_hive(cls, 10)) for cls in ["royal_guard", "striker", "nurturer", "scout", "alchemist"]]
    elif lot == "H3":
        specs = []
        for lvl in [20, 35, 50]:
            for cls in ["royal_guard", "striker", "nurturer", "scout", "alchemist"]:
                specs.append((f"hive_{cls}_l{lvl}", class_hive(cls, lvl)))
    else:
        raise ValueError(lot)

    entries = []
    for asset_id, image in specs:
        rel = Path(lot) / f"{asset_id}.png"
        info = save_asset(image, OUT / rel)
        info["id"] = asset_id
        info["lot"] = lot
        info["transparent_background"] = True
        info["no_text_ui_route_ring"] = True
        entries.append(info)
    contact = sheet(lot, entries)
    manifest = {
        "lot": lot,
        "status": "premium_local_export",
        "asset_count": len(entries),
        "contact_sheet": contact,
        "assets": entries,
        "constraints": {
            "transparent_png": True,
            "no_text": True,
            "no_ui": True,
            "no_route": True,
            "no_painted_ring": True,
            "no_terrain_changes": True,
        },
    }
    path = OUT / lot / f"manifest_{lot}.json"
    path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    return manifest


def main() -> None:
    import argparse

    parser = argparse.ArgumentParser()
    parser.add_argument("lot", choices=["H1", "H2", "H3"])
    args = parser.parse_args()
    manifest = export_lot(args.lot)
    print(json.dumps({"lot": args.lot, "asset_count": manifest["asset_count"], "contact_sheet": manifest["contact_sheet"]}, indent=2))


if __name__ == "__main__":
    main()
