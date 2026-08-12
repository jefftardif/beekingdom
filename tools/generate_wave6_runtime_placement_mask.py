from __future__ import annotations

import hashlib
import json
import math
import re
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
from PIL import Image


ROOT = Path(r"C:\projets\beekingdomgame-master")
TERRAIN_ROOT = ROOT / "Assets" / "BeeKingdom" / "Playground" / "Resources" / "WorldMapWave6Runtime" / "UIB_ImmenseContinuousMaster50x50_wave5method_12288_preview"
OUTPUT_ROOT = ROOT / "Assets" / "BeeKingdom" / "Playground" / "Resources" / "WorldMapRuntimePlacement"
OUTPUT_JSON = OUTPUT_ROOT / "wave6_wave5method_12288_placement_mask.json"
REPORT_JSON = ROOT / "Docs" / "WorldMapAudit" / "Wave6_50x50_Wave5Method12288" / "RuntimePlacementMaskGeneration_20260718.json"
REPORT_RECEIPT_JSON = ROOT / "Docs" / "WorldMapAudit" / "Wave6_50x50_Wave5Method12288" / "RuntimeResourcePlacementColorVeto_20260718.json"

ORIGIN_CHUNK_X = 7
ORIGIN_CHUNK_Y = 7
ROWS = 50
COLUMNS = 50
CORE_SIZE = 512
GUTTER = 2
EXPECTED_MASTER_SHA256 = "3CE816052FFF97BCDE78251FA930C4D725DC622120D3644C806A9C1BE1330697"

TILE_RE = re.compile(r"R(?P<row>\d{2})C(?P<col>\d{2})_g2\.png$", re.IGNORECASE)
RESOURCE_TOKENS = ("pollen", "nectar", "wax", "honey", "propolis", "royal_jelly", "water")
NON_WATER_RESOURCE_TOKENS = tuple(token for token in RESOURCE_TOKENS if token != "water")


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest().upper()


def clamp(v: float, lo: float, hi: float) -> float:
    return max(lo, min(hi, v))


def sample_patch(rgb: np.ndarray, x01: float, y01: float, radius: int = 42) -> np.ndarray:
    h, w, _ = rgb.shape
    x = int(clamp(x01, 0.08, 0.92) * (w - 1))
    y = int(clamp(y01, 0.08, 0.92) * (h - 1))
    x0 = max(0, x - radius)
    x1 = min(w, x + radius + 1)
    y0 = max(0, y - radius)
    y1 = min(h, y + radius + 1)
    return rgb[y0:y1, x0:x1]


def patch_features(patch: np.ndarray) -> dict[str, float]:
    data = patch.astype(np.float32) / 255.0
    r = data[:, :, 0]
    g = data[:, :, 1]
    b = data[:, :, 2]
    lum = r * 0.2126 + g * 0.7152 + b * 0.0722
    water = np.clip((b - r * 0.88) * 2.1 + (b - g * 0.62) * 0.95, 0.0, 1.0)
    vegetation = np.clip((g - b * 0.54) * 1.75 + (g - r * 0.42) * 1.15, 0.0, 1.0)
    snow = np.clip((lum - 0.67) * 2.6 + (b - g * 0.70) * 0.75, 0.0, 1.0)
    gray_rock = np.clip((lum - 0.38) * 1.9 - np.abs(g - r) * 0.8 - vegetation * 0.42, 0.0, 1.0)
    purple_rock = np.clip((b - g * 0.78) * 1.25 + (r - g * 0.82) * 0.75 + (lum - 0.28) * 0.35, 0.0, 1.0)
    ochre_cliff = np.clip((r - b * 0.72) * 1.12 + (g - b * 0.70) * 0.95 + (lum - 0.42) * 0.85 - vegetation * 0.72, 0.0, 1.0)
    gx = np.abs(np.diff(lum, axis=1)).mean() if patch.shape[1] > 1 else 0.0
    gy = np.abs(np.diff(lum, axis=0)).mean() if patch.shape[0] > 1 else 0.0
    edge = float((gx + gy) * 0.5)
    pale_mountain = np.clip((lum - 0.52) * 2.1 + edge * 7.0 - vegetation * 0.52, 0.0, 1.0)
    return {
        "water": float(water.mean()),
        "water_peak": float(np.quantile(water, 0.82)),
        "vegetation": float(vegetation.mean()),
        "vegetation_floor": float(np.quantile(vegetation, 0.30)),
        "snow": float(snow.mean()),
        "snow_peak": float(np.quantile(snow, 0.82)),
        "rock": float(gray_rock.mean()),
        "rock_peak": float(np.quantile(np.maximum.reduce([gray_rock, purple_rock, ochre_cliff, pale_mountain]), 0.82)),
        "purple_rock": float(purple_rock.mean()),
        "ochre_cliff": float(ochre_cliff.mean()),
        "pale_mountain": float(pale_mountain.mean()),
        "luma": float(lum.mean()),
        "luma_peak": float(np.quantile(lum, 0.82)),
        "edge": edge,
    }


def candidate_grid() -> list[tuple[float, float]]:
    points: list[tuple[float, float]] = []
    for y in (0.14, 0.23, 0.32, 0.41, 0.50, 0.59, 0.68, 0.77, 0.86):
        for x in (0.14, 0.23, 0.32, 0.41, 0.50, 0.59, 0.68, 0.77, 0.86):
            points.append((x, y))
    return points


def deterministic_offset(row: int, col: int, salt: int, scale: float) -> tuple[float, float]:
    h = hashlib.sha256(f"{row}:{col}:{salt}".encode("ascii")).digest()
    dx = (h[0] / 255.0 - 0.5) * scale
    dy = (h[1] / 255.0 - 0.5) * scale
    return dx, dy


def is_land_safe(candidate: dict[str, float]) -> bool:
    f = candidate["features"]
    return (
        f["vegetation"] >= 0.390
        and f["vegetation_floor"] >= 0.180
        and f["water"] <= 0.070
        and f["water_peak"] <= 0.135
        and f["snow"] <= 0.075
        and f["snow_peak"] <= 0.155
        and f["rock"] <= 0.125
        and f["rock_peak"] <= 0.245
        and f["purple_rock"] <= 0.080
        and f["ochre_cliff"] <= 0.115
        and f["pale_mountain"] <= 0.145
        and f["luma_peak"] <= 0.625
        and 0.235 <= f["luma"] <= 0.585
    )


def is_water_safe(candidate: dict[str, float]) -> bool:
    f = candidate["features"]
    return (
        f["water"] >= 0.135
        and f["water_peak"] >= 0.240
        and f["snow"] <= 0.245
        and f["rock"] <= 0.320
    )


def anchor_bias(candidate: dict[str, float], token: str) -> float:
    if not token:
        return 0.0
    slots = {
        "pollen": (0.34, 0.36),
        "nectar": (0.66, 0.38),
        "wax": (0.58, 0.24),
        "honey": (0.26, 0.70),
        "propolis": (0.40, 0.68),
        "royal_jelly": (0.78, 0.30),
        "water": (0.24, 0.64),
    }
    x, y = slots.get(token, (0.50, 0.50))
    return -abs(candidate["x"] - x) * 0.16 - abs(candidate["y"] - y) * 0.16


def choose_anchor(scored: list[dict[str, float]], family: str, token: str = "", used: list[dict[str, float]] | None = None) -> dict[str, float]:
    used = used or []
    if family == "water":
        key = lambda c: (
            c["features"]["water"] * 2.7
            + c["features"]["vegetation"] * 0.34
            - c["features"]["snow"] * 1.2
            - c["features"]["rock"] * 0.55
            - c["features"]["edge"] * 0.9
            - abs(c["x"] - 0.50) * 0.16
            - abs(c["y"] - 0.50) * 0.16
            + anchor_bias(c, token)
        )
        allowed = [c for c in scored if is_water_safe(c)]
    elif family == "hive":
        key = lambda c: (
            c["features"]["vegetation"] * 1.9
            + (1.0 - min(1.0, c["features"]["edge"] * 8.0)) * 0.45
            - c["features"]["water"] * 2.3
            - c["features"]["snow"] * 2.2
            - c["features"]["rock"] * 0.55
            - abs(c["x"] - 0.50) * 0.20
            - abs(c["y"] - 0.56) * 0.14
        )
        allowed = [c for c in scored if is_land_safe(c)]
    elif family == "threat":
        key = lambda c: (
            c["features"]["vegetation"] * 1.0
            + c["features"]["rock"] * 0.14
            - c["features"]["water"] * 1.8
            - c["features"]["snow"] * 1.4
            - c["features"]["edge"] * 0.7
        )
        allowed = [c for c in scored if is_land_safe(c)]
    else:
        key = lambda c: (
            c["features"]["vegetation"] * 1.65
            - c["features"]["rock"] * 0.40
            - c["features"]["purple_rock"] * 0.60
            - c["features"]["water"] * 2.0
            - c["features"]["snow"] * 1.8
            - c["features"]["edge"] * 0.72
            - abs(c["x"] - 0.50) * 0.10
            + anchor_bias(c, token)
        )
        allowed = [c for c in scored if is_land_safe(c)]

    pool = allowed if allowed else scored
    ranked = sorted(pool, key=key, reverse=True)
    best = ranked[0]
    for candidate in ranked:
        if all(math.hypot(candidate["x"] - u["x"], candidate["y"] - u["y"]) >= 0.145 for u in used):
            best = candidate
            break

    result = dict(best)
    result["score"] = float(key(best))
    result["fallback"] = not allowed
    result["rejected"] = len(scored) - len(allowed)
    result["safe"] = is_water_safe(best) if family == "water" else is_land_safe(best)
    return result


def classify_tile(scored: list[dict[str, float]]) -> str:
    avg_water = sum(c["features"]["water"] for c in scored) / len(scored)
    avg_snow = sum(c["features"]["snow"] for c in scored) / len(scored)
    avg_veg = sum(c["features"]["vegetation"] for c in scored) / len(scored)
    if avg_water > 0.30:
        return "water_or_shore"
    if avg_snow > 0.28:
        return "snow_or_peak"
    if avg_veg > 0.40:
        return "vegetation"
    return "mixed_rock_meadow"


def rounded_anchor(anchor: dict[str, float], row: int, col: int, salt: int) -> tuple[float, float]:
    return round(anchor["x"], 4), round(anchor["y"], 4)


def analyze_tile(path: Path, row: int, col: int) -> dict[str, object]:
    with Image.open(path) as image:
        rgb = np.asarray(image.convert("RGB"))[GUTTER:GUTTER + CORE_SIZE, GUTTER:GUTTER + CORE_SIZE]

    scored: list[dict[str, float]] = []
    for x, y in candidate_grid():
        features = patch_features(sample_patch(rgb, x, y))
        scored.append({"x": x, "y": y, "features": features})

    hive = choose_anchor(scored, "hive")
    used_resources: list[dict[str, float]] = []
    resource_anchors: dict[str, dict[str, float]] = {}
    for token in NON_WATER_RESOURCE_TOKENS:
        anchor = choose_anchor(scored, "resource", token, used_resources)
        resource_anchors[token] = anchor
        used_resources.append(anchor)
    resource = resource_anchors["pollen"]
    water = choose_anchor(scored, "water", "water")
    threat = choose_anchor(scored, "threat")

    hive_x, hive_y = rounded_anchor(hive, row, col, 101)
    resource_x, resource_y = rounded_anchor(resource, row, col, 211)
    water_x, water_y = rounded_anchor(water, row, col, 307)
    threat_x, threat_y = rounded_anchor(threat, row, col, 409)
    resource_positions: dict[str, tuple[float, float]] = {}
    for index, token in enumerate(NON_WATER_RESOURCE_TOKENS):
        resource_positions[token] = rounded_anchor(resource_anchors[token], row, col, 501 + index * 97)
    resource_positions["water"] = (water_x, water_y)

    entry = {
        "row": row,
        "column": col,
        "chunk_x": ORIGIN_CHUNK_X + col,
        "chunk_y": ORIGIN_CHUNK_Y + row,
        "hive_x": hive_x,
        "hive_y": hive_y,
        "resource_x": resource_x,
        "resource_y": resource_y,
        "water_x": water_x,
        "water_y": water_y,
        "threat_x": threat_x,
        "threat_y": threat_y,
        "pollen_x": resource_positions["pollen"][0],
        "pollen_y": resource_positions["pollen"][1],
        "nectar_x": resource_positions["nectar"][0],
        "nectar_y": resource_positions["nectar"][1],
        "wax_x": resource_positions["wax"][0],
        "wax_y": resource_positions["wax"][1],
        "honey_x": resource_positions["honey"][0],
        "honey_y": resource_positions["honey"][1],
        "propolis_x": resource_positions["propolis"][0],
        "propolis_y": resource_positions["propolis"][1],
        "royal_jelly_x": resource_positions["royal_jelly"][0],
        "royal_jelly_y": resource_positions["royal_jelly"][1],
        "terrain": classify_tile(scored),
        "land_score": round(float(resource["score"]), 5),
        "water_score": round(float(water["score"]), 5),
        "hive_score": round(float(hive["score"]), 5),
        "threat_score": round(float(threat["score"]), 5),
        "audit_rejected_land_candidates": int(resource["rejected"]),
        "audit_rejected_water_candidates": int(water["rejected"]),
        "audit_resource_fallbacks": int(sum(1 for token in NON_WATER_RESOURCE_TOKENS if resource_anchors[token]["fallback"])),
        "audit_water_fallback": int(water["fallback"]),
    }
    for token in NON_WATER_RESOURCE_TOKENS:
        entry[f"{token}_safe"] = bool(resource_anchors[token]["safe"])
    entry["water_safe"] = bool(water["safe"])
    entry["hive_safe"] = bool(hive["safe"])
    entry["threat_safe"] = bool(threat["safe"])
    return entry


def audit_entries(entries: list[dict[str, object]]) -> dict[str, object]:
    non_water_unsafe = 0
    water_unsafe = 0
    resource_fallbacks = 0
    water_fallbacks = 0
    rejected_land = 0
    rejected_water = 0
    borrowed_land = 0
    borrowed_water = 0
    hive_unsafe = 0
    threat_unsafe = 0
    borrowed_hive = 0
    borrowed_threat = 0
    for entry in entries:
        rejected_land += int(entry["audit_rejected_land_candidates"])
        rejected_water += int(entry["audit_rejected_water_candidates"])
        resource_fallbacks += int(entry["audit_resource_fallbacks"])
        water_fallbacks += int(entry["audit_water_fallback"])
        borrowed_land += int(entry.get("audit_borrowed_land_anchors", 0))
        borrowed_water += int(entry.get("audit_borrowed_water_anchor", 0))
        borrowed_hive += int(entry.get("audit_borrowed_hive_anchor", 0))
        borrowed_threat += int(entry.get("audit_borrowed_threat_anchor", 0))
        if not bool(entry.get("hive_safe", False)):
            hive_unsafe += 1
        if not bool(entry.get("threat_safe", False)):
            threat_unsafe += 1
        for token in NON_WATER_RESOURCE_TOKENS:
            if not bool(entry[f"{token}_safe"]):
                non_water_unsafe += 1
        if not bool(entry["water_safe"]):
            water_unsafe += 1
    return {
        "policy_version": "v3_visual_mountain_veto_large_icons",
        "candidate_points_per_tile": len(candidate_grid()),
        "non_water_resource_anchor_checks": len(entries) * len(NON_WATER_RESOURCE_TOKENS),
        "non_water_resource_unsafe_anchor_count": non_water_unsafe,
        "water_resource_unsafe_anchor_count": water_unsafe,
        "hive_unsafe_anchor_count": hive_unsafe,
        "threat_unsafe_anchor_count": threat_unsafe,
        "resource_fallback_count": resource_fallbacks,
        "water_fallback_count": water_fallbacks,
        "borrowed_land_anchor_count": borrowed_land,
        "borrowed_water_anchor_count": borrowed_water,
        "borrowed_hive_anchor_count": borrowed_hive,
        "borrowed_threat_anchor_count": borrowed_threat,
        "land_candidates_rejected_by_color_veto": rejected_land,
        "water_candidates_rejected_by_color_veto": rejected_water,
    }


def repair_unsafe_anchors(entries: list[dict[str, object]]) -> None:
    safe_by_token = {
        token: [entry for entry in entries if bool(entry[f"{token}_safe"])]
        for token in RESOURCE_TOKENS
    }
    safe_by_token["hive"] = [entry for entry in entries if bool(entry["hive_safe"])]
    safe_by_token["threat"] = [entry for entry in entries if bool(entry["threat_safe"])]
    safe_by_token["land"] = safe_by_token["pollen"]

    def nearest(entry: dict[str, object], token: str) -> dict[str, object] | None:
        row = int(entry["row"])
        col = int(entry["column"])
        candidates = safe_by_token[token]
        if not candidates:
            return None
        return min(
            candidates,
            key=lambda other: (int(other["row"]) - row) ** 2 + (int(other["column"]) - col) ** 2,
        )

    for entry in entries:
        if not bool(entry["hive_safe"]):
            donor = nearest(entry, "land")
            if donor is not None:
                entry["hive_x"] = round((int(donor["column"]) + float(donor["pollen_x"])) - int(entry["column"]), 4)
                entry["hive_y"] = round((int(donor["row"]) + float(donor["pollen_y"])) - int(entry["row"]), 4)
                entry["hive_safe"] = True
                entry["audit_borrowed_hive_anchor"] = 1
            else:
                entry["audit_borrowed_hive_anchor"] = 0
        else:
            entry["audit_borrowed_hive_anchor"] = 0

        if not bool(entry["threat_safe"]):
            donor = nearest(entry, "land")
            if donor is not None:
                entry["threat_x"] = round((int(donor["column"]) + float(donor["pollen_x"])) - int(entry["column"]), 4)
                entry["threat_y"] = round((int(donor["row"]) + float(donor["pollen_y"])) - int(entry["row"]), 4)
                entry["threat_safe"] = True
                entry["audit_borrowed_threat_anchor"] = 1
            else:
                entry["audit_borrowed_threat_anchor"] = 0
        else:
            entry["audit_borrowed_threat_anchor"] = 0

        borrowed_land = 0
        for token in NON_WATER_RESOURCE_TOKENS:
            if bool(entry[f"{token}_safe"]):
                continue
            donor = nearest(entry, token)
            if donor is None:
                continue
            entry[f"{token}_x"] = round((int(donor["column"]) + float(donor[f"{token}_x"])) - int(entry["column"]), 4)
            entry[f"{token}_y"] = round((int(donor["row"]) + float(donor[f"{token}_y"])) - int(entry["row"]), 4)
            entry[f"{token}_safe"] = True
            borrowed_land += 1
        entry["audit_borrowed_land_anchors"] = borrowed_land

        if not bool(entry["water_safe"]):
            donor = nearest(entry, "water")
            if donor is not None:
                entry["water_x"] = round((int(donor["column"]) + float(donor["water_x"])) - int(entry["column"]), 4)
                entry["water_y"] = round((int(donor["row"]) + float(donor["water_y"])) - int(entry["row"]), 4)
                entry["water_safe"] = True
                entry["audit_borrowed_water_anchor"] = 1
            else:
                entry["audit_borrowed_water_anchor"] = 0
        else:
            entry["audit_borrowed_water_anchor"] = 0

        entry["resource_x"] = entry["pollen_x"]
        entry["resource_y"] = entry["pollen_y"]


def main() -> int:
    tiles: list[tuple[int, int, Path]] = []
    for path in sorted(TERRAIN_ROOT.glob("R??C??_g2.png")):
        match = TILE_RE.match(path.name)
        if not match:
            continue
        row = int(match.group("row"))
        col = int(match.group("col"))
        tiles.append((row, col, path))

    if len(tiles) != ROWS * COLUMNS:
        raise RuntimeError(f"Expected {ROWS * COLUMNS} tiles, found {len(tiles)} in {TERRAIN_ROOT}")

    entries = [analyze_tile(path, row, col) for row, col, path in tiles]
    repair_unsafe_anchors(entries)
    terrain_counts: dict[str, int] = {}
    for entry in entries:
        terrain_counts[entry["terrain"]] = terrain_counts.get(entry["terrain"], 0) + 1
    audit = audit_entries(entries)

    manifest_path = TERRAIN_ROOT / "runtime_manifest.json"
    package_hash = sha256(manifest_path) if manifest_path.exists() else ""
    payload = {
        "schema": "bee-kingdom.world-map.wave6-runtime-placement-mask.v2",
        "generated_utc": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        "source_package": "UIB_ImmenseContinuousMaster50x50_wave5method_12288_preview",
        "source_expected_master_sha256": EXPECTED_MASTER_SHA256,
        "source_runtime_manifest_sha256": package_hash,
        "origin_chunk_x": ORIGIN_CHUNK_X,
        "origin_chunk_y": ORIGIN_CHUNK_Y,
        "rows": ROWS,
        "columns": COLUMNS,
        "tile_size": CORE_SIZE,
        "gutter": GUTTER,
        "policy": {
            "version": "v3_visual_mountain_veto_large_icons",
            "terrain_art_locked": True,
            "entity_spawns_use_mask_anchors": True,
            "raw_random_spawn_allowed": False,
            "water_resources_use_water_anchor": True,
            "non_water_resources_veto_water_mountain_snow_rock_colors": True,
            "resource_specific_anchors": list(RESOURCE_TOKENS),
            "hives_and_bestiary_use_land_anchors": True,
        },
        "terrain_counts": terrain_counts,
        "audit": audit,
        "entries": entries,
    }

    OUTPUT_ROOT.mkdir(parents=True, exist_ok=True)
    REPORT_JSON.parent.mkdir(parents=True, exist_ok=True)
    text = json.dumps(payload, indent=2, ensure_ascii=True) + "\n"
    OUTPUT_JSON.write_text(text, encoding="utf-8")
    REPORT_JSON.write_text(text, encoding="utf-8")
    REPORT_RECEIPT_JSON.write_text(json.dumps({k: payload[k] for k in (
        "schema",
        "generated_utc",
        "source_package",
        "source_expected_master_sha256",
        "source_runtime_manifest_sha256",
        "origin_chunk_x",
        "origin_chunk_y",
        "rows",
        "columns",
        "policy",
        "terrain_counts",
        "audit",
    )}, indent=2, ensure_ascii=True) + "\n", encoding="utf-8")
    print(f"Wrote {OUTPUT_JSON}")
    print(f"Wrote {REPORT_JSON}")
    print(f"Wrote {REPORT_RECEIPT_JSON}")
    print(json.dumps({"entries": len(entries), "terrain_counts": terrain_counts, "audit": audit}, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
