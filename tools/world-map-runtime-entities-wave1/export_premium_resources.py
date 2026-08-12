from __future__ import annotations

import hashlib
import json
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(r"C:\projets\beekingdomgame-master")
OUT = ROOT / "artifacts" / "WorldMapRuntimeEntitiesWave1" / "premium"
SIZE = 512
KINDS = ["nectar", "pollen", "water", "wax", "honey", "royal_jelly", "propolis"]


def rgba(h: str, a: int = 255):
    h = h.lstrip("#")
    return tuple(int(h[i : i + 2], 16) for i in (0, 2, 4)) + (a,)


def shadow(image: Image.Image, bbox, opacity=62):
    layer = Image.new("RGBA", image.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    draw.ellipse(bbox, fill=(0, 0, 0, opacity))
    layer = layer.filter(ImageFilter.GaussianBlur(14))
    image.alpha_composite(layer)


def line(draw, pts, fill, width=4):
    draw.line(pts, fill=fill, width=width, joint="curve")


def draw_ground(draw, cx, cy, scale):
    pts = [(cx, cy - 36 * scale), (cx + 128 * scale, cy), (cx, cy + 54 * scale), (cx - 128 * scale, cy)]
    draw.polygon(pts, fill=rgba("#31442a", 95), outline=rgba("#6e8055", 100))


def resource(kind: str, tier: int) -> Image.Image:
    rng = random.Random(hash(kind) % 100000 + tier * 719)
    image = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    cx, cy = 256, 286
    scale = [0.72, 0.94, 1.16][tier - 1]
    shadow(image, (126, 326, 386, 404), 32 + tier * 18)
    draw_ground(draw, cx, cy + 34, scale)

    if kind == "nectar":
        count = [2, 4, 7][tier - 1]
        for i in range(count):
            x = cx + rng.randint(-86, 86) * scale
            y = cy + rng.randint(-70, 18) * scale
            r = rng.randint(22, 34) * scale
            for p in range(5):
                ox = rng.randint(-18, 18) * scale
                oy = rng.randint(-18, 18) * scale
                draw.ellipse((x + ox - r * 0.45, y + oy - r * 0.35, x + ox + r * 0.45, y + oy + r * 0.35), fill=rgba("#b96cff", 225), outline=rgba("#f6d96a", 220), width=3)
            draw.ellipse((x - r * 0.28, y - r * 0.28, x + r * 0.28, y + r * 0.28), fill=rgba("#ffe36d", 235))
            line(draw, [(x, y + r * 0.45), (x + rng.randint(-18, 18), y + 64 * scale)], rgba("#55783a", 220), 6)
    elif kind == "pollen":
        count = [3, 6, 10][tier - 1]
        for i in range(count):
            x = cx + rng.randint(-100, 100) * scale
            y = cy + rng.randint(-62, 36) * scale
            r = rng.randint(18, 30) * scale
            draw.ellipse((x - r, y - r, x + r, y + r), fill=rgba("#f2cf32", 238), outline=rgba("#fff1a0", 210), width=4)
            draw.arc((x - r, y - r, x + r, y + r), 25, 220, fill=rgba("#b8781f", 160), width=4)
    elif kind == "water":
        w, h = [92, 132, 178][tier - 1], [46, 62, 82][tier - 1]
        draw.ellipse((cx - w, cy - h // 2, cx + w, cy + h), fill=rgba("#46bfe8", 205), outline=rgba("#c6f1ff", 240), width=6)
        draw.ellipse((cx - w * 0.45, cy - h * 0.62, cx + w * 0.35, cy + h * 0.10), fill=rgba("#8fe5ff", 215))
        for _ in range(4 + tier * 3):
            x = cx + rng.randint(-w + 10, w - 10)
            y = cy + rng.randint(-h // 2, h)
            draw.ellipse((x - 4, y - 2, x + 4, y + 2), fill=rgba("#eaffff", 170))
    elif kind == "wax":
        count = [2, 4, 8][tier - 1]
        for _ in range(count):
            x = cx + rng.randint(-92, 92) * scale
            y = cy + rng.randint(-62, 46) * scale
            s = rng.randint(30, 48) * scale
            pts = [(x, y - s * 0.62), (x + s, y - s * 0.2), (x + s * 0.72, y + s * 0.52), (x - s * 0.72, y + s * 0.52), (x - s, y - s * 0.2)]
            draw.polygon(pts, fill=rgba("#eda83d", 240), outline=rgba("#6a3d14", 230))
            draw.polygon([(x, y - s * 0.32), (x + s * 0.45, y - s * 0.06), (x, y + s * 0.22), (x - s * 0.45, y - s * 0.06)], fill=rgba("#ffe088", 90))
    elif kind == "honey":
        w, h = [84, 122, 168][tier - 1], [44, 58, 76][tier - 1]
        draw.ellipse((cx - w, cy - h, cx + w, cy + h), fill=rgba("#bb6718", 226), outline=rgba("#ffd070", 240), width=7)
        draw.ellipse((cx - w * 0.62, cy - h * 1.25, cx + w * 0.62, cy - h * 0.34), fill=rgba("#ffc45b", 232))
        for _ in range(2 + tier * 3):
            x = cx + rng.randint(-w, w)
            line(draw, [(x, cy - h * 0.25), (x + rng.randint(-10, 10), cy + h * 0.9)], rgba("#ffb247", 170), 4)
    elif kind == "royal_jelly":
        count = [1, 3, 5][tier - 1]
        for i in range(count):
            x = cx + rng.randint(-72, 72) * scale
            y = cy + rng.randint(-58, 30) * scale
            r = rng.randint(30, 44) * scale
            draw.ellipse((x - r, y - r, x + r, y + r), fill=rgba("#fff4c4", 238), outline=rgba("#e1d0ff", 245), width=6)
            draw.ellipse((x - r * 0.38, y - r * 0.38, x + r * 0.38, y + r * 0.38), fill=rgba("#ead6ff", 225))
    else:
        count = [2, 4, 7][tier - 1]
        for _ in range(count):
            x = cx + rng.randint(-92, 92) * scale
            y = cy + rng.randint(-62, 38) * scale
            s = rng.randint(34, 54) * scale
            pts = [(x - s, y - s * 0.25), (x - s * 0.2, y - s), (x + s * 0.9, y - s * 0.22), (x + s * 0.65, y + s * 0.75), (x - s * 0.75, y + s * 0.56)]
            draw.polygon(pts, fill=rgba("#436b38", 235), outline=rgba("#25341f", 230))
            draw.ellipse((x - s * 0.36, y - s * 0.28, x + s * 0.36, y + s * 0.34), fill=rgba("#6d3d1f", 222))

    return image


def save(image: Image.Image, path: Path) -> dict:
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path)
    return {
        "file": str(path.relative_to(OUT)).replace("\\", "/"),
        "size": [image.width, image.height],
        "alpha_bbox": list(image.getchannel("A").getbbox()),
        "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
        "transparent_background": True,
        "no_text_ui_route_ring": True,
    }


def contact(lot: str, entries: list[dict]) -> str:
    cols, cell_w, cell_h = 7, 170, 226
    canvas = Image.new("RGBA", (cols * cell_w, cell_h), rgba("#172318"))
    draw = ImageDraw.Draw(canvas)
    for i, entry in enumerate(entries):
        x = i * cell_w
        draw.rectangle((x + 5, 6, x + cell_w - 5, cell_h - 6), outline=rgba("#6d8f60"), width=2)
        asset = Image.open(OUT / entry["file"]).convert("RGBA")
        for scale, sx, sy in [(1.0, 16, 30), (0.5, 106, 42), (0.25, 138, 88)]:
            preview = asset.resize((int(112 * scale), int(112 * scale)), Image.Resampling.LANCZOS)
            canvas.alpha_composite(preview, (x + sx, sy))
        draw.text((x + cell_w // 2, 176), entry["id"], fill=rgba("#f5e8be"), anchor="mm")
        draw.text((x + cell_w // 2, 198), "100 / 50 / 25", fill=rgba("#b8c7a4"), anchor="mm")
    path = OUT / lot / f"contact_{lot}.png"
    canvas.convert("RGB").save(path)
    return str(path.relative_to(OUT)).replace("\\", "/")


def export_lot(lot: str) -> dict:
    tier = {"R1": 1, "R2": 2, "R3": 3}[lot]
    names = {"R1": "poor", "R2": "medium", "R3": "rich"}
    entries = []
    for kind in KINDS:
        asset_id = f"resource_{kind}_{names[lot]}"
        info = save(resource(kind, tier), OUT / lot / f"{asset_id}.png")
        info["id"] = asset_id
        info["lot"] = lot
        entries.append(info)
    manifest = {
        "lot": lot,
        "status": "premium_local_export",
        "tier": names[lot],
        "asset_count": len(entries),
        "contact_sheet": contact(lot, entries),
        "assets": entries,
        "constraints": {
            "transparent_png": True,
            "runtime_spawn_only": True,
            "no_text": True,
            "no_ui": True,
            "no_route": True,
            "no_painted_ring": True,
            "no_terrain_changes": True,
        },
    }
    (OUT / lot / f"manifest_{lot}.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    return manifest


def main():
    import argparse

    parser = argparse.ArgumentParser()
    parser.add_argument("lot", choices=["R1", "R2", "R3"])
    args = parser.parse_args()
    manifest = export_lot(args.lot)
    print(json.dumps({"lot": args.lot, "asset_count": manifest["asset_count"], "contact_sheet": manifest["contact_sheet"]}, indent=2))


if __name__ == "__main__":
    main()
