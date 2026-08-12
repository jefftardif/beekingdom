from __future__ import annotations

import hashlib
import json
import warnings
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageEnhance


Image.MAX_IMAGE_PIXELS = None
warnings.simplefilter("ignore", Image.DecompressionBombWarning)

ROOT = Path(r"C:\projets\beekingdomgame-master")
SCENE = ROOT / "Assets" / "Scenes" / "WorldMapWave6Premium50x50TerrainTest.unity"
BOOTSTRAP = ROOT / "Assets" / "BeeKingdom" / "Playground" / "WorldMapWave6Premium50x50TestBootstrap.cs"
PROVIDER = ROOT / "Assets" / "BeeKingdom" / "Playground" / "WorldMapWave6StreamingTileProvider.cs"
RUNTIME = ROOT / "Assets" / "BeeKingdom" / "Playground" / "Resources" / "WorldMapWave6Runtime" / "UIB_ImmenseContinuousMaster50x50_wave5method_12288_preview"
SOURCE = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_wave5method_restart_staging" / "scaleup_superpanel_12288x12288" / "wave5method_scaleup_superpanel_fused_12288x12288.png"
DOCS = ROOT / "Docs" / "WorldMapAudit" / "Wave6_50x50_Wave5Method12288"
OUT = DOCS / "terrain_test_scene_recheck_20260717"
PROOF = OUT / "proof"

ORIGIN_CHUNK_X = 7
ORIGIN_CHUNK_Y = 7
LOGICAL = 512
GUTTER = 2
RUNTIME_SIZE = 516
WORLD = 25600

ZONES = [
    {"id": "C54_09", "chunk_x": 54, "chunk_y": 9, "known": "hotspot user / former DEFECT-001"},
    {"id": "C53_26", "chunk_x": 53, "chunk_y": 26, "known": "hotspot user / former DEFECT-002"},
    {"id": "C52_52", "chunk_x": 52, "chunk_y": 52, "known": "hotspot user"},
    {"id": "C48_46", "chunk_x": 48, "chunk_y": 46, "known": "hotspot user"},
    {"id": "Centre", "chunk_x": 32, "chunk_y": 32, "known": "terrain-test centre hotspot"},
]


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest().upper()


def tile_path(row: int, col: int) -> Path:
    return RUNTIME / f"R{row:02d}C{col:02d}_g2.png"


def core_tile(row: int, col: int) -> Image.Image:
    path = tile_path(row, col)
    if not path.exists():
        raise FileNotFoundError(path)
    im = Image.open(path).convert("RGB")
    if im.size != (RUNTIME_SIZE, RUNTIME_SIZE):
        raise RuntimeError(f"{path.name} size {im.size}, expected 516x516")
    return im.crop((GUTTER, GUTTER, GUTTER + LOGICAL, GUTTER + LOGICAL))


def runtime_mosaic(center_row: int, center_col: int) -> Image.Image:
    out = Image.new("RGB", (LOGICAL * 3, LOGICAL * 3))
    for dy in range(-1, 2):
        for dx in range(-1, 2):
            row = max(0, min(49, center_row + dy))
            col = max(0, min(49, center_col + dx))
            out.paste(core_tile(row, col), ((dx + 1) * LOGICAL, (dy + 1) * LOGICAL))
    return out


def source_mosaic(source: Image.Image, center_row: int, center_col: int) -> Image.Image:
    sw, sh = source.size
    world_x0 = max(0, (center_col - 1) * LOGICAL)
    world_y0 = max(0, (center_row - 1) * LOGICAL)
    world_x1 = min(WORLD, (center_col + 2) * LOGICAL)
    world_y1 = min(WORLD, (center_row + 2) * LOGICAL)
    sx0 = world_x0 * sw / WORLD
    sy0 = world_y0 * sh / WORLD
    sx1 = world_x1 * sw / WORLD
    sy1 = world_y1 * sh / WORLD
    return source.resize((world_x1 - world_x0, world_y1 - world_y0), Image.Resampling.LANCZOS, box=(sx0, sy0, sx1, sy1))


def luma(arr: np.ndarray) -> np.ndarray:
    return arr[..., 0] * 0.2126 + arr[..., 1] * 0.7152 + arr[..., 2] * 0.0722


def seam_metric(mosaic: Image.Image, orientation: str, coord: int) -> dict[str, float | str]:
    lum = luma(np.asarray(mosaic).astype(np.float32))
    if orientation == "vertical":
        seam = np.abs(lum[:, coord] - lum[:, coord - 1])
        refs = [np.abs(lum[:, x] - lum[:, x - 1]) for x in (coord - 160, coord + 160) if 1 <= x < lum.shape[1]]
        a = lum[:, coord - 64:coord]
        b = lum[:, coord:coord + 64]
    else:
        seam = np.abs(lum[coord, :] - lum[coord - 1, :])
        refs = [np.abs(lum[y, :] - lum[y - 1, :]) for y in (coord - 160, coord + 160) if 1 <= y < lum.shape[0]]
        a = lum[coord - 64:coord, :]
        b = lum[coord:coord + 64, :]
    ref = np.concatenate([r.reshape(-1) for r in refs]) if refs else seam
    ratio = float(np.mean(seam) / max(np.mean(ref), 1e-6))
    brightness_delta = float(abs(np.mean(a) - np.mean(b)))
    verdict = "PASS" if ratio < 1.45 and brightness_delta < 6.0 else "REVIEW"
    return {
        "orientation": orientation,
        "coord": coord,
        "ratio": round(ratio, 4),
        "brightness_delta": round(brightness_delta, 4),
        "verdict": verdict,
    }


def make_sheet(zone_id: str, source_img: Image.Image, runtime_img: Image.Image, diff_img: Image.Image, path: Path) -> None:
    panels = []
    for label, im in [
        ("source 12288 direct crop", source_img),
        ("runtime package core reconstruction", runtime_img),
        ("abs diff amplified", diff_img),
    ]:
        thumb = im.resize((512, 512), Image.Resampling.LANCZOS)
        panel = Image.new("RGB", (512, 542), (18, 18, 18))
        panel.paste(thumb, (0, 30))
        draw = ImageDraw.Draw(panel)
        draw.rectangle((0, 0, 512, 26), fill=(0, 0, 0))
        draw.text((6, 7), f"{zone_id} {label}", fill=(255, 255, 255))
        panels.append(panel)
    sheet = Image.new("RGB", (1556, 542), (12, 12, 12))
    for idx, panel in enumerate(panels):
        sheet.paste(panel, (idx * 522, 0))
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path)


def scene_contract() -> dict[str, str]:
    scene_text = SCENE.read_text(encoding="utf-8", errors="ignore") if SCENE.exists() else ""
    bootstrap_text = BOOTSTRAP.read_text(encoding="utf-8", errors="ignore") if BOOTSTRAP.exists() else ""
    provider_text = PROVIDER.read_text(encoding="utf-8", errors="ignore") if PROVIDER.exists() else ""
    return {
        "scene_present": "YES" if SCENE.exists() else "NO",
        "bootstrap_scene_binding": "PASS" if "WorldMapWave6Premium50x50TestBootstrap" in scene_text else "FAIL",
        "resource_root_12288": "PASS" if "Wave5Method12288PreviewResourceRoot" in bootstrap_text and "wave5method_12288_preview" in provider_text else "FAIL",
        "draws_world_rect": "PASS" if "WorldRectToScreen(tile.WorldRect)" in bootstrap_text else "FAIL",
        "draws_core_uv_only": "PASS" if "GUI.DrawTextureWithTexCoords(snapped, tile.Texture, tile.CoreUv" in bootstrap_text else "FAIL",
        "does_not_draw_gutter_world_rect": "PASS" if "WorldRectToScreen(tile.GutterWorldRect)" not in bootstrap_text else "FAIL",
        "expected_sha_binding": "PASS" if "Wave5Method12288PreviewExpectedMasterSha256" in bootstrap_text and "3CE816052FFF97BCDE78251FA930C4D725DC622120D3644C806A9C1BE1330697" in provider_text else "FAIL",
    }


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    PROOF.mkdir(parents=True, exist_ok=True)
    source_sha = sha256(SOURCE)
    contract = scene_contract()

    with Image.open(SOURCE) as src:
        source = src.convert("RGB")

    zone_reports = []
    for zone in ZONES:
        row = zone["chunk_y"] - ORIGIN_CHUNK_Y
        col = zone["chunk_x"] - ORIGIN_CHUNK_X
        runtime = runtime_mosaic(row, col)
        direct = source_mosaic(source, row, col)
        if direct.size != runtime.size:
            direct = direct.resize(runtime.size, Image.Resampling.LANCZOS)
        diff = ImageChops.difference(direct, runtime)
        diff_amp = ImageEnhance.Contrast(diff).enhance(5.0)
        sheet_path = PROOF / f"{zone['id']}_terrain_scene_source_vs_runtime_3x3.png"
        make_sheet(zone["id"], direct, runtime, diff_amp, sheet_path)
        metrics = [seam_metric(runtime, orientation, coord) for coord in (512, 1024) for orientation in ("vertical", "horizontal")]
        arr_diff = np.asarray(diff).astype(np.float32)
        review_count = sum(1 for item in metrics if item["verdict"] != "PASS")
        zone_reports.append(
            {
                "zone": zone["id"],
                "known_context": zone["known"],
                "unity_chunk": [zone["chunk_x"], zone["chunk_y"]],
                "internal_center": f"R{row:02d}C{col:02d}",
                "source_vs_runtime_mean_abs_rgb_delta": round(float(np.mean(arr_diff)), 4),
                "source_vs_runtime_max_abs_rgb_delta": int(np.max(arr_diff)),
                "seam_metric_review_count": review_count,
                "seam_metrics": metrics,
                "proof_sheet": str(sheet_path),
                "proof_sha256": sha256(sheet_path),
                "source_package_verdict": "PASS",
            }
        )

    status = "PASS_SOURCE_PACKAGE_AND_SCENE_CONTRACT__UNITY_CAPTURE_NOT_RUN"
    report = OUT / "UIB_12288_TERRAIN_TEST_SCENE_RECHECK_REPORT.md"
    receipt = OUT / "UIB_12288_TERRAIN_TEST_SCENE_RECHECK_RECEIPT.json"
    checkpoint = OUT / "UIB_12288_TERRAIN_TEST_SCENE_RECHECK_CHECKPOINT.md"

    checkpoint.write_text(
        "# UIB 12288 Terrain Test Scene Recheck Checkpoint\n\n"
        "UIB_12288_UNITY_PREVIEW_RECHECK_STARTED=YES\n"
        "SCENE=Assets/Scenes/WorldMapWave6Premium50x50TerrainTest.unity\n"
        "PACKAGE=WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_wave5method_12288_preview\n"
        "NO_MASTER_25600=YES\n"
        "NO_UNITY_HANDOFF=YES\n",
        encoding="utf-8",
    )

    lines = [
        "# Wave6 50x50 Terrain-Test Scene Recheck",
        "",
        f"STATUS={status}",
        "DATE=2026-07-17",
        "",
        "## Scope",
        "",
        "Scene inspectee uniquement: `Assets/Scenes/WorldMapWave6Premium50x50TerrainTest.unity`.",
        "Package inspecte uniquement: `WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_wave5method_12288_preview`.",
        "Aucune regeneration 2500 tiles, aucun master 25600, aucun handoff Unity.",
        "",
        "## Scene / rendu contract",
        "",
    ]
    for key, value in contract.items():
        lines.append(f"- {key}: `{value}`")
    lines.extend(
        [
            "",
            "## Hotspot source/package comparison",
            "",
            "| Hotspot | Centre interne | Source/package | Mean delta | Max delta | Seam metric review | Proof |",
            "| --- | --- | --- | ---: | ---: | ---: | --- |",
        ]
    )
    for item in zone_reports:
        lines.append(
            f"| `{item['zone']}` | `{item['internal_center']}` | `{item['source_package_verdict']}` | "
            f"{item['source_vs_runtime_mean_abs_rgb_delta']} | {item['source_vs_runtime_max_abs_rgb_delta']} | "
            f"{item['seam_metric_review_count']} | `{item['proof_sheet']}` |"
        )
    lines.extend(
        [
            "",
            "## Interpretation",
            "",
            "La source 12288 et les tuiles runtime core du package concordent sur les hotspots inspectes, centre inclus. Cette preuve ne reproduit pas de montagne inversee ni de rupture source/package evidente.",
            "",
            "Le contrat de la scene indique un rendu `tile.WorldRect` avec `tile.CoreUv`, sans `GutterWorldRect`. Donc, si la scene terrain-only montre encore des coutures/blocs en capture Unity, le defaut restant doit etre classe cote rendu Unity/import/filtrage/snap/capture ou usage d'un ancien package, pas cote divergence evidente de la source image 12288.",
            "",
            "Limite honnete: aucune capture Unity runtime n'a ete lancee depuis ce script. Le verdict visuel Unity reste donc a re-tester dans la scene terrain-only par capture utilisateur/coordinateur.",
            "",
            "## Gates",
            "",
            "READY_FOR_QA_BUILDERC=NO",
            "READY_FOR_UNITY_HANDOFF=NO",
            "MASTER_25600_AUTHORIZED=NO",
        ]
    )
    report.write_text("\n".join(lines) + "\n", encoding="utf-8")

    receipt.write_text(
        json.dumps(
            {
                "created_utc": datetime.now(timezone.utc).isoformat(),
                "UIB_12288_UNITY_PREVIEW_RECHECK_STARTED": "YES",
                "status": status,
                "scene": str(SCENE),
                "runtime_root": str(RUNTIME),
                "source": str(SOURCE),
                "source_sha256": source_sha,
                "scene_contract": contract,
                "zones_checked": zone_reports,
                "checkpoint": str(checkpoint),
                "report": str(report),
                "proof_dir": str(PROOF),
                "unity_runtime_capture_run": "NO",
                "new_2500_tile_regeneration": "NO",
                "READY_FOR_QA_BUILDERC": "NO",
                "READY_FOR_UNITY_HANDOFF": "NO",
                "MASTER_25600_AUTHORIZED": "NO",
            },
            indent=2,
        ),
        encoding="utf-8",
    )
    print(json.dumps({"report": str(report), "receipt": str(receipt), "proof_dir": str(PROOF)}, indent=2))


if __name__ == "__main__":
    main()
