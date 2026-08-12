from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(r"C:\projets\beekingdomgame-master")
OUT = ROOT / "artifacts" / "WorldMapRuntimeEntitiesWave1" / "exports_limited_v1"
SIZE = 256


def font(size: int) -> ImageFont.ImageFont:
    try:
        return ImageFont.truetype("arial.ttf", size)
    except OSError:
        return ImageFont.load_default()


def iso_poly(cx: int, cy: int, w: int, h: int) -> list[tuple[int, int]]:
    return [(cx, cy - h // 2), (cx + w // 2, cy), (cx, cy + h // 2), (cx - w // 2, cy)]


def line(draw: ImageDraw.ImageDraw, points, fill, width=4):
    draw.line(points, fill=fill, width=width, joint="curve")


def label(draw: ImageDraw.ImageDraw, text: str):
    draw.rounded_rectangle((20, 218, 236, 246), radius=8, fill=(12, 16, 12, 190), outline=(238, 198, 82, 210), width=2)
    draw.text((128, 232), text, fill=(255, 248, 220, 255), anchor="mm", font=font(14))


def new_image() -> tuple[Image.Image, ImageDraw.ImageDraw]:
    image = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)


def hive_asset(name: str, cls: str, scale: float, accent: tuple[int, int, int, int]) -> Image.Image:
    image, draw = new_image()
    cx, cy = 128, 126
    w, h = int(122 * scale), int(116 * scale)
    top = iso_poly(cx, cy - 18, w, h // 2)
    left = [(cx - w // 2, cy - 18), (cx, cy + h // 4), (cx, cy + h // 2), (cx - w // 2, cy + h // 6)]
    right = [(cx + w // 2, cy - 18), (cx, cy + h // 4), (cx, cy + h // 2), (cx + w // 2, cy + h // 6)]
    draw.polygon(left, fill=(150, 96, 32, 245), outline=(66, 42, 18, 230))
    draw.polygon(right, fill=(108, 70, 28, 245), outline=(66, 42, 18, 230))
    draw.polygon(top, fill=(218, 155, 50, 250), outline=(246, 196, 74, 245))
    for ox, oy, rr in [(-28, 6, 9), (2, 18, 11), (31, 1, 8), (-4, -12, 8)]:
        draw.ellipse((cx + ox - rr, cy + oy - rr, cx + ox + rr, cy + oy + rr), fill=(38, 23, 10, 235))
    if cls == "royal_guard":
        line(draw, [(72, 94), (98, 54), (128, 82), (158, 54), (184, 94)], accent, 8)
    elif cls == "striker":
        for x1, y1, x2, y2 in [(66, 100, 34, 72), (190, 100, 222, 72), (96, 70, 82, 28), (160, 70, 174, 28)]:
            line(draw, [(x1, y1), (x2, y2)], accent, 8)
    elif cls == "nurturer":
        draw.ellipse((66, 70, 96, 100), fill=accent)
        draw.ellipse((160, 70, 190, 100), fill=accent)
        line(draw, [(70, 166), (104, 190), (150, 190), (186, 166)], accent, 7)
    elif cls == "scout":
        line(draw, [(128, 48), (128, 2), (100, 58), (70, 16), (156, 58), (188, 16)], accent, 6)
    elif cls == "alchemist":
        draw.ellipse((62, 62, 90, 90), fill=(102, 246, 188, 240))
        draw.ellipse((166, 62, 194, 90), fill=accent)
        line(draw, [(76, 52), (108, 18), (148, 18), (180, 52)], accent, 7)
    else:
        line(draw, [(74, 82), (98, 50), (128, 66), (158, 50), (182, 82)], accent, 6)
    label(draw, name)
    return image


def resource_asset(name: str, kind: str, color: tuple[int, int, int, int]) -> Image.Image:
    image, draw = new_image()
    draw.polygon(iso_poly(128, 154, 150, 70), fill=(43, 62, 34, 110), outline=(86, 112, 70, 130))
    if kind == "nectar":
        for x, y, r in [(102, 132, 24), (132, 118, 28), (158, 137, 22)]:
            draw.ellipse((x - r, y - r, x + r, y + r), fill=color, outline=(245, 215, 106, 230), width=4)
    elif kind == "pollen":
        for x, y, r in [(100, 142, 24), (132, 124, 30), (164, 144, 23)]:
            draw.ellipse((x - r, y - r, x + r, y + r), fill=color)
            draw.arc((x - r, y - r, x + r, y + r), 20, 210, fill=(255, 239, 128, 240), width=4)
    elif kind == "water":
        draw.ellipse((70, 116, 186, 176), fill=(70, 190, 255, 210), outline=(190, 238, 255, 245), width=5)
        draw.ellipse((98, 96, 154, 148), fill=(114, 220, 255, 230))
    elif kind == "wax":
        for poly in [iso_poly(98, 134, 66, 48), iso_poly(150, 132, 76, 54), iso_poly(128, 166, 82, 46)]:
            draw.polygon(poly, fill=color, outline=(90, 50, 18, 230))
    elif kind == "honey":
        draw.ellipse((66, 110, 190, 176), fill=(194, 108, 22, 230), outline=(255, 196, 88, 245), width=5)
        draw.ellipse((88, 92, 168, 128), fill=(255, 190, 76, 235))
    elif kind == "royal_jelly":
        draw.ellipse((74, 84, 182, 184), fill=(255, 244, 196, 240), outline=(220, 198, 255, 250), width=6)
        draw.ellipse((104, 112, 152, 160), fill=color)
    else:
        draw.polygon([(78, 118), (118, 74), (178, 108), (166, 178), (88, 170)], fill=(64, 104, 56, 235), outline=(36, 48, 24, 230))
        draw.ellipse((104, 110, 158, 162), fill=color)
    label(draw, name)
    return image


def beast_asset(name: str, tier: str, color: tuple[int, int, int, int]) -> Image.Image:
    image, draw = new_image()
    cx, cy = 128, 126
    if tier == "t1":
        draw.ellipse((84, 98, 172, 154), fill=color, outline=(26, 34, 18, 230), width=4)
        line(draw, [(106, 100), (82, 64), (150, 100), (174, 64)], (160, 210, 110, 230), 5)
    elif tier == "t2":
        for box in [(68, 108, 118, 146), (108, 96, 160, 142), (150, 106, 202, 150)]:
            draw.ellipse(box, fill=color, outline=(38, 20, 12, 230), width=4)
        for x in [82, 116, 152, 186]:
            line(draw, [(x, 128), (x - 32, 96)], (208, 122, 58, 230), 5)
            line(draw, [(x, 134), (x - 30, 172)], (208, 122, 58, 230), 5)
    elif tier == "t3":
        draw.ellipse((72, 78, 176, 166), fill=color, outline=(34, 26, 48, 230), width=5)
        draw.ellipse((50, 98, 104, 150), fill=(52, 40, 70, 245))
        for sx in [70, 92, 164, 186]:
            line(draw, [(sx, 116), (sx - 46 if sx < 128 else sx + 46, 62)], (142, 124, 216, 235), 6)
            line(draw, [(sx, 132), (sx - 54 if sx < 128 else sx + 54, 174)], (142, 124, 216, 235), 6)
    elif tier == "t4":
        draw.ellipse((104, 64, 152, 174), fill=color, outline=(34, 62, 26, 230), width=5)
        draw.ellipse((104, 34, 152, 82), fill=(70, 108, 54, 245))
        line(draw, [(106, 82), (42, 28), (78, 92), (150, 82), (214, 28), (178, 92)], (132, 182, 92, 240), 7)
    elif tier == "t5":
        draw.ellipse((62, 96, 194, 154), fill=color, outline=(70, 44, 18, 230), width=5)
        draw.polygon([(98, 92), (62, 42), (122, 76)], fill=(222, 236, 255, 185), outline=(222, 236, 255, 235))
        draw.polygon([(158, 92), (194, 42), (134, 76)], fill=(222, 236, 255, 185), outline=(222, 236, 255, 235))
        line(draw, [(80, 126), (176, 126)], (245, 202, 65, 240), 6)
    elif tier == "t6":
        draw.ellipse((58, 104, 190, 160), fill=color, outline=(44, 26, 18, 230), width=5)
        line(draw, [(78, 116), (28, 72), (48, 118), (178, 116), (228, 72), (208, 118)], (194, 122, 84, 240), 7)
        line(draw, [(164, 102), (190, 42), (138, 24), (122, 64)], (194, 122, 84, 240), 7)
    else:
        draw.ellipse((42, 92, 214, 160), fill=color, outline=(82, 48, 16, 230), width=6)
        draw.ellipse((96, 40, 160, 98), fill=(112, 66, 22, 245), outline=(245, 209, 94, 240), width=5)
        draw.polygon([(88, 86), (24, 20), (102, 62)], fill=(222, 236, 255, 180), outline=(222, 236, 255, 230))
        draw.polygon([(168, 86), (232, 20), (154, 62)], fill=(222, 236, 255, 180), outline=(222, 236, 255, 230))
        line(draw, [(108, 36), (128, 4), (148, 36)], (245, 209, 94, 240), 6)
    label(draw, name)
    return image


ASSETS = [
    ("hives", "hive_neutral_l1.png", lambda: hive_asset("Neutral L1", "neutral", 0.72, (246, 213, 106, 245))),
    ("hives", "hive_neutral_l9.png", lambda: hive_asset("Neutral L9", "neutral", 0.95, (246, 213, 106, 245))),
    ("hives", "hive_royal_guard_l20.png", lambda: hive_asset("Royal Guard", "royal_guard", 1.05, (125, 199, 255, 245))),
    ("hives", "hive_striker_l20.png", lambda: hive_asset("Striker", "striker", 1.05, (255, 100, 74, 245))),
    ("hives", "hive_nurturer_l20.png", lambda: hive_asset("Nurturer", "nurturer", 1.05, (141, 240, 168, 245))),
    ("hives", "hive_scout_l20.png", lambda: hive_asset("Scout", "scout", 1.05, (241, 220, 88, 245))),
    ("hives", "hive_alchemist_l20.png", lambda: hive_asset("Alchemist", "alchemist", 1.05, (163, 108, 255, 245))),
    ("resources", "resource_nectar_rich.png", lambda: resource_asset("Nectar", "nectar", (179, 108, 255, 235))),
    ("resources", "resource_pollen_rich.png", lambda: resource_asset("Pollen", "pollen", (242, 207, 50, 245))),
    ("resources", "resource_water_rich.png", lambda: resource_asset("Eau", "water", (94, 201, 255, 235))),
    ("resources", "resource_wax_rich.png", lambda: resource_asset("Cire", "wax", (240, 170, 61, 245))),
    ("resources", "resource_honey_rich.png", lambda: resource_asset("Miel", "honey", (217, 134, 32, 245))),
    ("resources", "resource_royal_jelly_rich.png", lambda: resource_asset("Gelee royale", "royal_jelly", (233, 212, 255, 245))),
    ("resources", "resource_propolis_rich.png", lambda: resource_asset("Propolis", "propolis", (109, 61, 31, 245))),
    ("bestiary", "beast_t1_aphid.png", lambda: beast_asset("T1 Aphid", "t1", (70, 96, 38, 245))),
    ("bestiary", "beast_t2_ant.png", lambda: beast_asset("T2 Ant", "t2", (84, 42, 24, 245))),
    ("bestiary", "beast_t3_spider.png", lambda: beast_asset("T3 Spider", "t3", (40, 33, 54, 245))),
    ("bestiary", "beast_t4_mantis.png", lambda: beast_asset("T4 Mantis", "t4", (48, 79, 36, 245))),
    ("bestiary", "beast_t5_hornet.png", lambda: beast_asset("T5 Hornet", "t5", (93, 58, 18, 245))),
    ("bestiary", "beast_t6_scorpion.png", lambda: beast_asset("T6 Scorpion", "t6", (58, 35, 24, 245))),
    ("bestiary", "beast_t7_hornet_queen.png", lambda: beast_asset("T7 Queen", "t7", (95, 56, 18, 245))),
]


def alpha_bbox(image: Image.Image) -> tuple[int, int, int, int] | None:
    return image.getchannel("A").getbbox()


def save_contact_sheet(entries: list[dict]) -> str:
    cols, cell = 7, 176
    rows = (len(entries) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * cell, rows * cell), (18, 24, 18, 255))
    draw = ImageDraw.Draw(sheet)
    for i, entry in enumerate(entries):
        image = Image.open(OUT / entry["category"] / entry["file"]).convert("RGBA")
        image.thumbnail((128, 128), Image.Resampling.LANCZOS)
        x = (i % cols) * cell
        y = (i // cols) * cell
        draw.rectangle((x + 6, y + 6, x + cell - 6, y + cell - 6), outline=(91, 118, 78, 255), width=2)
        sheet.alpha_composite(image, (x + (cell - image.width) // 2, y + 12))
        draw.text((x + cell // 2, y + 150), entry["name"], fill=(245, 232, 190, 255), anchor="mm", font=font(11))
    path = OUT / "contact_sheet_wave1_limited_v1.png"
    sheet.convert("RGB").save(path)
    return path.name


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    entries: list[dict] = []
    for category, filename, factory in ASSETS:
        folder = OUT / category
        folder.mkdir(parents=True, exist_ok=True)
        image = factory()
        bbox = alpha_bbox(image)
        if bbox is None:
            raise RuntimeError(f"empty alpha for {filename}")
        path = folder / filename
        image.save(path)
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        entries.append(
            {
                "category": category,
                "file": filename,
                "name": filename.removesuffix(".png"),
                "size": [image.width, image.height],
                "alpha_bbox": list(bbox),
                "sha256": digest,
                "transparent_background": True,
                "source": "procedural concept export v1",
            }
        )
    contact = save_contact_sheet(entries)
    manifest = {
        "wave": "WorldMapRuntimeEntitiesWave1",
        "lot": "limited_v1",
        "status": "local_concept_png_export",
        "asset_count": len(entries),
        "contact_sheet": contact,
        "constraints": {
            "transparent_png": True,
            "runtime_spawn_only": True,
            "no_terrain_tile_changes": True,
            "no_bear": True,
            "no_server": True,
            "no_apk_rebuild": True,
        },
        "assets": entries,
    }
    (OUT / "manifest_wave1_limited_v1.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print(json.dumps({"asset_count": len(entries), "out": str(OUT), "contact_sheet": contact}, indent=2))


if __name__ == "__main__":
    main()
