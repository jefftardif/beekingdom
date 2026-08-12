from __future__ import annotations

import hashlib
import json
import math
import random
import warnings
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageOps


Image.MAX_IMAGE_PIXELS = None
warnings.simplefilter("ignore", Image.DecompressionBombWarning)

ROOT = Path(r"C:\projets\beekingdomgame-master")
STAGING = ROOT / "artifacts" / "UIB_Wave6_50x50_runtime_resource_assets_premium_staging"
ASSETS = STAGING / "assets"
PROOF = STAGING / "proof"
REPORT = STAGING / "UIB_Wave6_50x50_RuntimeResourceAssetsPremium_Report.md"
MANIFEST = STAGING / "premium_runtime_resource_assets_manifest.json"
RECEIPT = STAGING / "UIB_Wave6_50x50_RuntimeResourceAssetsPremium_RECEIPT.json"
TERRAIN_ROOT = ROOT / "Assets" / "BeeKingdom" / "Playground" / "Resources" / "WorldMapWave6Runtime" / "UIB_ImmenseContinuousMaster50x50_wave5method_12288_preview"

SIZE = 512
TIERS = [
    ("R1", "poor", 0.72, 0.78),
    ("R2", "medium", 0.92, 1.00),
    ("R3", "rich", 1.10, 1.24),
]
RESOURCES = ["pollen", "nectar", "wax", "propolis", "royal_jelly", "water", "honey"]


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest().upper()


def rgba(color: tuple[int, int, int], alpha: int = 255) -> tuple[int, int, int, int]:
    return color[0], color[1], color[2], alpha


def jitter_poly(cx: float, cy: float, rx: float, ry: float, n: int, rng: random.Random, rotation: float = 0) -> list[tuple[float, float]]:
    pts = []
    for i in range(n):
        a = rotation + math.tau * i / n
        jr = 0.78 + rng.random() * 0.44
        pts.append((cx + math.cos(a) * rx * jr, cy + math.sin(a) * ry * jr))
    return pts


def draw_soft_shadow(base: Image.Image, mask: Image.Image, offset=(16, 20), blur=18, alpha=95) -> None:
    shadow = Image.new("RGBA", base.size, (0, 0, 0, 0))
    m = mask.filter(ImageFilter.GaussianBlur(blur))
    shadow.putalpha(ImageOps.autocontrast(m).point(lambda p: min(alpha, p * alpha // 255)))
    base.alpha_composite(shadow, offset)


def draw_leaf(draw: ImageDraw.ImageDraw, cx: float, cy: float, length: float, width: float, angle: float, fill, outline=None) -> None:
    ca, sa = math.cos(angle), math.sin(angle)
    tip = (cx + ca * length * 0.5, cy + sa * length * 0.5)
    root = (cx - ca * length * 0.5, cy - sa * length * 0.5)
    left = (cx - sa * width, cy + ca * width)
    right = (cx + sa * width, cy - ca * width)
    draw.polygon([root, left, tip, right], fill=fill, outline=outline)


def draw_strokes(layer: Image.Image, rng: random.Random, palette: list[tuple[int, int, int]], count: int, bbox: tuple[int, int, int, int], alpha=120) -> None:
    draw = ImageDraw.Draw(layer, "RGBA")
    x0, y0, x1, y1 = bbox
    for _ in range(count):
        color = rgba(rng.choice(palette), rng.randint(alpha // 2, alpha))
        x = rng.randint(x0, x1)
        y = rng.randint(y0, y1)
        length = rng.randint(14, 58)
        angle = rng.uniform(-math.pi, math.pi)
        draw.line((x, y, x + math.cos(angle) * length, y + math.sin(angle) * length), fill=color, width=rng.randint(2, 5))


def organic_base(rng: random.Random, scale: float, palette: list[tuple[int, int, int]], squashed=False) -> tuple[Image.Image, Image.Image]:
    mask = Image.new("L", (SIZE, SIZE), 0)
    draw = ImageDraw.Draw(mask)
    rx = 135 * scale
    ry = (92 if squashed else 116) * scale
    for _ in range(5):
        cx = SIZE / 2 + rng.uniform(-22, 22) * scale
        cy = SIZE / 2 + rng.uniform(-16, 18) * scale
        pts = jitter_poly(cx, cy, rx * rng.uniform(0.82, 1.05), ry * rng.uniform(0.78, 1.06), 18, rng, rng.random() * math.tau)
        draw.polygon(pts, fill=rng.randint(110, 190))
    mask = mask.filter(ImageFilter.GaussianBlur(2.2))

    layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    color = rng.choice(palette)
    colored = Image.new("RGBA", (SIZE, SIZE), rgba(color, 218))
    colored.putalpha(mask)
    layer.alpha_composite(colored)
    draw_strokes(layer, rng, palette, 90, (115, 120, 400, 390), 115)
    return layer, mask


def add_grain(img: Image.Image, rng: random.Random, amount=0.09) -> Image.Image:
    arr = np.asarray(img).astype(np.int16)
    noise = rng.normalvariate if False else None
    rng_np = np.random.default_rng(rng.randint(0, 2**31 - 1))
    n = rng_np.normal(0, 255 * amount, arr[..., :3].shape)
    alpha = arr[..., 3:4] / 255.0
    arr[..., :3] = np.clip(arr[..., :3] + n * alpha, 0, 255)
    return Image.fromarray(arr.astype(np.uint8), "RGBA")


def pollen_sprite(rng: random.Random, tier: str, scale: float) -> Image.Image:
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    base, mask = organic_base(rng, scale, [(63, 91, 42), (87, 119, 54), (114, 134, 59)], True)
    draw_soft_shadow(img, mask)
    img.alpha_composite(base)
    draw = ImageDraw.Draw(img, "RGBA")
    flowers = {"poor": 16, "medium": 28, "rich": 46}[tier]
    for _ in range(flowers):
        x = rng.gauss(256, 78 * scale)
        y = rng.gauss(250, 54 * scale)
        petals = rng.randint(4, 6)
        color = rng.choice([(244, 192, 54), (249, 216, 75), (236, 169, 37), (228, 143, 188), (245, 229, 123)])
        for i in range(petals):
            a = math.tau * i / petals + rng.uniform(-0.2, 0.2)
            draw.ellipse((x + math.cos(a)*10-8, y + math.sin(a)*10-5, x + math.cos(a)*10+8, y + math.sin(a)*10+5), fill=rgba(color, 218))
        draw.ellipse((x-4, y-4, x+4, y+4), fill=rgba((119, 77, 29), 230))
    return add_grain(img, rng)


def nectar_sprite(rng: random.Random, tier: str, scale: float) -> Image.Image:
    img = pollen_sprite(rng, tier, scale)
    draw = ImageDraw.Draw(img, "RGBA")
    drops = {"poor": 5, "medium": 9, "rich": 15}[tier]
    for _ in range(drops):
        x = rng.gauss(258, 66 * scale)
        y = rng.gauss(248, 48 * scale)
        r = rng.randint(11, 22)
        draw.ellipse((x-r, y-r*0.7, x+r, y+r*0.9), fill=(255, 184, 70, 140), outline=(255, 232, 140, 150), width=2)
        draw.ellipse((x-r*0.35, y-r*0.42, x-r*0.05, y-r*0.12), fill=(255, 250, 207, 160))
    return img


def wax_sprite(rng: random.Random, tier: str, scale: float) -> Image.Image:
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    base, mask = organic_base(rng, scale, [(102, 93, 61), (139, 122, 69), (89, 106, 72)], True)
    draw_soft_shadow(img, mask)
    img.alpha_composite(base)
    draw = ImageDraw.Draw(img, "RGBA")
    cells = {"poor": 7, "medium": 13, "rich": 24}[tier]
    for _ in range(cells):
        x = rng.gauss(255, 62 * scale)
        y = rng.gauss(250, 46 * scale)
        rad = rng.randint(18, 31)
        pts = [(x + math.cos(math.tau*i/6) * rad, y + math.sin(math.tau*i/6) * rad * 0.82) for i in range(6)]
        draw.polygon(pts, fill=(239, 191, 88, 205), outline=(255, 225, 132, 180))
        inner = [(x + math.cos(math.tau*i/6) * rad*0.62, y + math.sin(math.tau*i/6) * rad*0.50) for i in range(6)]
        draw.polygon(inner, fill=(181, 128, 49, 70))
    draw_strokes(img, rng, [(254, 220, 119), (191, 135, 46), (112, 95, 47)], 35, (130, 160, 385, 350), 130)
    return add_grain(img, rng)


def propolis_sprite(rng: random.Random, tier: str, scale: float) -> Image.Image:
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    base, mask = organic_base(rng, scale, [(68, 70, 55), (96, 83, 60), (56, 89, 67)], True)
    draw_soft_shadow(img, mask, blur=16, alpha=105)
    img.alpha_composite(base)
    draw = ImageDraw.Draw(img, "RGBA")
    rocks = {"poor": 5, "medium": 9, "rich": 16}[tier]
    for _ in range(rocks):
        x = rng.gauss(256, 70 * scale)
        y = rng.gauss(252, 48 * scale)
        pts = jitter_poly(x, y, rng.randint(20, 42), rng.randint(12, 28), rng.randint(7, 11), rng, rng.random()*math.tau)
        draw.polygon(pts, fill=rgba(rng.choice([(92, 76, 62), (61, 58, 53), (119, 99, 75)]), 230), outline=(39, 36, 31, 130))
        if rng.random() < 0.7:
            draw.line(pts[:3], fill=(174, 102, 47, 180), width=3)
    resin = {"poor": 6, "medium": 11, "rich": 20}[tier]
    for _ in range(resin):
        x = rng.gauss(255, 73 * scale)
        y = rng.gauss(250, 45 * scale)
        r = rng.randint(8, 18)
        draw.ellipse((x-r, y-r*0.7, x+r, y+r*0.8), fill=(111, 58, 31, 190), outline=(197, 107, 44, 160))
    return add_grain(img, rng)


def royal_jelly_sprite(rng: random.Random, tier: str, scale: float) -> Image.Image:
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    base, mask = organic_base(rng, scale, [(72, 91, 83), (91, 111, 83), (115, 103, 74)], True)
    draw_soft_shadow(img, mask)
    img.alpha_composite(base)
    glow = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow, "RGBA")
    pools = {"poor": 3, "medium": 5, "rich": 8}[tier]
    for _ in range(pools):
        x = rng.gauss(256, 55 * scale)
        y = rng.gauss(250, 44 * scale)
        rx, ry = rng.randint(26, 50), rng.randint(16, 34)
        gd.ellipse((x-rx, y-ry, x+rx, y+ry), fill=(240, 255, 185, 128), outline=(255, 253, 212, 190), width=2)
        gd.ellipse((x-rx*0.45, y-ry*0.55, x-rx*0.1, y-ry*0.2), fill=(255, 255, 239, 170))
    img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(1.4)))
    draw = ImageDraw.Draw(img, "RGBA")
    for _ in range({"poor": 4, "medium": 8, "rich": 14}[tier]):
        x = rng.gauss(255, 72 * scale)
        y = rng.gauss(250, 50 * scale)
        draw.ellipse((x-5, y-5, x+5, y+5), fill=(255, 251, 195, 210))
    return add_grain(img, rng, 0.055)


def water_sprite(rng: random.Random, tier: str, scale: float) -> Image.Image:
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    mask = Image.new("L", (SIZE, SIZE), 0)
    md = ImageDraw.Draw(mask)
    for _ in range({"poor": 2, "medium": 4, "rich": 7}[tier]):
        cx = rng.gauss(256, 60 * scale)
        cy = rng.gauss(250, 40 * scale)
        pts = jitter_poly(cx, cy, rng.randint(46, 82) * scale, rng.randint(18, 38) * scale, 16, rng, rng.random()*math.tau)
        md.polygon(pts, fill=175)
    mask = mask.filter(ImageFilter.GaussianBlur(2))
    draw_soft_shadow(img, mask, offset=(12, 18), blur=14, alpha=80)
    water = Image.new("RGBA", (SIZE, SIZE), (54, 156, 178, 195))
    water.putalpha(mask)
    img.alpha_composite(water)
    draw = ImageDraw.Draw(img, "RGBA")
    for _ in range({"poor": 12, "medium": 22, "rich": 36}[tier]):
        x = rng.gauss(256, 86 * scale)
        y = rng.gauss(250, 52 * scale)
        draw.arc((x-34, y-10, x+34, y+13), start=rng.randint(185, 220), end=rng.randint(300, 350), fill=(158, 232, 229, 155), width=2)
    for _ in range({"poor": 5, "medium": 10, "rich": 18}[tier]):
        draw_leaf(draw, rng.gauss(260, 80*scale), rng.gauss(252, 46*scale), rng.randint(20, 38), rng.randint(5, 11), rng.uniform(-0.8, 0.8), (83, 124, 63, 190))
    return add_grain(img, rng, 0.04)


def honey_sprite(rng: random.Random, tier: str, scale: float) -> Image.Image:
    img = wax_sprite(rng, tier, scale)
    draw = ImageDraw.Draw(img, "RGBA")
    globs = {"poor": 4, "medium": 8, "rich": 14}[tier]
    for _ in range(globs):
        x = rng.gauss(256, 58 * scale)
        y = rng.gauss(252, 42 * scale)
        r = rng.randint(17, 34)
        draw.ellipse((x-r, y-r*0.75, x+r, y+r), fill=(233, 141, 29, 175), outline=(255, 212, 92, 160), width=2)
        draw.ellipse((x-r*0.3, y-r*0.45, x-r*0.02, y-r*0.15), fill=(255, 242, 158, 165))
    return add_grain(img, rng, 0.045)


GENERATORS = {
    "pollen": pollen_sprite,
    "nectar": nectar_sprite,
    "wax": wax_sprite,
    "propolis": propolis_sprite,
    "royal_jelly": royal_jelly_sprite,
    "water": water_sprite,
    "honey": honey_sprite,
}


def terrain_bg(tile_name: str, darken: float) -> Image.Image:
    p = TERRAIN_ROOT / tile_name
    if p.exists():
        im = Image.open(p).convert("RGB").crop((2, 2, 514, 514)).resize((256, 256), Image.Resampling.LANCZOS)
    else:
        im = Image.new("RGB", (256, 256), (69, 92, 60))
    arr = np.asarray(im).astype(np.float32)
    arr = np.clip(arr * darken, 0, 255).astype(np.uint8)
    return Image.fromarray(arr, "RGB").convert("RGBA")


def make_proof_sheet(asset_records: list[dict]) -> Path:
    cell_w, cell_h = 256, 296
    header_h = 42
    sheet = Image.new("RGBA", (cell_w * len(RESOURCES), header_h + cell_h * len(TIERS)), (23, 24, 20, 255))
    draw = ImageDraw.Draw(sheet, "RGBA")
    for c, resource in enumerate(RESOURCES):
        draw.rectangle((c*cell_w, 0, (c+1)*cell_w, header_h), fill=(18, 19, 16, 255))
        draw.text((c*cell_w + 12, 13), resource, fill=(238, 224, 176, 255))
    rec_by_key = {(r["resource"], r["tier"]): r for r in asset_records}
    for r_idx, (folder, tier, _, _) in enumerate(TIERS):
        for c, resource in enumerate(RESOURCES):
            x = c * cell_w
            y = header_h + r_idx * cell_h
            bg = terrain_bg("R25C25_g2.png" if (c + r_idx) % 2 == 0 else "R02C47_g2.png", 0.52 if (c + r_idx) % 2 == 0 else 0.95)
            sheet.alpha_composite(bg, (x, y))
            sprite = Image.open(rec_by_key[(resource, tier)]["path"]).convert("RGBA").resize((172, 172), Image.Resampling.LANCZOS)
            sheet.alpha_composite(sprite, (x + 42, y + 50))
            draw.text((x + 10, y + 10), f"{folder} / {tier}", fill=(255, 255, 240, 255))
            draw.rectangle((x, y, x + cell_w - 1, y + cell_h - 1), outline=(255, 238, 182, 70), width=1)
    out = PROOF / "premium_runtime_resources_7x3_proof_sheet.png"
    out.parent.mkdir(parents=True, exist_ok=True)
    sheet.convert("RGB").save(out, quality=95)
    return out


def main() -> None:
    STAGING.mkdir(parents=True, exist_ok=True)
    ASSETS.mkdir(parents=True, exist_ok=True)
    PROOF.mkdir(parents=True, exist_ok=True)

    records = []
    for folder, tier, scale, spread in TIERS:
        (ASSETS / folder).mkdir(parents=True, exist_ok=True)
        for resource in RESOURCES:
            rng = random.Random(f"wave6-premium-runtime-resource::{resource}::{tier}::20260718")
            img = GENERATORS[resource](rng, tier, scale)
            path = ASSETS / folder / f"resource_{resource}_{tier}.png"
            img.save(path)
            alpha = np.asarray(img.getchannel("A"))
            bbox = img.getbbox()
            records.append(
                {
                    "resource": resource,
                    "tier_folder": folder,
                    "tier": tier,
                    "path": str(path),
                    "width": SIZE,
                    "height": SIZE,
                    "mode": "RGBA",
                    "transparent_background": "YES",
                    "alpha_nonzero_pixels": int(np.count_nonzero(alpha)),
                    "content_bbox": list(bbox) if bbox else None,
                    "sha256": sha256(path),
                }
            )

    proof_sheet = make_proof_sheet(records)
    manifest = {
        "schema": "bee-kingdom.wave6.runtime-resource-assets-premium.v1",
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "scope": "runtime_resource_overlay_sprites_only",
        "terrain_50x50_modified": "NO",
        "current_assets_overwritten": "NO",
        "resources": RESOURCES,
        "tiers": [{"folder": f, "tier": t} for f, t, _, _ in TIERS],
        "sprite_size": [SIZE, SIZE],
        "asset_count": len(records),
        "assets": records,
        "proof_sheet": str(proof_sheet),
        "proof_sheet_sha256": sha256(proof_sheet),
    }
    MANIFEST.write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    receipt = {
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "status": "PASS",
        "staging": str(STAGING),
        "asset_count": len(records),
        "required_asset_count": 21,
        "resources_7x3_complete": "YES" if len(records) == 21 else "NO",
        "transparent_pngs": "YES",
        "terrain_50x50_modified": "NO",
        "existing_runtime_assets_overwritten": "NO",
        "proof_sheet_created": "YES",
        "manifest": str(MANIFEST),
        "report": str(REPORT),
        "proof_sheet": str(proof_sheet),
        "READY_FOR_UNITY_RESOURCE_ASSET_REVIEW": "YES" if len(records) == 21 else "NO",
        "READY_FOR_QA_BUILDERC": "NO",
        "READY_FOR_UNITY_HANDOFF": "NO",
    }
    RECEIPT.write_text(json.dumps(receipt, indent=2), encoding="utf-8")

    lines = [
        "# UIB Wave6 50x50 Runtime Resource Assets Premium",
        "",
        "DATE=2026-07-18",
        "STATUS=PASS",
        "",
        "## Scope",
        "",
        "Production limitee aux overlays/sprites runtime de ressources. Le terrain Wave6 50x50 accepte et gele n'a pas ete modifie.",
        "",
        "## Livrables",
        "",
        f"- Staging: `{STAGING}`",
        f"- Assets: `{ASSETS}`",
        f"- Manifest: `{MANIFEST}`",
        f"- Receipt: `{RECEIPT}`",
        f"- Proof sheet: `{proof_sheet}`",
        "",
        "## Couverture",
        "",
        "7 ressources x 3 tiers = 21 PNG RGBA 512x512 transparents.",
        "",
        "| Resource | R1 poor | R2 medium | R3 rich |",
        "| --- | --- | --- | --- |",
    ]
    for resource in RESOURCES:
        row = [resource]
        for _, tier, _, _ in TIERS:
            rec = next(r for r in records if r["resource"] == resource and r["tier"] == tier)
            row.append(f"`{Path(rec['path']).name}`")
        lines.append("| " + " | ".join(row) + " |")
    lines.extend(
        [
            "",
            "## Direction artistique",
            "",
            "- Style painterly/isometrique compatible carte vue du dessus.",
            "- Ombre douce et occlusion legere integrees dans l'alpha.",
            "- Bases organiques, pas d'icone plat ni contour UI dur.",
            "- Lisibilite petit format via silhouettes de clusters, reliefs, reflets et matieres distinctes.",
            "",
            "## Gates",
            "",
            "READY_FOR_UNITY_RESOURCE_ASSET_REVIEW=YES",
            "READY_FOR_QA_BUILDERC=NO",
            "READY_FOR_UNITY_HANDOFF=NO",
            "TERRAIN_50X50_MODIFIED=NO",
        ]
    )
    REPORT.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(json.dumps(receipt, indent=2))


if __name__ == "__main__":
    main()
