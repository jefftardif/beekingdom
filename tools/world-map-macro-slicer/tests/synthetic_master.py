from __future__ import annotations

from pathlib import Path

import numpy as np
from PIL import Image


MASTER_SIZE = 2560


def synthetic_feature_masks() -> tuple[np.ndarray, np.ndarray]:
    x = np.arange(MASTER_SIZE, dtype=np.int32)[None, :]
    y = np.arange(MASTER_SIZE, dtype=np.int32)[:, None]
    diagonal_phase = (x + 2 * y) % 320
    river_mask = np.abs(diagonal_phase - 160) <= 18
    relief_mask = ((x // 7 + y // 11) & 1).astype(bool)
    return river_mask, relief_mask


def create_synthetic_master(path: Path, mode: str = "RGB") -> None:
    x = np.arange(MASTER_SIZE, dtype=np.int32)[None, :]
    y = np.arange(MASTER_SIZE, dtype=np.int32)[:, None]
    river_mask, relief_mask = synthetic_feature_masks()

    red = ((x * 3 + y * 2 + 31) % 256).astype(np.uint8)
    green = ((x // 3 + y * 5 + 67) % 256).astype(np.uint8)
    blue = (115 + 62 * np.sin(x / 37.0) + 51 * np.cos(y / 29.0)).clip(0, 255).astype(np.uint8)
    pixels = np.stack((red, green, blue), axis=2)

    relief_delta = np.where(relief_mask, 24, -18).astype(np.int16)
    pixels = np.clip(pixels.astype(np.int16) + relief_delta[:, :, None], 0, 255).astype(np.uint8)
    river_blue = np.empty_like(pixels)
    river_blue[:, :, 0] = 18
    river_blue[:, :, 1] = 122 + ((x + y) % 55).astype(np.uint8)
    river_blue[:, :, 2] = 205 + ((x // 9) % 45).astype(np.uint8)
    pixels[river_mask] = river_blue[river_mask]

    if mode == "RGBA":
        alpha = (190 + ((x * 5 + y * 7) % 66)).astype(np.uint8)
        pixels = np.concatenate((pixels, alpha[:, :, None]), axis=2)
    elif mode != "RGB":
        raise ValueError(f"Mode synthétique non supporté: {mode}")

    image = Image.fromarray(np.ascontiguousarray(pixels))
    image.save(path, format="PNG", compress_level=9, optimize=False)
    image.close()
