#!/usr/bin/env python
"""Offline spatial index and player placement proof for Bee Kingdom world map.

No Unity, no server, no Assets. This is a deterministic support kernel intended
for later integration discussions with Builder-A and Server-B.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import time
from dataclasses import dataclass, asdict
from pathlib import Path
from typing import Callable, Dict, Iterable, List, Optional, Tuple

from PIL import Image


RESOURCE_KINDS = ("nectar", "pollen", "wax", "propolis", "royal_jelly_demo")
FLIGHT_STATES = ("FlyingToTarget", "Gathering", "FlyingBack", "Completed")


def hbits(*parts: object, bits: int = 64) -> int:
    text = "|".join(str(part) for part in parts)
    digest = hashlib.sha256(text.encode("utf-8")).hexdigest()
    return int(digest[: bits // 4], 16)


def stable_float(seed: int, *parts: object) -> float:
    return hbits(seed, *parts, bits=64) / float(0xFFFFFFFFFFFFFFFF)


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def stable_json(path: Path, data: object) -> str:
    text = json.dumps(data, ensure_ascii=True, indent=2, sort_keys=True) + "\n"
    path.write_text(text, encoding="utf-8")
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


@dataclass(frozen=True)
class WorldConfig:
    schema: str = "bee-kingdom.world-map-spatial-placement.v1"
    world_id: str = "BK-DEMO-WORLD-SPATIAL-WAVE1"
    game_server_id: str = "GS-DEMO-SPATIAL-001"
    chunk_size: int = 512
    chunks_x: int = 64
    chunks_y: int = 64
    sector_size_chunks: int = 4
    active_window_chunks: int = 5
    min_hive_distance: int = 300
    resource_hive_margin: int = 105
    spatial_cell_size: int = 256
    max_alliance_members: int = 100
    max_spawn_attempts: int = 96

    @property
    def world_width(self) -> int:
        return self.chunk_size * self.chunks_x

    @property
    def world_height(self) -> int:
        return self.chunk_size * self.chunks_y

    @property
    def sectors_x(self) -> int:
        return self.chunks_x // self.sector_size_chunks

    @property
    def sectors_y(self) -> int:
        return self.chunks_y // self.sector_size_chunks


@dataclass(frozen=True)
class Coord:
    x: int
    y: int


@dataclass(frozen=True)
class Hive:
    id: str
    player_id: str
    alliance_id: str
    x: int
    y: int
    chunk_id: str
    sector_id: str
    power_band: str
    placement_attempts: int


@dataclass(frozen=True)
class Resource:
    id: str
    kind: str
    x: int
    y: int
    chunk_id: str
    sector_id: str
    richness_band: int
    occupancy_state: str
    respawn_tick: int


@dataclass(frozen=True)
class Flight:
    id: str
    owner_player_id: str
    origin_hive_id: str
    destination_resource_id: str
    origin_x: int
    origin_y: int
    destination_x: int
    destination_y: int
    state: str
    air_distance: float
    ground_routes_used: bool
    painted_road_sampling_for_pathfinding: bool


class SpatialIndex:
    def __init__(self, cell_size: int) -> None:
        self.cell_size = cell_size
        self.cells: Dict[Tuple[int, int], List[Tuple[str, str]]] = {}

    def _cell(self, x: int, y: int) -> Tuple[int, int]:
        return x // self.cell_size, y // self.cell_size

    def insert_point(self, kind: str, entity_id: str, x: int, y: int) -> None:
        self.cells.setdefault(self._cell(x, y), []).append((kind, entity_id))

    def insert_segment_bbox(self, kind: str, entity_id: str, ax: int, ay: int, bx: int, by: int) -> None:
        min_x, max_x = sorted((ax, bx))
        min_y, max_y = sorted((ay, by))
        c0 = self._cell(min_x, min_y)
        c1 = self._cell(max_x, max_y)
        for cy in range(c0[1], c1[1] + 1):
            for cx in range(c0[0], c1[0] + 1):
                self.cells.setdefault((cx, cy), []).append((kind, entity_id))

    def query_radius(self, x: int, y: int, radius: int) -> List[Tuple[str, str]]:
        c0 = self._cell(x - radius, y - radius)
        c1 = self._cell(x + radius, y + radius)
        found: List[Tuple[str, str]] = []
        for cy in range(c0[1], c1[1] + 1):
            for cx in range(c0[0], c1[0] + 1):
                found.extend(self.cells.get((cx, cy), []))
        return found

    def query_rect(self, x: int, y: int, width: int, height: int) -> List[Tuple[str, str]]:
        c0 = self._cell(x, y)
        c1 = self._cell(x + width, y + height)
        found: List[Tuple[str, str]] = []
        for cy in range(c0[1], c1[1] + 1):
            for cx in range(c0[0], c1[0] + 1):
                found.extend(self.cells.get((cx, cy), []))
        return found


class SpatialWorld:
    def __init__(self, cfg: WorldConfig, seed: int, suitability: Optional[Callable[[int, int], bool]] = None) -> None:
        self.cfg = cfg
        self.seed = seed
        self.suitability = suitability
        self.hives: Dict[str, Hive] = {}
        self.resources: Dict[str, Resource] = {}
        self.flights: Dict[str, Flight] = {}
        self.index = SpatialIndex(cfg.spatial_cell_size)
        self.sector_load: Dict[str, int] = {}
        self.alliance_members: Dict[str, int] = {}
        self.reserved_zones = [
            {"id": "central_wonder_reserved", "x": cfg.world_width // 2 - 768, "y": cfg.world_height // 2 - 768, "width": 1536, "height": 1536},
            {"id": "north_event_reserved", "x": cfg.world_width // 3, "y": 1024, "width": 1024, "height": 768},
        ]

    def chunk_xy(self, x: int, y: int) -> Tuple[int, int]:
        return min(self.cfg.chunks_x - 1, max(0, x // self.cfg.chunk_size)), min(self.cfg.chunks_y - 1, max(0, y // self.cfg.chunk_size))

    def chunk_id(self, x: int, y: int) -> str:
        cx, cy = self.chunk_xy(x, y)
        return f"C{cx:02d}_{cy:02d}"

    def sector_id(self, x: int, y: int) -> str:
        cx, cy = self.chunk_xy(x, y)
        sx = cx // self.cfg.sector_size_chunks
        sy = cy // self.cfg.sector_size_chunks
        return f"S{sx:02d}_{sy:02d}"

    def tile_coord(self, x: int, y: int) -> Coord:
        return Coord(*self.chunk_xy(x, y))

    def is_reserved(self, x: int, y: int) -> bool:
        for zone in self.reserved_zones:
            if zone["x"] <= x < zone["x"] + zone["width"] and zone["y"] <= y < zone["y"] + zone["height"]:
                return True
        return False

    def is_suitable(self, x: int, y: int) -> bool:
        if x < 0 or y < 0 or x >= self.cfg.world_width or y >= self.cfg.world_height:
            return False
        if self.is_reserved(x, y):
            return False
        if self.suitability is not None and not self.suitability(x, y):
            return False
        return True

    def too_close_to_hive(self, x: int, y: int, margin: int) -> bool:
        margin_sq = margin * margin
        for kind, entity_id in self.index.query_radius(x, y, margin):
            if kind != "hive":
                continue
            hive = self.hives[entity_id]
            dx = hive.x - x
            dy = hive.y - y
            if dx * dx + dy * dy < margin_sq:
                return True
        return False

    def target_sector_for_player(self, player_id: str) -> Tuple[int, int]:
        sector_count = self.cfg.sectors_x * self.cfg.sectors_y
        idx = hbits(self.seed, player_id, "sector") % sector_count
        return idx % self.cfg.sectors_x, idx // self.cfg.sectors_x

    def place_player(self, player_id: str, alliance_id: str) -> Hive:
        if self.alliance_members.get(alliance_id, 0) >= self.cfg.max_alliance_members:
            raise ValueError(f"alliance_capacity_exceeded:{alliance_id}")
        base_sx, base_sy = self.target_sector_for_player(player_id)
        max_sector_load = math.ceil(1500 / (self.cfg.sectors_x * self.cfg.sectors_y)) * 3

        last_reason = "not_attempted"
        for attempt in range(self.cfg.max_spawn_attempts):
            # Deterministic spillover walks nearby sectors, avoiding toxic clusters.
            sector_offset = attempt // 12
            sx = (base_sx + sector_offset + (hbits(self.seed, player_id, attempt, "sx") % 3) - 1) % self.cfg.sectors_x
            sy = (base_sy + sector_offset + (hbits(self.seed, player_id, attempt, "sy") % 3) - 1) % self.cfg.sectors_y
            sector_id = f"S{sx:02d}_{sy:02d}"
            if self.sector_load.get(sector_id, 0) >= max_sector_load and attempt < self.cfg.max_spawn_attempts - 16:
                last_reason = "sector_soft_capacity"
                continue

            sector_world_x = sx * self.cfg.sector_size_chunks * self.cfg.chunk_size
            sector_world_y = sy * self.cfg.sector_size_chunks * self.cfg.chunk_size
            sector_span = self.cfg.sector_size_chunks * self.cfg.chunk_size
            x = sector_world_x + 160 + int(stable_float(self.seed, player_id, attempt, "x") * (sector_span - 320))
            y = sector_world_y + 160 + int(stable_float(self.seed, player_id, attempt, "y") * (sector_span - 320))

            if not self.is_suitable(x, y):
                last_reason = "reserved_or_unsuitable"
                continue
            if self.too_close_to_hive(x, y, self.cfg.min_hive_distance):
                last_reason = "hive_collision_margin"
                continue

            hive = Hive(
                id=f"hive_{player_id}",
                player_id=player_id,
                alliance_id=alliance_id,
                x=x,
                y=y,
                chunk_id=self.chunk_id(x, y),
                sector_id=self.sector_id(x, y),
                power_band="new_player",
                placement_attempts=attempt + 1,
            )
            self.hives[hive.id] = hive
            self.index.insert_point("hive", hive.id, x, y)
            self.sector_load[hive.sector_id] = self.sector_load.get(hive.sector_id, 0) + 1
            self.alliance_members[alliance_id] = self.alliance_members.get(alliance_id, 0) + 1
            return hive
        raise ValueError(f"placement_failed:{player_id}:{last_reason}")

    def generate_resources(self, count: int) -> None:
        placed = 0
        attempt = 0
        max_attempts = count * 10
        while placed < count and attempt < max_attempts:
            x = int(stable_float(self.seed, "resource", attempt, "x") * (self.cfg.world_width - 1))
            y = int(stable_float(self.seed, "resource", attempt, "y") * (self.cfg.world_height - 1))
            attempt += 1
            if not self.is_suitable(x, y):
                continue
            if self.too_close_to_hive(x, y, self.cfg.resource_hive_margin):
                continue
            kind = RESOURCE_KINDS[hbits(self.seed, "resource-kind", placed) % len(RESOURCE_KINDS)]
            richness = 1 + hbits(self.seed, "richness", placed) % 5
            resource = Resource(
                id=f"res_{placed:05d}",
                kind=kind,
                x=x,
                y=y,
                chunk_id=self.chunk_id(x, y),
                sector_id=self.sector_id(x, y),
                richness_band=richness,
                occupancy_state="available",
                respawn_tick=3600 + int(hbits(self.seed, "respawn", placed) % 7200),
            )
            self.resources[resource.id] = resource
            self.index.insert_point("resource", resource.id, x, y)
            placed += 1
        if placed < count:
            raise ValueError(f"resource_generation_failed:{placed}/{count}")

    def generate_demo_flights(self, count: int) -> None:
        hives = sorted(self.hives.values(), key=lambda item: item.id)
        resources = sorted(self.resources.values(), key=lambda item: item.id)
        for i in range(count):
            hive = hives[hbits(self.seed, "flight-hive", i) % len(hives)]
            resource = resources[hbits(self.seed, "flight-resource", i) % len(resources)]
            distance = math.dist((hive.x, hive.y), (resource.x, resource.y))
            flight = Flight(
                id=f"flight_{i:04d}",
                owner_player_id=hive.player_id,
                origin_hive_id=hive.id,
                destination_resource_id=resource.id,
                origin_x=hive.x,
                origin_y=hive.y,
                destination_x=resource.x,
                destination_y=resource.y,
                state=FLIGHT_STATES[hbits(self.seed, "flight-state", i) % len(FLIGHT_STATES)],
                air_distance=round(distance, 3),
                ground_routes_used=False,
                painted_road_sampling_for_pathfinding=False,
            )
            self.flights[flight.id] = flight
            self.index.insert_segment_bbox("flight", flight.id, flight.origin_x, flight.origin_y, flight.destination_x, flight.destination_y)

    def hives_within_radius(self, x: int, y: int, radius: int) -> List[Hive]:
        result = []
        r2 = radius * radius
        for kind, entity_id in self.index.query_radius(x, y, radius):
            if kind != "hive":
                continue
            hive = self.hives[entity_id]
            if (hive.x - x) ** 2 + (hive.y - y) ** 2 <= r2:
                result.append(hive)
        return sorted(result, key=lambda h: h.id)

    def entities_in_chunk(self, chunk_id: str) -> Dict[str, int]:
        return {
            "hives": sum(1 for h in self.hives.values() if h.chunk_id == chunk_id),
            "resources": sum(1 for r in self.resources.values() if r.chunk_id == chunk_id),
            "flights": sum(1 for f in self.flights.values() if self.segment_crosses_chunk(f, chunk_id)),
        }

    def entities_in_sector(self, sector_id: str) -> Dict[str, int]:
        return {
            "hives": sum(1 for h in self.hives.values() if h.sector_id == sector_id),
            "resources": sum(1 for r in self.resources.values() if r.sector_id == sector_id),
            "flights": sum(1 for f in self.flights.values() if self.segment_crosses_sector(f, sector_id)),
        }

    def nearest_free_location(self, x: int, y: int, margin: int) -> Dict[str, object]:
        for radius in range(0, 2400 + 1, 80):
            steps = max(8, radius // 40) if radius else 1
            for step in range(steps):
                angle = (math.tau * step) / steps if radius else 0
                cx = min(self.cfg.world_width - 1, max(0, int(x + math.cos(angle) * radius)))
                cy = min(self.cfg.world_height - 1, max(0, int(y + math.sin(angle) * radius)))
                if self.is_suitable(cx, cy) and not self.too_close_to_hive(cx, cy, margin):
                    return {"found": True, "x": cx, "y": cy, "radius_checked": radius, "chunk_id": self.chunk_id(cx, cy), "sector_id": self.sector_id(cx, cy)}
        return {"found": False, "reason": "no_free_location_within_radius", "radius_checked": 2400}

    def resources_available(self, x: int, y: int, radius: int, limit: int = 50) -> List[Resource]:
        result = []
        r2 = radius * radius
        for kind, entity_id in self.index.query_radius(x, y, radius):
            if kind != "resource":
                continue
            resource = self.resources[entity_id]
            if resource.occupancy_state == "available" and (resource.x - x) ** 2 + (resource.y - y) ** 2 <= r2:
                result.append(resource)
        return sorted(result, key=lambda r: ((r.x - x) ** 2 + (r.y - y) ** 2, r.id))[:limit]

    def flights_crossing_rect(self, x: int, y: int, width: int, height: int) -> List[Flight]:
        rect = (x, y, x + width, y + height)
        result = []
        seen = set()
        for kind, entity_id in self.index.query_rect(x, y, width, height):
            if kind != "flight" or entity_id in seen:
                continue
            seen.add(entity_id)
            flight = self.flights[entity_id]
            if segment_intersects_rect((flight.origin_x, flight.origin_y), (flight.destination_x, flight.destination_y), rect):
                result.append(flight)
        return sorted(result, key=lambda f: f.id)

    def segment_crosses_chunk(self, flight: Flight, chunk_id: str) -> bool:
        cx = int(chunk_id[1:3])
        cy = int(chunk_id[4:6])
        rect = (cx * self.cfg.chunk_size, cy * self.cfg.chunk_size, (cx + 1) * self.cfg.chunk_size, (cy + 1) * self.cfg.chunk_size)
        return segment_intersects_rect((flight.origin_x, flight.origin_y), (flight.destination_x, flight.destination_y), rect)

    def segment_crosses_sector(self, flight: Flight, sector_id: str) -> bool:
        sx = int(sector_id[1:3])
        sy = int(sector_id[4:6])
        span = self.cfg.sector_size_chunks * self.cfg.chunk_size
        rect = (sx * span, sy * span, (sx + 1) * span, (sy + 1) * span)
        return segment_intersects_rect((flight.origin_x, flight.origin_y), (flight.destination_x, flight.destination_y), rect)

    def snapshot(self) -> Dict[str, object]:
        return {
            "schema": self.cfg.schema,
            "world": asdict(self.cfg) | {
                "world_width": self.cfg.world_width,
                "world_height": self.cfg.world_height,
                "sectors_x": self.cfg.sectors_x,
                "sectors_y": self.cfg.sectors_y,
            },
            "seed": self.seed,
            "non_live": True,
            "server_authoritative_future_only": True,
            "ground_routes_used": False,
            "painted_road_sampling_for_pathfinding": False,
            "reserved_zones": self.reserved_zones,
            "sector_load": dict(sorted(self.sector_load.items())),
            "alliance_members": dict(sorted(self.alliance_members.items())),
            "hives": [asdict(hive) for hive in sorted(self.hives.values(), key=lambda h: h.id)],
            "resources": [asdict(resource) for resource in sorted(self.resources.values(), key=lambda r: r.id)],
            "flights": [asdict(flight) for flight in sorted(self.flights.values(), key=lambda f: f.id)],
        }


def segment_intersects_rect(a: Tuple[int, int], b: Tuple[int, int], rect: Tuple[int, int, int, int]) -> bool:
    x0, y0, x1, y1 = rect
    if (x0 <= a[0] <= x1 and y0 <= a[1] <= y1) or (x0 <= b[0] <= x1 and y0 <= b[1] <= y1):
        return True
    edges = [((x0, y0), (x1, y0)), ((x1, y0), (x1, y1)), ((x1, y1), (x0, y1)), ((x0, y1), (x0, y0))]
    return any(segments_intersect(a, b, c, d) for c, d in edges)


def segments_intersect(a: Tuple[int, int], b: Tuple[int, int], c: Tuple[int, int], d: Tuple[int, int]) -> bool:
    def orient(p: Tuple[int, int], q: Tuple[int, int], r: Tuple[int, int]) -> int:
        value = (q[1] - p[1]) * (r[0] - q[0]) - (q[0] - p[0]) * (r[1] - q[1])
        return 0 if value == 0 else (1 if value > 0 else 2)

    o1 = orient(a, b, c)
    o2 = orient(a, b, d)
    o3 = orient(c, d, a)
    o4 = orient(c, d, b)
    return o1 != o2 and o3 != o4


def build_suitability(path: Optional[Path], cfg: WorldConfig) -> Optional[Callable[[int, int], bool]]:
    if path is None:
        return None
    image = Image.open(path).convert("RGB")

    def is_ok(x: int, y: int) -> bool:
        px = min(image.width - 1, max(0, int(x / cfg.world_width * image.width)))
        py = min(image.height - 1, max(0, int(y / cfg.world_height * image.height)))
        r, g, b = image.getpixel((px, py))
        # Explicit proxy heuristic only when a suitability image is provided:
        # reject very dark/deep-blue pixels as water/steep unusable terrain.
        return not (b > r + 35 and b > g + 25 and b > 95)

    return is_ok


def run_world(output: Path, seed: int, players: int, resources: int, flights: int, suitability_path: Optional[Path]) -> Dict[str, object]:
    cfg = WorldConfig()
    world = SpatialWorld(cfg, seed, build_suitability(suitability_path, cfg))
    output.mkdir(parents=True, exist_ok=True)
    timings: Dict[str, float] = {}

    start = time.perf_counter()
    for i in range(players):
        player_id = f"player_{i:04d}"
        alliance_id = f"alliance_{i // cfg.max_alliance_members:02d}"
        world.place_player(player_id, alliance_id)
    timings["place_players_seconds"] = round(time.perf_counter() - start, 6)

    start = time.perf_counter()
    world.generate_resources(resources)
    timings["generate_resources_seconds"] = round(time.perf_counter() - start, 6)

    start = time.perf_counter()
    world.generate_demo_flights(flights)
    timings["generate_flights_seconds"] = round(time.perf_counter() - start, 6)

    snapshot = world.snapshot()
    snapshot_hash = stable_json(output / "snapshot.json", snapshot)
    readback = json.loads((output / "snapshot.json").read_text(encoding="utf-8"))
    readback_hash = hashlib.sha256((json.dumps(readback, ensure_ascii=True, indent=2, sort_keys=True) + "\n").encode("utf-8")).hexdigest()

    validation = validate_world(world, snapshot_hash, readback_hash, timings)
    stable_json(output / "validation.json", validation)
    write_integration_contract(output)
    return validation


def validate_world(world: SpatialWorld, snapshot_hash: str, readback_hash: str, timings: Dict[str, float]) -> Dict[str, object]:
    cfg = world.cfg
    start = time.perf_counter()
    hives = list(world.hives.values())
    collisions = 0
    min_distance = None
    for i, hive in enumerate(hives):
        for other in world.hives_within_radius(hive.x, hive.y, cfg.min_hive_distance - 1):
            if other.id <= hive.id:
                continue
            collisions += 1
        for other in world.hives_within_radius(hive.x, hive.y, cfg.min_hive_distance * 2):
            if other.id == hive.id:
                continue
            dist = math.dist((hive.x, hive.y), (other.x, other.y))
            min_distance = dist if min_distance is None else min(min_distance, dist)
    timings["collision_scan_seconds"] = round(time.perf_counter() - start, 6)

    start = time.perf_counter()
    sample_center = hives[len(hives) // 2]
    neighbor_sample = world.hives_within_radius(sample_center.x, sample_center.y, 1200)
    resources_sample = world.resources_available(sample_center.x, sample_center.y, 1600, limit=25)
    chunk_sample = world.entities_in_chunk(sample_center.chunk_id)
    sector_sample = world.entities_in_sector(sample_center.sector_id)
    free_sample = world.nearest_free_location(sample_center.x, sample_center.y, cfg.min_hive_distance)
    rect_x = max(0, sample_center.x - 1200)
    rect_y = max(0, sample_center.y - 900)
    flights_sample = world.flights_crossing_rect(rect_x, rect_y, 2400, 1800)
    timings["query_sample_seconds"] = round(time.perf_counter() - start, 6)

    boundary_points = [
        {"x": 511, "y": 511, "chunk_id": world.chunk_id(511, 511), "sector_id": world.sector_id(511, 511)},
        {"x": 512, "y": 512, "chunk_id": world.chunk_id(512, 512), "sector_id": world.sector_id(512, 512)},
        {"x": 2047, "y": 2047, "chunk_id": world.chunk_id(2047, 2047), "sector_id": world.sector_id(2047, 2047)},
        {"x": 2048, "y": 2048, "chunk_id": world.chunk_id(2048, 2048), "sector_id": world.sector_id(2048, 2048)},
    ]

    alliance_ok = all(count <= cfg.max_alliance_members for count in world.alliance_members.values())
    sector_counts = list(world.sector_load.values())
    validation = {
        "snapshot_hash": snapshot_hash,
        "readback_hash": readback_hash,
        "snapshot_readback_identical": snapshot_hash == readback_hash,
        "counts": {
            "players": len(world.hives),
            "resources": len(world.resources),
            "flights": len(world.flights),
            "sectors_with_players": len(world.sector_load),
            "max_sector_player_count": max(sector_counts),
            "min_sector_player_count": min(sector_counts),
            "alliances": len(world.alliance_members),
            "max_alliance_members": max(world.alliance_members.values()),
        },
        "collisions": {
            "hive_margin": cfg.min_hive_distance,
            "collision_count": collisions,
            "minimum_observed_distance": round(min_distance or 0, 3),
        },
        "queries": {
            "neighbors_radius_count": len(neighbor_sample),
            "entities_in_chunk": chunk_sample,
            "entities_in_sector": sector_sample,
            "nearest_free_location": free_sample,
            "resources_available_count": len(resources_sample),
            "flights_crossing_zone_count": len(flights_sample),
        },
        "chunk_boundaries": boundary_points,
        "alliance_max_100_ok": alliance_ok,
        "air_flights_only": all(not f.ground_routes_used and not f.painted_road_sampling_for_pathfinding for f in world.flights.values()),
        "timings": timings,
    }
    required_ok = (
        len(world.hives) == 1500
        and len(world.resources) >= 10000
        and collisions == 0
        and validation["snapshot_readback_identical"]
        and alliance_ok
        and validation["air_flights_only"]
        and len(flights_sample) >= 0
    )
    validation["overall_ok"] = required_ok
    if not required_ok:
        raise SystemExit("Spatial validation failed.")
    return validation


def write_integration_contract(output: Path) -> None:
    text = """# World Map Spatial Placement Wave 1 - Integration Contract

Status: offline support only. No live server claim.

## Coordinate Contract

- WorldCoord uses integer X/Y.
- Chunk size is 512 world units.
- ChunkId format is Cxx_yy.
- Sector groups 4x4 chunks.
- SectorId format is Sxx_yy.
- Active viewport window remains compatible with 5x5 chunks.

## Placement Contract

- Input: world seed, playerId, allianceId, existing occupancy snapshot.
- Output: hive marker with WorldId, GameServerId, owner, alliance, WorldCoord, ChunkId, SectorId.
- Failure must return a bounded reason such as alliance_capacity_exceeded, sector_soft_capacity, reserved_or_unsuitable, hive_collision_margin, or placement_failed.
- Server-B remains authoritative in future integration.

## Query Contract

- neighbors within radius;
- entities by chunk;
- entities by sector;
- nearest free location;
- available resources;
- flights crossing a rectangular zone.

## Flight Contract

- Flights store origin and destination WorldCoord.
- Air distance is direct Euclidean distance.
- ground_routes_used is always false in this kernel.
- painted road sampling for pathfinding is always false.
"""
    (output / "integration_contract.md").write_text(text, encoding="utf-8")


def compare_runs(a: Path, b: Path, output: Path) -> Dict[str, object]:
    files = ["snapshot.json", "integration_contract.md"]
    rows = []
    mismatches = []
    for rel in files:
        ha = sha256_file(a / rel)
        hb = sha256_file(b / rel)
        match = ha == hb
        if not match:
            mismatches.append(rel)
        rows.append({"file": rel, "run_a_sha256": ha, "run_b_sha256": hb, "match": match})
    result = {"files_compared": len(files), "all_hashes_match": not mismatches, "mismatches": mismatches, "rows": rows}
    stable_json(output, result)
    return result


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    parser.add_argument("--seed", type=int, default=738921)
    parser.add_argument("--players", type=int, default=1500)
    parser.add_argument("--resources", type=int, default=10000)
    parser.add_argument("--flights", type=int, default=300)
    parser.add_argument("--suitability-map")
    args = parser.parse_args()

    root = Path(args.output).resolve()
    if root.exists():
        import shutil

        shutil.rmtree(root)
    root.mkdir(parents=True, exist_ok=True)

    suitability = Path(args.suitability_map).resolve() if args.suitability_map else None
    run1 = root / "run1"
    run2 = root / "run2"
    seed_alt = root / "seed_alt"
    v1 = run_world(run1, args.seed, args.players, args.resources, args.flights, suitability)
    v2 = run_world(run2, args.seed, args.players, args.resources, args.flights, suitability)
    v3 = run_world(seed_alt, args.seed + 1, args.players, args.resources, args.flights, suitability)
    compare = compare_runs(run1, run2, root / "determinism_compare.json")
    distribution_changed = v1["snapshot_hash"] != v3["snapshot_hash"]
    summary = {
        "overall_ok": v1["overall_ok"] and v2["overall_ok"] and compare["all_hashes_match"] and distribution_changed,
        "determinism_same_seed": compare,
        "different_seed_distribution_changed": distribution_changed,
        "run1": v1,
        "run2": v2,
        "seed_alt": v3,
    }
    stable_json(root / "summary.json", summary)
    if not summary["overall_ok"]:
        raise SystemExit("Wave 1 summary failed.")


if __name__ == "__main__":
    main()
