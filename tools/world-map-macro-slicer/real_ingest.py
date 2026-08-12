#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
from typing import Any, Callable

import numpy as np
from PIL import Image

from worldmap_macro_slicer.core import GUTTER, slice_master, verify_bundle


EXPECTED_IDS = [f"R{row}C{column}" for row in range(5) for column in range(5)]


class RealIngestError(RuntimeError):
    pass


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def read_json(path: Path) -> dict[str, Any]:
    data = json.loads(path.read_text(encoding="utf-8-sig"))
    if not isinstance(data, dict):
        raise RealIngestError(f"Objet JSON attendu: {path}")
    return data


def write_json(path: Path, data: dict[str, Any]) -> None:
    path.write_text(
        json.dumps(data, indent=2, sort_keys=True, ensure_ascii=False) + "\n",
        encoding="utf-8",
        newline="\n",
    )


def load_pixels(path: Path) -> tuple[np.ndarray, str, tuple[int, int]]:
    with Image.open(path) as image:
        image.load()
        return np.asarray(image, dtype=np.uint8).copy(), image.mode, image.size


def pixel_difference_count(left: np.ndarray, right: np.ndarray) -> int:
    if left.shape != right.shape:
        return max(left.shape[0] * left.shape[1], right.shape[0] * right.shape[1])
    return int(np.count_nonzero(np.any(left != right, axis=2)))


def expected_neighbors(row: int, column: int) -> dict[str, str | None]:
    return {
        "N": f"R{row - 1}C{column}" if row > 0 else None,
        "E": f"R{row}C{column + 1}" if column < 4 else None,
        "S": f"R{row + 1}C{column}" if row < 4 else None,
        "W": f"R{row}C{column - 1}" if column > 0 else None,
    }


def preflight_ui_bundle(ui_dir: Path, expected_hash: str) -> tuple[dict[str, Any], np.ndarray]:
    ui_dir = ui_dir.resolve()
    master_path = ui_dir / "master_5x5_2560.png"
    manifest_path = ui_dir / "manifest.json"
    hashes_path = ui_dir / "hashes_sha256.json"
    mechanical_path = ui_dir / "mechanical_validation.json"
    perceptual_path = ui_dir / "perceptual_review_uib.json"
    for required in (master_path, manifest_path, hashes_path, mechanical_path, perceptual_path):
        if not required.is_file():
            raise RealIngestError(f"Fichier UI-B requis absent: {required}")

    expected_hash = expected_hash.lower()
    master_hash = sha256_file(master_path)
    if master_hash != expected_hash:
        raise RealIngestError(f"Hash master inattendu: {master_hash}, attendu {expected_hash}")
    master, master_mode, master_size = load_pixels(master_path)
    if master_mode != "RGB" or master_size != (2560, 2560):
        raise RealIngestError(f"Contrat master invalide: mode={master_mode}, size={master_size}")

    manifest = read_json(manifest_path)
    if manifest.get("schema") != "bee-kingdom.world-map.continuous-master-wave3.v1":
        raise RealIngestError("Schéma manifest UI-B inattendu.")
    master_entry = manifest.get("master")
    if not isinstance(master_entry, dict):
        raise RealIngestError("Entrée master absente du manifest UI-B.")
    expected_master_entry = {
        "file": master_path.name,
        "height": 2560,
        "mode": "RGB",
        "sha256": expected_hash,
        "width": 2560,
    }
    if master_entry != expected_master_entry:
        raise RealIngestError(f"Entrée master UI-B incohérente: {master_entry}")

    tiling = manifest.get("tiling")
    if not isinstance(tiling, dict):
        raise RealIngestError("Bloc tiling UI-B absent.")
    for key, value in {
        "rows": 5,
        "columns": 5,
        "tile_width": 512,
        "tile_height": 512,
        "future_boundaries_x": [512, 1024, 1536, 2048],
        "future_boundaries_y": [512, 1024, 1536, 2048],
    }.items():
        if tiling.get(key) != value:
            raise RealIngestError(f"Contrat tiling UI-B incohérent pour {key}.")
    entries = tiling.get("tiles")
    if not isinstance(entries, list) or len(entries) != 25:
        raise RealIngestError("Le manifest UI-B doit contenir 25 tuiles.")
    if [entry.get("id") for entry in entries if isinstance(entry, dict)] != EXPECTED_IDS:
        raise RealIngestError("Ordre UI-B différent de R0C0..R4C4.")

    reconstructed = np.zeros_like(master)
    ui_tile_hashes: dict[str, str] = {}
    tile_pixel_differences: dict[str, int] = {}
    for index, tile_id in enumerate(EXPECTED_IDS):
        row, column = divmod(index, 5)
        entry = entries[index]
        expected_file = f"tiles/{tile_id}.png"
        expected_contract = {
            "id": tile_id,
            "row": row,
            "column": column,
            "file": expected_file,
            "width": 512,
            "height": 512,
            "neighbors": expected_neighbors(row, column),
        }
        for key, value in expected_contract.items():
            if entry.get(key) != value:
                raise RealIngestError(f"Métadonnée UI-B {tile_id}.{key} incohérente.")
        tile_path = ui_dir / expected_file
        if not tile_path.is_file():
            raise RealIngestError(f"Tuile UI-B absente: {tile_path}")
        tile_hash = sha256_file(tile_path)
        if tile_hash != str(entry.get("sha256", "")).lower():
            raise RealIngestError(f"Hash de tuile UI-B incohérent: {tile_id}")
        pixels, mode, size = load_pixels(tile_path)
        if mode != "RGB" or size != (512, 512):
            raise RealIngestError(f"Contrat image UI-B invalide: {tile_id}")
        expected_pixels = master[row * 512 : (row + 1) * 512, column * 512 : (column + 1) * 512, :]
        difference = pixel_difference_count(pixels, expected_pixels)
        if difference:
            raise RealIngestError(f"Tuile UI-B {tile_id} diffère du master sur {difference} pixels.")
        reconstructed[row * 512 : (row + 1) * 512, column * 512 : (column + 1) * 512, :] = pixels
        ui_tile_hashes[tile_id] = tile_hash
        tile_pixel_differences[tile_id] = difference
    if len(set(ui_tile_hashes.values())) != 25:
        raise RealIngestError("Les hashes des 25 tuiles UI-B ne sont pas uniques.")

    reconstructed_difference = pixel_difference_count(reconstructed, master)
    if reconstructed_difference:
        raise RealIngestError("La reconstruction mémoire UI-B n'est pas pixel-identique.")
    reconstruction_entry = manifest.get("reconstruction")
    if not isinstance(reconstruction_entry, dict):
        raise RealIngestError("Entrée reconstruction UI-B absente.")
    reconstruction_path = ui_dir / str(reconstruction_entry.get("file", ""))
    reconstruction_hash = sha256_file(reconstruction_path)
    if reconstruction_hash != expected_hash or reconstruction_entry.get("sha256") != expected_hash:
        raise RealIngestError("Hash de reconstruction UI-B incohérent.")
    reconstruction_pixels, reconstruction_mode, reconstruction_size = load_pixels(reconstruction_path)
    reconstruction_file_difference = pixel_difference_count(reconstruction_pixels, master)
    if reconstruction_mode != "RGB" or reconstruction_size != (2560, 2560) or reconstruction_file_difference:
        raise RealIngestError("Reconstruction UI-B enregistrée non pixel-identique.")

    hashes_index = read_json(hashes_path)
    indexed_files = hashes_index.get("files")
    if not isinstance(indexed_files, dict):
        raise RealIngestError("Index SHA-256 UI-B invalide.")
    hash_index_mismatches: list[str] = []
    for relative, declared in sorted(indexed_files.items()):
        file_path = (ui_dir / relative).resolve()
        if not file_path.is_relative_to(ui_dir) or not file_path.is_file():
            hash_index_mismatches.append(relative)
            continue
        if sha256_file(file_path) != str(declared).lower():
            hash_index_mismatches.append(relative)
    if hash_index_mismatches:
        raise RealIngestError(f"Index SHA-256 UI-B incohérent: {hash_index_mismatches}")

    mechanical = read_json(mechanical_path)
    mechanical_checks = mechanical.get("checks")
    boolean_check_failures = [
        key for key, value in (mechanical_checks or {}).items() if isinstance(value, bool) and value is not True
    ]
    if (
        mechanical.get("mechanical_verdict") != "PASS"
        or not isinstance(mechanical_checks, dict)
        or boolean_check_failures
        or mechanical.get("seam_gradient_diagnostics", {}).get("count") != 40
    ):
        raise RealIngestError("Validation mécanique UI-B non cohérente avec un PASS 40 coutures.")
    required_true_checks = (
        "master_dimensions_2560x2560",
        "master_mode_rgb",
        "tile_count_25",
        "all_tiles_512x512",
        "all_tile_hashes_unique",
        "unique_internal_adjacencies_40",
        "neighbors_reciprocal",
        "reconstruction_pixel_identical",
        "master_sha256_equals_reconstruction_sha256",
        "coverage_exact_no_hole_no_overlap",
    )
    if not all(mechanical_checks.get(key) is True for key in required_true_checks):
        raise RealIngestError("Un check mécanique UI-B obligatoire n'est pas true.")

    perceptual = read_json(perceptual_path)
    if (
        str(perceptual.get("master_sha256", "")).lower() != expected_hash
        or perceptual.get("signed_verdict") != "PASS"
        or perceptual.get("gates", {}).get("WORLD_MAP_WAVE3_CONTINUOUS_MACRO_MASTER") != "PASS"
        or perceptual.get("gates", {}).get("WORLD_MAP_PERCEPTUAL_CONTINUITY_UIB") != "PASS"
        or perceptual.get("gates", {}).get("WORLD_MAP_GRID_PATTERN_VISIBLE") != "NO"
    ):
        raise RealIngestError("Revue perceptuelle UI-B incohérente.")

    report_relative = manifest.get("report")
    report_path = ui_dir / str(report_relative or "")
    if not report_path.is_file():
        raise RealIngestError("Rapport UI-B déclaré absent.")
    report_text = report_path.read_text(encoding="utf-8-sig").lower()
    if expected_hash not in report_text or "world_map_wave3_continuous_macro_master = pass" not in report_text:
        raise RealIngestError("Rapport UI-B ne contient pas le hash/verdict attendu.")

    return (
        {
            "hash_index_entries_checked": len(indexed_files),
            "hash_index_mismatches": hash_index_mismatches,
            "manifest_schema": manifest["schema"],
            "master_hash": master_hash,
            "master_mode": master_mode,
            "master_size": list(master_size),
            "mechanical_seams": 40,
            "mechanical_verdict": mechanical["mechanical_verdict"],
            "perceptual_verdict": perceptual["signed_verdict"],
            "reconstruction_file_pixel_difference_count": reconstruction_file_difference,
            "reconstruction_memory_pixel_difference_count": reconstructed_difference,
            "report_file": str(report_relative),
            "tile_count": len(entries),
            "tile_hashes_unique": len(set(ui_tile_hashes.values())),
            "tile_pixel_difference_count_total": sum(tile_pixel_differences.values()),
        },
        master,
    )


def tree_hashes(root: Path) -> dict[str, str]:
    return {
        path.relative_to(root).as_posix(): sha256_file(path)
        for path in sorted(root.rglob("*"))
        if path.is_file()
    }


def tree_digest(hashes: dict[str, str]) -> str:
    payload = "".join(f"{relative}\0{digest}\n" for relative, digest in sorted(hashes.items()))
    return hashlib.sha256(payload.encode("utf-8")).hexdigest()


def issue_codes(result: dict[str, Any]) -> set[str]:
    return {str(issue.get("code")) for issue in result.get("issues", [])}


def run_negative_case(
    name: str,
    master_path: Path,
    bundle: Path,
    expected_codes: set[str],
    mutate: Callable[[], Callable[[], None]],
) -> dict[str, Any]:
    restore = mutate()
    try:
        rejected = verify_bundle(master_path, bundle)
        codes = issue_codes(rejected)
    finally:
        restore()
    restored = verify_bundle(master_path, bundle)
    passed = rejected["status"] == "FAIL" and expected_codes.issubset(codes) and restored["status"] == "PASS"
    if not passed:
        raise RealIngestError(
            f"Injection négative {name} non concluante: status={rejected['status']}, "
            f"codes={sorted(codes)}, restored={restored['status']}"
        )
    return {
        "expected_issue_codes": sorted(expected_codes),
        "observed_issue_codes": sorted(codes),
        "rejected_status": rejected["status"],
        "restored_status": restored["status"],
    }


def compare_generated_to_ui(ui_dir: Path, run_dir: Path, master: np.ndarray) -> dict[str, Any]:
    canonical_difference = 0
    runtime_interior_difference = 0
    canonical_file_hash_equal_count = 0
    for index, tile_id in enumerate(EXPECTED_IDS):
        row, column = divmod(index, 5)
        ui_path = ui_dir / "tiles" / f"{tile_id}.png"
        generated_path = run_dir / "canonical" / "tiles" / f"{tile_id}.png"
        runtime_path = run_dir / "runtime" / "tiles" / f"{tile_id}_g{GUTTER}.png"
        ui_pixels, _, _ = load_pixels(ui_path)
        generated_pixels, _, _ = load_pixels(generated_path)
        runtime_pixels, _, _ = load_pixels(runtime_path)
        canonical_difference += pixel_difference_count(generated_pixels, ui_pixels)
        runtime_interior_difference += pixel_difference_count(
            runtime_pixels[GUTTER : GUTTER + 512, GUTTER : GUTTER + 512, :], ui_pixels
        )
        if sha256_file(ui_path) == sha256_file(generated_path):
            canonical_file_hash_equal_count += 1
        expected_crop = master[row * 512 : (row + 1) * 512, column * 512 : (column + 1) * 512, :]
        if pixel_difference_count(generated_pixels, expected_crop):
            raise RealIngestError(f"Crop généré incohérent avec le master: {tile_id}")

    runtime_manifest = read_json(run_dir / "runtime" / "manifest.runtime.json")
    entries = runtime_manifest.get("tiles", [])
    uv_min = 2 / 516
    uv_max = 514 / 516
    uv_exact_count = 0
    internal_true_neighbor_sides = 0
    outer_clamp_sides = 0
    invalid_internal_clamp_sides = 0
    for entry in entries:
        row = int(entry["row"])
        column = int(entry["column"])
        uv = entry["uv_inner_normalized"]
        if uv == {"u_max": uv_max, "u_min": uv_min, "v_max": uv_max, "v_min": uv_min}:
            uv_exact_count += 1
        for side, is_outer in {
            "top": row == 0,
            "bottom": row == 4,
            "left": column == 0,
            "right": column == 4,
        }.items():
            clamp = entry["clamp_pixels"][side]
            provenance = entry["gutter_provenance"][side]
            if is_outer:
                if clamp == 2 and provenance == "outer_edge_clamp":
                    outer_clamp_sides += 1
            else:
                if clamp == 0 and provenance == "true_master_neighbor_pixels":
                    internal_true_neighbor_sides += 1
                else:
                    invalid_internal_clamp_sides += 1
    return {
        "canonical_file_hash_equal_to_uib_count": canonical_file_hash_equal_count,
        "canonical_pixel_difference_from_uib_total": canonical_difference,
        "internal_true_neighbor_sides": internal_true_neighbor_sides,
        "invalid_internal_clamp_sides": invalid_internal_clamp_sides,
        "outer_clamp_sides": outer_clamp_sides,
        "runtime_interior_pixel_difference_from_uib_total": runtime_interior_difference,
        "uv_exact_count": uv_exact_count,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Ingest réel UI-B du macro master World Map Wave 3.")
    parser.add_argument("--ui-dir", required=True, type=Path)
    parser.add_argument("--staging-root", required=True, type=Path)
    parser.add_argument("--expected-hash", required=True)
    parser.add_argument("--version", required=True)
    args = parser.parse_args()

    ui_dir = args.ui_dir.resolve()
    staging_root = args.staging_root.resolve()
    master_path = ui_dir / "master_5x5_2560.png"
    preflight, master = preflight_ui_bundle(ui_dir, args.expected_hash)
    run1 = staging_root / "run1"
    run2 = staging_root / "run2"
    for run in (run1, run2):
        if run.exists() and any(run.iterdir()):
            raise RealIngestError(f"Le dossier doit être absent ou vide pour un ingest depuis zéro: {run}")
    staging_root.mkdir(parents=True, exist_ok=True)

    slice_result1 = slice_master(master_path, run1, args.version)
    slice_result2 = slice_master(master_path, run2, args.version)
    verify1 = verify_bundle(master_path, run1)
    verify2 = verify_bundle(master_path, run2)
    if verify1["status"] != "PASS" or verify2["status"] != "PASS":
        raise RealIngestError("Verify explicite run1/run2 non PASS.")

    generated_vs_ui_run1 = compare_generated_to_ui(ui_dir, run1, master)
    generated_vs_ui_run2 = compare_generated_to_ui(ui_dir, run2, master)
    for comparison in (generated_vs_ui_run1, generated_vs_ui_run2):
        if (
            comparison["canonical_pixel_difference_from_uib_total"] != 0
            or comparison["runtime_interior_pixel_difference_from_uib_total"] != 0
            or comparison["uv_exact_count"] != 25
            or comparison["internal_true_neighbor_sides"] != 80
            or comparison["outer_clamp_sides"] != 20
            or comparison["invalid_internal_clamp_sides"] != 0
        ):
            raise RealIngestError(f"Comparaison UI-B/runtime non conforme: {comparison}")

    negative: dict[str, Any] = {}

    canonical_r0c0 = run2 / "canonical" / "tiles" / "R0C0.png"

    def mutate_missing() -> Callable[[], None]:
        payload = canonical_r0c0.read_bytes()
        canonical_r0c0.unlink()
        return lambda: canonical_r0c0.write_bytes(payload)

    negative["missing_tile"] = run_negative_case(
        "missing_tile", master_path, run2, {"MISSING_CANONICAL_TILE"}, mutate_missing
    )

    duplicate_target = run2 / "canonical" / "tiles" / "R0C1.png"

    def mutate_duplicate() -> Callable[[], None]:
        payload = duplicate_target.read_bytes()
        duplicate_target.write_bytes(canonical_r0c0.read_bytes())
        return lambda: duplicate_target.write_bytes(payload)

    negative["duplicate_tile"] = run_negative_case(
        "duplicate_tile",
        master_path,
        run2,
        {"DUPLICATE_CANONICAL_TILE", "CANONICAL_PIXEL_ALTERATION"},
        mutate_duplicate,
    )

    canonical_manifest_path = run2 / "canonical" / "manifest.canonical.json"

    def mutate_hash() -> Callable[[], None]:
        payload = canonical_manifest_path.read_bytes()
        manifest = json.loads(payload.decode("utf-8"))
        manifest["tiles"][0]["png_sha256"] = "0" * 64
        write_json(canonical_manifest_path, manifest)
        return lambda: canonical_manifest_path.write_bytes(payload)

    negative["manifest_hash"] = run_negative_case(
        "manifest_hash", master_path, run2, {"CANONICAL_HASH_MISMATCH"}, mutate_hash
    )

    def mutate_order() -> Callable[[], None]:
        payload = canonical_manifest_path.read_bytes()
        manifest = json.loads(payload.decode("utf-8"))
        manifest["tiles"][0], manifest["tiles"][1] = manifest["tiles"][1], manifest["tiles"][0]
        write_json(canonical_manifest_path, manifest)
        return lambda: canonical_manifest_path.write_bytes(payload)

    negative["manifest_order"] = run_negative_case(
        "manifest_order", master_path, run2, {"CANONICAL_ORDER_MISMATCH"}, mutate_order
    )

    runtime_manifest_path = run2 / "runtime" / "manifest.runtime.json"

    def mutate_gutter_contract() -> Callable[[], None]:
        payload = runtime_manifest_path.read_bytes()
        manifest = json.loads(payload.decode("utf-8"))
        manifest["gutter"]["stretching"] = True
        write_json(runtime_manifest_path, manifest)
        return lambda: runtime_manifest_path.write_bytes(payload)

    negative["gutter_contract"] = run_negative_case(
        "gutter_contract",
        master_path,
        run2,
        {"RUNTIME_GUTTER_CONTRACT_MISMATCH"},
        mutate_gutter_contract,
    )

    runtime_r2c2 = run2 / "runtime" / "tiles" / "R2C2_g2.png"

    def mutate_runtime_pixels() -> Callable[[], None]:
        payload = runtime_r2c2.read_bytes()
        pixels, _, _ = load_pixels(runtime_r2c2)
        pixels[0, 0, 0] ^= 0xFF
        pixels[GUTTER + 7, GUTTER + 11, 1] ^= 0x7F
        image = Image.fromarray(pixels)
        image.save(runtime_r2c2, format="PNG", compress_level=9, optimize=False)
        image.close()
        return lambda: runtime_r2c2.write_bytes(payload)

    negative["runtime_gutter_and_interior_pixels"] = run_negative_case(
        "runtime_gutter_and_interior_pixels",
        master_path,
        run2,
        {"RUNTIME_HASH_MISMATCH", "RUNTIME_PIXEL_ALTERATION", "INTERNAL_GUTTER_BOUNDARY_FAILURE"},
        mutate_runtime_pixels,
    )

    final_verify1 = verify_bundle(master_path, run1)
    final_verify2 = verify_bundle(master_path, run2)
    if final_verify1["status"] != "PASS" or final_verify2["status"] != "PASS":
        raise RealIngestError("Verify final après restauration non PASS.")

    hashes1 = tree_hashes(run1)
    hashes2 = tree_hashes(run2)
    missing_in_run1 = sorted(set(hashes2) - set(hashes1))
    extra_in_run1 = sorted(set(hashes1) - set(hashes2))
    different = sorted(path for path in set(hashes1) & set(hashes2) if hashes1[path] != hashes2[path])
    byte_identical = not missing_in_run1 and not extra_in_run1 and not different
    if not byte_identical:
        raise RealIngestError("run1/run2 non byte-identiques après restauration.")

    summary = {
        "bundle_claims": {
            "immense_live_world_delivered": False,
            "local_runtime_bundle_ready_for_validation": True,
            "unity_integration_done": False,
        },
        "canonical": {
            "reconstruction_pixel_difference_count_run1": final_verify1["canonical"][
                "reconstruction_pixel_difference_count"
            ],
            "reconstruction_pixel_difference_count_run2": final_verify2["canonical"][
                "reconstruction_pixel_difference_count"
            ],
            "tile_count_run1": final_verify1["canonical"]["tile_count_actual"],
            "tile_count_run2": final_verify2["canonical"]["tile_count_actual"],
        },
        "expected_source_sha256": args.expected_hash.lower(),
        "generated_vs_uib": {
            "run1": generated_vs_ui_run1,
            "run2": generated_vs_ui_run2,
        },
        "negative_injections": negative,
        "preflight_ui_b": preflight,
        "run_comparison": {
            "byte_identical": byte_identical,
            "different_files": different,
            "extra_in_run1": extra_in_run1,
            "file_count_run1": len(hashes1),
            "file_count_run2": len(hashes2),
            "missing_in_run1": missing_in_run1,
            "tree_digest_run1": tree_digest(hashes1),
            "tree_digest_run2": tree_digest(hashes2),
        },
        "runtime": {
            "boundaries_checked_run1": final_verify1["runtime"]["internal_boundaries_checked"],
            "boundaries_checked_run2": final_verify2["runtime"]["internal_boundaries_checked"],
            "boundaries_passed_run1": final_verify1["runtime"]["internal_boundaries_passed"],
            "boundaries_passed_run2": final_verify2["runtime"]["internal_boundaries_passed"],
            "gutter_mismatch_pixel_count_run1": final_verify1["runtime"]["gutter_mismatch_pixel_count"],
            "gutter_mismatch_pixel_count_run2": final_verify2["runtime"]["gutter_mismatch_pixel_count"],
            "interior_mismatch_pixel_count_run1": final_verify1["runtime"]["interior_mismatch_pixel_count"],
            "interior_mismatch_pixel_count_run2": final_verify2["runtime"]["interior_mismatch_pixel_count"],
            "tile_count_run1": final_verify1["runtime"]["tile_count_actual"],
            "tile_count_run2": final_verify2["runtime"]["tile_count_actual"],
        },
        "schema": "bee-kingdom.world-map-wave3-real-ingest-summary.v1",
        "slice_verdicts": {"run1": slice_result1["verdicts"], "run2": slice_result2["verdicts"]},
        "source_sha256": preflight["master_hash"],
        "verify_verdicts": {"run1": final_verify1["verdicts"], "run2": final_verify2["verdicts"]},
        "version": args.version,
        "verdicts": {
            "READY_FOR_BUILDERC_RUNTIME_GUTTER_VALIDATION": "YES",
            "REAL_CANONICAL_RECONSTRUCTION_PIXEL_IDENTICAL": "YES",
            "REAL_MASTER_HASH_MATCH": "YES",
            "REAL_RUN1_RUN2_BYTE_IDENTICAL": "YES",
            "REAL_RUNTIME_GUTTERS_40_OF_40": "PASS",
            "WORLD_MAP_WAVE3_REAL_MASTER_INGEST": "PASS",
        },
    }
    write_json(staging_root / "real_ingest_summary.json", summary)
    write_json(staging_root / "verify_run1.json", final_verify1)
    write_json(staging_root / "verify_run2.json", final_verify2)
    print(json.dumps(summary["verdicts"], indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
