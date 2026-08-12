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


def rgba(h: str, a: int = 255):
    h = h.lstrip("#")
    return tuple(int(h[i : i + 2], 16) for i in (0, 2, 4)) + (a,)


def shadow(img: Image.Image, bbox, opacity=70):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    draw.ellipse(bbox, fill=(0, 0, 0, opacity))
    img.alpha_composite(layer.filter(ImageFilter.GaussianBlur(14)))


def line(draw, pts, fill, width=5):
    draw.line(pts, fill=fill, width=width, joint="curve")


def leg(draw, x, y, dx, dy, color, width):
    line(draw, [(x, y), (x + dx * 0.55, y + dy * 0.35), (x + dx, y + dy)], color, width)


def wing(draw, pts, fill=rgba("#dcecff", 150)):
    draw.polygon(pts, fill=fill, outline=rgba("#edf6ff", 190))


def eyes(draw, cx, cy, scale=1):
    for ox in [-10 * scale, 10 * scale]:
        draw.ellipse((cx + ox - 6 * scale, cy - 5 * scale, cx + ox + 6 * scale, cy + 7 * scale), fill=rgba("#f7e46d", 240))


def beast(asset_id: str, tier: int, variant: int) -> Image.Image:
    rng = random.Random(tier * 900 + variant * 37)
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    cx, cy = 256, 256
    scale = 0.72 + tier * 0.08
    shadow(img, (120, 330, 392, 412), 36 + tier * 8)

    if tier == 1 and variant == 1:  # aphid thief
        body = rgba("#587d34", 245)
        draw.ellipse((cx - 74, cy - 32, cx + 70, cy + 54), fill=body, outline=rgba("#26391c", 230), width=5)
        draw.ellipse((cx - 44, cy - 70, cx + 48, cy - 12), fill=rgba("#6d9641", 245), outline=rgba("#26391c", 230), width=5)
        line(draw, [(cx - 24, cy - 68), (cx - 70, cy - 126), (cx - 42, cy - 92)], rgba("#a4ca72", 235), 6)
        line(draw, [(cx + 24, cy - 68), (cx + 70, cy - 126), (cx + 42, cy - 92)], rgba("#a4ca72", 235), 6)
        eyes(draw, cx, cy - 44, 1)
    elif tier == 1:  # red mite
        draw.ellipse((cx - 62, cy - 52, cx + 62, cy + 62), fill=rgba("#9c3c2e", 245), outline=rgba("#3a1d16", 230), width=5)
        for sx in [-1, 1]:
            for yy in [-34, -8, 20]:
                leg(draw, cx + sx * 42, cy + yy, sx * 64, yy * 0.18, rgba("#d77a55", 235), 6)
        eyes(draw, cx, cy - 34, 0.8)
    elif tier == 2 and variant == 1:  # cutter ant
        for x, w, c in [(174, 54, "#5a2e1c"), (240, 66, "#6c361f"), (318, 72, "#4b2819")]:
            draw.ellipse((x - w, cy - 34, x + w, cy + 38), fill=rgba(c, 245), outline=rgba("#24130c", 230), width=5)
        for x in [190, 236, 292, 340]:
            leg(draw, x, cy + 10, -48, 62, rgba("#d27a3c", 235), 6)
            leg(draw, x, cy - 8, 46, -58, rgba("#d27a3c", 235), 6)
        line(draw, [(136, cy - 18), (92, cy - 56), (116, cy - 22)], rgba("#d27a3c", 235), 6)
        eyes(draw, 142, cy - 18, 0.8)
    elif tier == 2:  # shield beetle
        draw.pieslice((cx - 92, cy - 70, cx + 92, cy + 92), 190, 350, fill=rgba("#3f6230", 245), outline=rgba("#1d2a18", 235), width=5)
        draw.polygon([(cx - 92, cy + 10), (cx, cy - 86), (cx + 92, cy + 10), (cx, cy + 96)], fill=rgba("#5f843e", 220), outline=rgba("#26351e", 230))
        line(draw, [(cx, cy - 78), (cx, cy + 88)], rgba("#c9a94c", 180), 5)
        for sx in [-1, 1]:
            leg(draw, cx + sx * 60, cy + 14, sx * 62, 54, rgba("#7ea05a", 230), 6)
    elif tier == 3 and variant == 1:  # jumping spider
        draw.ellipse((cx - 82, cy - 62, cx + 82, cy + 80), fill=rgba("#302640", 245), outline=rgba("#15101e", 235), width=6)
        draw.ellipse((cx - 58, cy - 94, cx + 58, cy - 18), fill=rgba("#3e3155", 245), outline=rgba("#15101e", 235), width=5)
        for sx in [-1, 1]:
            for y in [-50, -12, 28, 58]:
                leg(draw, cx + sx * 58, cy + y, sx * (86 + rng.randint(-8, 8)), y * 0.55, rgba("#8c7bd8", 235), 7)
        eyes(draw, cx, cy - 62, 1.3)
    elif tier == 3:  # robber fly
        draw.ellipse((cx - 42, cy - 94, cx + 42, cy + 78), fill=rgba("#5b5530", 245), outline=rgba("#282614", 235), width=5)
        wing(draw, [(cx - 24, cy - 42), (cx - 128, cy - 116), (cx - 74, cy + 18)])
        wing(draw, [(cx + 24, cy - 42), (cx + 128, cy - 116), (cx + 74, cy + 18)])
        line(draw, [(cx, cy - 98), (cx, cy - 156)], rgba("#d0b86a", 235), 7)
        eyes(draw, cx, cy - 104, 1)
    elif tier == 4 and variant == 1:  # mantis
        draw.ellipse((cx - 38, cy - 112, cx + 38, cy + 96), fill=rgba("#426e34", 245), outline=rgba("#1c3118", 235), width=6)
        draw.ellipse((cx - 44, cy - 164, cx + 44, cy - 86), fill=rgba("#5f9148", 245), outline=rgba("#1c3118", 235), width=5)
        line(draw, [(cx - 34, cy - 82), (cx - 132, cy - 154), (cx - 88, cy - 38)], rgba("#91be68", 240), 9)
        line(draw, [(cx + 34, cy - 82), (cx + 132, cy - 154), (cx + 88, cy - 38)], rgba("#91be68", 240), 9)
        eyes(draw, cx, cy - 130, 1)
    elif tier == 4:  # centipede
        for i in range(8):
            x = cx - 112 + i * 32
            y = cy + math.sin(i * 0.8) * 22
            draw.ellipse((x - 28, y - 24, x + 32, y + 24), fill=rgba("#763d2d", 245), outline=rgba("#2a1510", 230), width=4)
            leg(draw, x, y + 14, -22, 38, rgba("#c26b4a", 230), 5)
            leg(draw, x, y - 14, 22, -38, rgba("#c26b4a", 230), 5)
        eyes(draw, cx - 124, cy - 8, 0.7)
    elif tier == 5 and variant == 1:  # hornet brigand
        draw.ellipse((cx - 110, cy - 52, cx + 110, cy + 56), fill=rgba("#603b14", 245), outline=rgba("#241508", 235), width=6)
        for x in [-58, 0, 58]:
            line(draw, [(cx + x, cy - 46), (cx + x, cy + 54)], rgba("#e8b735", 220), 8)
        wing(draw, [(cx - 28, cy - 46), (cx - 148, cy - 138), (cx - 72, cy - 6)])
        wing(draw, [(cx + 28, cy - 46), (cx + 148, cy - 138), (cx + 72, cy - 6)])
        eyes(draw, cx - 86, cy - 22, 1)
    elif tier == 5:  # stag beetle raider
        draw.ellipse((cx - 94, cy - 70, cx + 94, cy + 84), fill=rgba("#332218", 245), outline=rgba("#160d09", 235), width=6)
        line(draw, [(cx - 52, cy - 78), (cx - 132, cy - 150), (cx - 92, cy - 70)], rgba("#a46237", 235), 10)
        line(draw, [(cx + 52, cy - 78), (cx + 132, cy - 150), (cx + 92, cy - 70)], rgba("#a46237", 235), 10)
        line(draw, [(cx, cy - 72), (cx, cy + 76)], rgba("#c08b4b", 160), 5)
        eyes(draw, cx, cy - 58, 1)
    elif tier == 6 and variant == 1:  # root scorpion
        draw.ellipse((cx - 118, cy - 54, cx + 112, cy + 70), fill=rgba("#4a2b1f", 245), outline=rgba("#1b100b", 235), width=7)
        line(draw, [(cx + 68, cy - 54), (cx + 118, cy - 156), (cx + 44, cy - 188), (cx + 8, cy - 118)], rgba("#c27a55", 240), 10)
        line(draw, [(cx - 104, cy - 20), (cx - 176, cy - 74), (cx - 146, cy - 10)], rgba("#c27a55", 240), 11)
        line(draw, [(cx + 104, cy - 20), (cx + 176, cy - 74), (cx + 146, cy - 10)], rgba("#c27a55", 240), 11)
        eyes(draw, cx - 78, cy - 28, 0.8)
    elif tier == 6:  # armored tarantula
        draw.ellipse((cx - 98, cy - 82, cx + 98, cy + 88), fill=rgba("#2b2430", 245), outline=rgba("#120e16", 235), width=7)
        draw.ellipse((cx - 70, cy - 122, cx + 70, cy - 24), fill=rgba("#3f3148", 245), outline=rgba("#120e16", 235), width=6)
        for sx in [-1, 1]:
            for yy in [-78, -34, 12, 58]:
                leg(draw, cx + sx * 72, cy + yy, sx * 118, yy * 0.62, rgba("#7c668f", 235), 9)
        eyes(draw, cx, cy - 86, 1.2)
    elif tier == 7 and variant == 1:  # ancient hornet queen
        draw.ellipse((cx - 142, cy - 62, cx + 142, cy + 82), fill=rgba("#663d13", 245), outline=rgba("#231507", 235), width=8)
        draw.ellipse((cx - 58, cy - 134, cx + 58, cy - 34), fill=rgba("#804d18", 245), outline=rgba("#f1d35e", 230), width=6)
        wing(draw, [(cx - 36, cy - 58), (cx - 206, cy - 170), (cx - 92, cy - 16)], rgba("#dcecff", 165))
        wing(draw, [(cx + 36, cy - 58), (cx + 206, cy - 170), (cx + 92, cy - 16)], rgba("#dcecff", 165))
        line(draw, [(cx - 36, cy - 134), (cx, cy - 196), (cx + 36, cy - 134)], rgba("#f1d35e", 245), 8)
        for x in [-72, 0, 72]:
            line(draw, [(cx + x, cy - 56), (cx + x, cy + 78)], rgba("#e6b639", 210), 8)
        eyes(draw, cx, cy - 96, 1.2)
    else:  # titan stag beetle
        draw.ellipse((cx - 138, cy - 82, cx + 138, cy + 94), fill=rgba("#2d1f16", 245), outline=rgba("#110b07", 235), width=8)
        draw.polygon([(cx - 98, cy - 92), (cx, cy - 158), (cx + 98, cy - 92), (cx, cy - 34)], fill=rgba("#4a3020", 245), outline=rgba("#d19a56", 225))
        line(draw, [(cx - 78, cy - 128), (cx - 196, cy - 226), (cx - 152, cy - 104)], rgba("#b67843", 245), 12)
        line(draw, [(cx + 78, cy - 128), (cx + 196, cy - 226), (cx + 152, cy - 104)], rgba("#b67843", 245), 12)
        eyes(draw, cx, cy - 96, 1.1)

    for _ in range(24 + tier * 10):
        x = rng.randint(116, 396)
        y = rng.randint(126, 370)
        draw.ellipse((x - 2, y - 2, x + 2, y + 2), fill=(255, 230, 160, rng.randint(25, 70)))
    return img


SPECS = [
    ("beast_t1_aphid_thief", 1, 1, "solo nuisance"),
    ("beast_t1_red_mite", 1, 2, "solo nuisance"),
    ("beast_t2_cutter_ant", 2, 1, "small group harass"),
    ("beast_t2_shield_beetle", 2, 2, "small group guard"),
    ("beast_t3_jumping_spider", 3, 1, "solo elite ambush"),
    ("beast_t3_robber_fly", 3, 2, "solo aerial ambush"),
    ("beast_t4_mantis_predator", 4, 1, "elite burst"),
    ("beast_t4_centipede_runner", 4, 2, "pack pressure"),
    ("beast_t5_hornet_brigand", 5, 1, "squad aerial threat"),
    ("beast_t5_stag_beetle_raider", 5, 2, "squad bruiser"),
    ("beast_t6_root_scorpion", 6, 1, "mini raid tank"),
    ("beast_t6_armored_tarantula", 6, 2, "mini raid anchor"),
    ("beast_t7_ancient_hornet_queen", 7, 1, "raid boss aerial"),
    ("beast_t7_titan_stag_beetle", 7, 2, "raid boss ground"),
]


def save(img: Image.Image, path: Path) -> dict:
    path.parent.mkdir(parents=True, exist_ok=True)
    img.save(path)
    return {
        "file": str(path.relative_to(OUT)).replace("\\", "/"),
        "size": [img.width, img.height],
        "alpha_bbox": list(img.getchannel("A").getbbox()),
        "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
        "transparent_background": True,
        "no_text_ui_route_ring": True,
    }


def contact(entries: list[dict]) -> str:
    cols, cell_w, cell_h = 7, 170, 226
    rows = 2
    canvas = Image.new("RGBA", (cols * cell_w, rows * cell_h), rgba("#172318"))
    draw = ImageDraw.Draw(canvas)
    for i, entry in enumerate(entries):
        x = (i % cols) * cell_w
        y = (i // cols) * cell_h
        draw.rectangle((x + 5, y + 6, x + cell_w - 5, y + cell_h - 6), outline=rgba("#6d8f60"), width=2)
        asset = Image.open(OUT / entry["file"]).convert("RGBA")
        for scale, sx, sy in [(1.0, 16, 30), (0.5, 106, 42), (0.25, 138, 88)]:
            preview = asset.resize((int(112 * scale), int(112 * scale)), Image.Resampling.LANCZOS)
            canvas.alpha_composite(preview, (x + sx, y + sy))
        draw.text((x + cell_w // 2, y + 176), entry["id"].replace("beast_", ""), fill=rgba("#f5e8be"), anchor="mm")
        draw.text((x + cell_w // 2, y + 198), f"T{entry['tier']} 100/50/25", fill=rgba("#b8c7a4"), anchor="mm")
    path = OUT / "M1" / "contact_M1.png"
    canvas.convert("RGB").save(path)
    return str(path.relative_to(OUT)).replace("\\", "/")


def main():
    entries = []
    for asset_id, tier, variant, role in SPECS:
        info = save(beast(asset_id, tier, variant), OUT / "M1" / f"{asset_id}.png")
        info["id"] = asset_id
        info["tier"] = tier
        info["role"] = role
        entries.append(info)
    manifest = {
        "lot": "M1",
        "status": "premium_local_export",
        "asset_count": len(entries),
        "contact_sheet": contact(entries),
        "assets": entries,
        "constraints": {
            "transparent_png": True,
            "two_distinct_per_tier": True,
            "no_bear": True,
            "no_text": True,
            "no_ui": True,
            "no_route": True,
            "no_painted_ring": True,
            "no_terrain_changes": True,
        },
    }
    (OUT / "M1" / "manifest_M1.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print(json.dumps({"lot": "M1", "asset_count": len(entries), "contact_sheet": manifest["contact_sheet"]}, indent=2))


if __name__ == "__main__":
    main()
