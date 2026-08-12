from __future__ import annotations

import hashlib
import json
import math
from datetime import datetime
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(r"C:\projets\beekingdomgame-master")
OUT = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging" / "production_v3ad_local_native_4096_pictorial_prototype"


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest().upper()


def smooth_noise(size: int, seed: int, scale: int) -> np.ndarray:
    rng = np.random.default_rng(seed)
    small = rng.random((max(2, size // scale), max(2, size // scale)), dtype=np.float32)
    img = Image.fromarray(np.uint8(small * 255), "L").resize((size, size), Image.Resampling.BICUBIC)
    return np.asarray(img, dtype=np.float32) / 255.0


def fbm(size: int, seed: int) -> np.ndarray:
    layers = [
        (256, 0.45),
        (128, 0.35),
        (64, 0.28),
        (32, 0.20),
        (16, 0.12),
        (8, 0.07),
    ]
    acc = np.zeros((size, size), dtype=np.float32)
    total = 0.0
    for idx, (scale, weight) in enumerate(layers):
        acc += smooth_noise(size, seed + idx * 101, scale) * weight
        total += weight
    acc /= total
    return np.clip(acc, 0, 1)


def normalize(a: np.ndarray) -> np.ndarray:
    mn = float(a.min())
    mx = float(a.max())
    if mx - mn < 1e-6:
        return np.zeros_like(a)
    return (a - mn) / (mx - mn)


def generate_canvas(size: int = 4096) -> Image.Image:
    yy, xx = np.mgrid[0:size, 0:size].astype(np.float32)
    x = xx / size
    y = yy / size

    elev = fbm(size, 1201)
    ridges = 1.0 - np.abs(fbm(size, 2202) * 2.0 - 1.0)
    detail = fbm(size, 3303)
    micro = smooth_noise(size, 4404, 4)

    river_center = 0.23 + 0.48 * y + 0.055 * np.sin(y * math.tau * 2.2) + 0.035 * np.sin(y * math.tau * 5.4)
    river_dist = np.abs(x - river_center)
    river = np.exp(-((river_dist / 0.020) ** 2))
    river_banks = np.exp(-((river_dist / 0.045) ** 2))

    lake1 = np.exp(-(((x - 0.58) / 0.135) ** 2 + ((y - 0.33) / 0.075) ** 2))
    lake2 = np.exp(-(((x - 0.72) / 0.170) ** 2 + ((y - 0.69) / 0.115) ** 2))
    water = np.clip(river + lake1 * 0.85 + lake2 * 0.55, 0, 1)

    mountains = np.clip((elev * 0.62 + ridges * 0.62 + (x * 0.16) - 0.58) * 2.4, 0, 1)
    forest = np.clip((detail * 0.92 + elev * 0.35 - water * 1.25 - mountains * 0.18 - 0.30) * 1.8, 0, 1)
    meadow = np.clip((1.0 - mountains) * (1.0 - water) * (0.52 + 0.48 * smooth_noise(size, 5505, 48)), 0, 1)
    flowers = np.clip(smooth_noise(size, 6606, 12) - 0.62, 0, 1) * meadow * (1 - water)

    base = np.zeros((size, size, 3), dtype=np.float32)
    deep_green = np.array([24, 70, 39], dtype=np.float32)
    pine = np.array([17, 88, 54], dtype=np.float32)
    grass = np.array([116, 146, 42], dtype=np.float32)
    sun_grass = np.array([173, 163, 58], dtype=np.float32)
    rock = np.array([139, 135, 112], dtype=np.float32)
    snow = np.array([211, 209, 196], dtype=np.float32)
    teal = np.array([34, 154, 161], dtype=np.float32)
    deep_water = np.array([17, 88, 118], dtype=np.float32)
    flower = np.array([176, 132, 172], dtype=np.float32)

    base += meadow[..., None] * grass
    base = base * (1 - forest[..., None] * 0.72) + pine * (forest[..., None] * 0.72)
    base = base * (1 - mountains[..., None] * 0.85) + rock * (mountains[..., None] * 0.85)
    base = base * (1 - (mountains * ridges)[..., None] * 0.22) + snow * ((mountains * ridges)[..., None] * 0.22)
    base = base * (1 - water[..., None]) + (deep_water * (1 - river_banks[..., None] * 0.35) + teal * river_banks[..., None] * 0.35) * water[..., None]
    base = base * (1 - flowers[..., None] * 0.36) + flower * (flowers[..., None] * 0.36)
    base = base * (0.74 + 0.36 * detail[..., None]) + sun_grass * ((meadow * (0.10 + 0.08 * micro))[..., None])

    # Painted relief shading from continuous height, not per-tile.
    height = normalize(elev * 0.6 + ridges * 0.7 + mountains * 0.5 - water * 0.4)
    gy, gx = np.gradient(height)
    light = normalize(-gx * 0.72 - gy * 0.52 + height * 0.18)
    base *= (0.78 + light[..., None] * 0.44)

    # Add many tiny painterly strokes on the single canvas for premium detail.
    img = Image.fromarray(np.uint8(np.clip(base, 0, 255)), "RGB")
    draw = ImageDraw.Draw(img, "RGBA")
    rng = np.random.default_rng(7707)
    for _ in range(11500):
        px = int(rng.integers(0, size))
        py = int(rng.integers(0, size))
        if water[py, px] > 0.45:
            col = (95, 210, 215, int(rng.integers(18, 48)))
            length = int(rng.integers(8, 34))
            width = int(rng.integers(1, 3))
            angle = -0.55 + float(rng.normal(0, 0.22))
        elif mountains[py, px] > 0.55:
            col = (230, 224, 203, int(rng.integers(18, 58)))
            length = int(rng.integers(8, 46))
            width = int(rng.integers(1, 4))
            angle = -0.85 + float(rng.normal(0, 0.36))
        elif forest[py, px] > 0.45:
            col = (9, 55, 31, int(rng.integers(15, 42)))
            length = int(rng.integers(5, 20))
            width = int(rng.integers(1, 3))
            angle = float(rng.normal(-1.35, 0.50))
        else:
            col = (202, 184, 82, int(rng.integers(12, 32)))
            length = int(rng.integers(5, 26))
            width = int(rng.integers(1, 3))
            angle = float(rng.normal(-0.35, 0.75))
        dx = math.cos(angle) * length
        dy = math.sin(angle) * length
        draw.line((px - dx / 2, py - dy / 2, px + dx / 2, py + dy / 2), fill=col, width=width)

    return img.filter(ImageFilter.UnsharpMask(radius=1.2, percent=105, threshold=3))


def seam_stats(img: Image.Image, grid: int = 4) -> dict:
    arr = np.asarray(img, dtype=np.int16)
    tile = arr.shape[0] // grid
    stats: dict[str, list[float]] = {"vertical": [], "horizontal": []}
    for c in range(1, grid):
        x = c * tile
        delta = np.abs(arr[:, x - 1, :] - arr[:, x, :]).mean()
        stats["vertical"].append(round(float(delta), 3))
    for r in range(1, grid):
        y = r * tile
        delta = np.abs(arr[y - 1, :, :] - arr[y, :, :]).mean()
        stats["horizontal"].append(round(float(delta), 3))
    return stats


def crop_sheet(paths: list[Path], out: Path, labels: list[str]) -> None:
    cell = 320
    pad = 28
    cols = 4
    rows = math.ceil(len(paths) / cols)
    sheet = Image.new("RGB", (cols * cell + (cols + 1) * pad, rows * (cell + 42) + pad), (15, 21, 22))
    draw = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype("arial.ttf", 18)
    except Exception:
        font = ImageFont.load_default()
    for idx, path in enumerate(paths):
        row, col = divmod(idx, cols)
        x = pad + col * (cell + pad)
        y = pad + row * (cell + 42)
        im = Image.open(path).convert("RGB").resize((cell, cell), Image.Resampling.LANCZOS)
        sheet.paste(im, (x, y + 22))
        draw.text((x, y), labels[idx], fill=(235, 222, 165), font=font)
        draw.rectangle((x, y + 22, x + cell - 1, y + 22 + cell - 1), outline=(215, 179, 53), width=2)
    sheet.save(out)


def main() -> None:
    source_dir = OUT / "source"
    tiles_dir = OUT / "grid_4x4_1024"
    crops_dir = OUT / "crops_512"
    seam_dir = OUT / "seam_crops"
    proof_dir = OUT / "proof"
    for d in (source_dir, tiles_dir, crops_dir, seam_dir, proof_dir):
        d.mkdir(parents=True, exist_ok=True)

    img = generate_canvas(4096)
    source = source_dir / "v3ad_local_native_4096_single_canvas.png"
    img.save(source)

    tile_paths = []
    tile = 1024
    for r in range(4):
        for c in range(4):
            p = tiles_dir / f"R{r:02d}C{c:02d}_1024.png"
            img.crop((c * tile, r * tile, (c + 1) * tile, (r + 1) * tile)).save(p)
            tile_paths.append(p)

    crop_specs = [
        ("NW", 0, 0),
        ("N_MID", 1792, 128),
        ("CENTER", 1792, 1792),
        ("RIVER", 640, 2048),
        ("LAKE", 2304, 1120),
        ("MOUNTAIN", 3072, 512),
        ("FOREST", 512, 3072),
        ("MEADOW", 2380, 2860),
        ("SE", 3584, 3584),
    ]
    crop_paths: list[Path] = []
    labels: list[str] = []
    for label, x, y in crop_specs:
        p = crops_dir / f"v3ad_crop_{label}.png"
        img.crop((x, y, x + 512, y + 512)).save(p)
        crop_paths.append(p)
        labels.append(label)

    for idx, x in enumerate((1024, 2048, 3072), 1):
        p = seam_dir / f"v3ad_vertical_seam_{idx}_512.png"
        img.crop((x - 256, 1792, x + 256, 2304)).save(p)
        crop_paths.append(p)
        labels.append(f"V-SEAM-{idx}")
    for idx, y in enumerate((1024, 2048, 3072), 1):
        p = seam_dir / f"v3ad_horizontal_seam_{idx}_512.png"
        img.crop((1792, y - 256, 2304, y + 256)).save(p)
        crop_paths.append(p)
        labels.append(f"H-SEAM-{idx}")

    proof = proof_dir / "v3ad_local_native_4096_pictorial_prototype_proof_sheet.png"
    crop_sheet(crop_paths, proof, labels)
    stats = seam_stats(img)

    vertical_pass = max(stats["vertical"]) < 14.0
    horizontal_pass = max(stats["horizontal"]) < 14.0
    detail_verdict = "MANUAL_REVIEW_REQUIRED"
    premium_verdict = "MANUAL_REVIEW_REQUIRED"

    checkpoint = OUT / "V3AD_LOCAL_NATIVE_4096_PICTORIAL_PROTOTYPE_CHECKPOINT.md"
    checkpoint.write_text(
        "\n".join(
            [
                "# V3AD Local Native 4096 Pictorial Prototype",
                "",
                f"Created: {datetime.now().isoformat(timespec='seconds')}",
                "",
                "This is an honest local native-resolution prototype. It is not a final Unity handoff.",
                "",
                "## Source",
                f"- Native single canvas: `{source}`",
                "- Resolution: `4096x4096`",
                "- Grid cut: `4x4` panels of `1024x1024` from one continuous canvas",
                "",
                "## Current Verdict",
                "- Native >=4096: YES",
                f"- Vertical seam metric pass: {'YES' if vertical_pass else 'NO'}",
                f"- Horizontal seam metric pass: {'YES' if horizontal_pass else 'NO'}",
                "- Premium visual/detail pass: MANUAL_REVIEW_REQUIRED",
                "- Production scale ready: NO",
                "- Unity handoff ready: NO",
                "",
                "## Notes",
                "This route proves local native canvas size is possible, but must be visually reviewed against V3Z and Wave5 premium before any scale-up or Unity package work.",
            ]
        ),
        encoding="utf-8",
    )

    receipt = {
        "artifact": "V3AD_LOCAL_NATIVE_4096_PICTORIAL_PROTOTYPE",
        "created_at": datetime.now().isoformat(timespec="seconds"),
        "folder": str(OUT),
        "source": str(source),
        "source_resolution": [4096, 4096],
        "grid": {"rows": 4, "columns": 4, "tile_size": 1024, "folder": str(tiles_dir)},
        "crops": str(crops_dir),
        "seam_crops": str(seam_dir),
        "proof_sheet": str(proof),
        "seam_stats_mean_abs_rgb_delta": stats,
        "verdict": "Native 4096 single-canvas prototype created; visual premium status requires manual review before any production scale or Unity handoff.",
        "gates": {
            "ACTIVE_WORK_RESUMED": "YES",
            "V3AD_LOCAL_NATIVE_4096_CREATED": "YES",
            "V3AD_SINGLE_CANVAS_CREATED": "YES",
            "V3AD_GRID_CUT_CREATED": "YES",
            "V3AD_VERTICAL_SEAM_METRIC_PASS": "YES" if vertical_pass else "NO",
            "V3AD_HORIZONTAL_SEAM_METRIC_PASS": "YES" if horizontal_pass else "NO",
            "V3AD_DETAIL_PASS": detail_verdict,
            "V3AD_PREMIUM_VISUAL_PASS": premium_verdict,
            "V3AD_PRODUCTION_SCALE_READY": "NO",
            "V3AD_FULL_TILE_PACKAGE_CREATED": "NO",
            "READY_FOR_QA_BUILDERC": "NO",
            "READY_FOR_UNITY_HANDOFF": "NO",
        },
        "hashes": {
            "source_sha256": sha256(source),
            "proof_sha256": sha256(proof),
            "checkpoint_sha256": sha256(checkpoint),
        },
    }
    receipt_path = OUT / "V3AD_LOCAL_NATIVE_4096_PICTORIAL_PROTOTYPE_RECEIPT.json"
    receipt_path.write_text(json.dumps(receipt, indent=2), encoding="utf-8")

    communication = ROOT / "Docs" / "WorldMapCommunication" / "WorldMapCommunication_BeeKingdomWave6_V3ADLocalNative4096PictorialPrototype_2026-07-16.md"
    communication.parent.mkdir(parents=True, exist_ok=True)
    communication.write_text(
        "\n".join(
            [
                "# Bee Kingdom Wave6 - V3AD Local Native 4096 Pictorial Prototype",
                "",
                f"Created: {datetime.now().isoformat(timespec='seconds')}",
                "",
                "A local native 4096x4096 single-canvas prototype was created to test whether we can escape the 1254x1254 image-generation cap without returning to the rejected flat legacy 25600 route.",
                "",
                "## Result",
                "- Native 4096 single canvas: YES",
                "- Deterministic 4x4 grid cut: YES",
                f"- Seam metric pass: vertical={'YES' if vertical_pass else 'NO'}, horizontal={'YES' if horizontal_pass else 'NO'}",
                "- Premium visual pass: MANUAL_REVIEW_REQUIRED",
                "- Unity handoff: NO",
                "",
                f"Receipt: `{receipt_path}`",
                f"Proof sheet: `{proof}`",
            ]
        ),
        encoding="utf-8",
    )

    print(json.dumps({"receipt": str(receipt_path), "proof": str(proof), "source": str(source)}, indent=2))


if __name__ == "__main__":
    main()
