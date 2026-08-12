from __future__ import annotations

import argparse
import hashlib
import json
import sys
import tempfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from synthetic_master import create_synthetic_master
from worldmap_macro_slicer.core import slice_master


def _sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def _tree_hashes(root: Path) -> dict[str, str]:
    return {
        path.relative_to(root).as_posix(): _sha256(path)
        for path in sorted(root.rglob("*"))
        if path.is_file()
    }


def _tree_digest(hashes: dict[str, str]) -> str:
    payload = "".join(f"{path}\0{digest}\n" for path, digest in sorted(hashes.items()))
    return hashlib.sha256(payload.encode("utf-8")).hexdigest()


def _write_json(path: Path, data: dict) -> None:
    path.write_text(
        json.dumps(data, indent=2, sort_keys=True, ensure_ascii=False) + "\n",
        encoding="utf-8",
        newline="\n",
    )


def main() -> int:
    parser = argparse.ArgumentParser(description="Génère une preuve compacte déterministe du slicer Wave 3.")
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)

    with tempfile.TemporaryDirectory() as temporary:
        root = Path(temporary)
        master = root / "synthetic_macro_master.png"
        run1 = root / "run1"
        run2 = root / "run2"
        create_synthetic_master(master)
        result1 = slice_master(master, run1, "synthetic-wave3-proof")
        result2 = slice_master(master, run2, "synthetic-wave3-proof")
        hashes1 = _tree_hashes(run1)
        hashes2 = _tree_hashes(run2)
        mismatches = sorted(
            path for path in set(hashes1) | set(hashes2) if hashes1.get(path) != hashes2.get(path)
        )
        summary = {
            "canonical_reconstruction_pixel_difference_count": result1["canonical"][
                "reconstruction_pixel_difference_count"
            ],
            "determinism_mismatch_files": mismatches,
            "features": {
                "diagonal_river_family": True,
                "global_gradients": True,
                "high_frequency_relief": True,
                "internal_boundaries_exercised": 40,
            },
            "output_file_count_each_run": len(hashes1),
            "run1_run2_byte_identical": hashes1 == hashes2,
            "runtime_boundary_gutter_mismatch_pixel_count": result1["runtime"][
                "boundary_gutter_mismatch_pixel_count"
            ],
            "runtime_gutter_mismatch_pixel_count": result1["runtime"]["gutter_mismatch_pixel_count"],
            "runtime_internal_boundaries_checked": result1["runtime"]["internal_boundaries_checked"],
            "runtime_internal_boundaries_passed": result1["runtime"]["internal_boundaries_passed"],
            "schema": "bee-kingdom.world-map-macro-synthetic-proof.v1",
            "source_png_sha256": _sha256(master),
            "tree_digest_run1": _tree_digest(hashes1),
            "tree_digest_run2": _tree_digest(hashes2),
            "verdicts": result1["verdicts"],
        }
        _write_json(output / "synthetic_proof_summary.json", summary)
        (output / "synthetic_validation_snapshot.json").write_bytes((run1 / "validation.json").read_bytes())
        (output / "synthetic_canonical_manifest_snapshot.json").write_bytes(
            (run1 / "canonical" / "manifest.canonical.json").read_bytes()
        )
        (output / "synthetic_runtime_manifest_snapshot.json").write_bytes(
            (run1 / "runtime" / "manifest.runtime.json").read_bytes()
        )

    print(json.dumps(summary, indent=2, sort_keys=True, ensure_ascii=False))
    return 0 if not mismatches and result1["status"] == result2["status"] == "PASS" else 2


if __name__ == "__main__":
    raise SystemExit(main())
