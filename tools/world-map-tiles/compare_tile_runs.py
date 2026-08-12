#!/usr/bin/env python
"""Compare two tile pipeline runs by relative file SHA-256."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def collect_files(root: Path) -> list[str]:
    files = [
        "manifest.json",
        "validation/validation.json",
        "validation/reconstruction.png",
        "validation/contact_sheet.png",
    ]
    files.extend(sorted(path.relative_to(root).as_posix() for path in (root / "tiles").glob("*.png")))
    return files


def main() -> None:
    parser = argparse.ArgumentParser(description="Compare deterministic outputs from two tile pipeline runs.")
    parser.add_argument("--run-a", required=True)
    parser.add_argument("--run-b", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()

    run_a = Path(args.run_a).resolve()
    run_b = Path(args.run_b).resolve()
    files = collect_files(run_a)
    rows = []
    mismatches = []

    for rel in files:
        path_a = run_a / rel
        path_b = run_b / rel
        hash_a = sha256_file(path_a)
        hash_b = sha256_file(path_b)
        match = hash_a == hash_b
        if not match:
            mismatches.append(rel)
        rows.append(
            {
                "file": rel,
                "run_a_sha256": hash_a,
                "run_b_sha256": hash_b,
                "match": match,
            }
        )

    result = {
        "run_a": str(run_a).replace("\\", "/"),
        "run_b": str(run_b).replace("\\", "/"),
        "files_compared": len(rows),
        "all_hashes_match": len(mismatches) == 0,
        "mismatches": mismatches,
        "rows": rows,
    }
    Path(args.output).write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    if mismatches:
        raise SystemExit("Determinism comparison failed.")


if __name__ == "__main__":
    main()
