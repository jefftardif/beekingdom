#!/usr/bin/env python3
"""Read-only BeeKingdom Unity metadata/source preflight.

Unity may delete *.meta files when the corresponding ignored art source was
not copied into a git worktree. This tool NEVER edits/restores anything.
Requires only Python 3 and git. UTF-8 JSON can be saved for QA evidence.
"""
from __future__ import annotations

import argparse
import datetime
import json
import os
from pathlib import Path
import subprocess
import sys


def tracked_metas(worktree: Path) -> list[str]:
    completed = subprocess.run(
        ["git", "-C", str(worktree), "ls-files", "-z", "--", "Assets"],
        check=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    return sorted(
        os.fsdecode(item)
        for item in completed.stdout.split(b"\0")
        if item and item.endswith(b".meta")
    )


def inspect(worktree: Path, reference: Path | None) -> dict:
    paths = tracked_metas(worktree)
    result: dict[str, object] = {
        "checked_at_utc": datetime.datetime.now(
            datetime.timezone.utc
        ).isoformat(),
        "worktree": str(worktree),
        "reference": str(reference) if reference else None,
        "tracked_meta_count": len(paths),
        "pairs_present": 0,
        "missing_source_with_meta": [],
        "missing_meta_with_source": [],
        "missing_both_recoverable_from_reference": [],
        "missing_both_without_reference_source": [],
    }
    for meta in paths:
        asset = meta[:-5]
        source = worktree.joinpath(*asset.split("/"))
        meta_path = worktree.joinpath(*meta.split("/"))
        has_source = source.exists()
        has_meta = meta_path.is_file()
        if has_source and has_meta:
            result["pairs_present"] += 1
        elif has_meta:
            result["missing_source_with_meta"].append(meta)
        elif has_source:
            result["missing_meta_with_source"].append(meta)
        elif reference and reference.joinpath(*asset.split("/")).exists():
            result["missing_both_recoverable_from_reference"].append(meta)
        else:
            result["missing_both_without_reference_source"].append(meta)
    result["issues"] = sum(
        len(result[key])
        for key in (
            "missing_source_with_meta",
            "missing_meta_with_source",
            "missing_both_recoverable_from_reference",
            "missing_both_without_reference_source",
        )
    )
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--worktree", required=True, type=Path)
    parser.add_argument("--reference", type=Path)
    parser.add_argument("--json-report", type=Path)
    args = parser.parse_args()

    worktree = args.worktree.resolve()
    reference = args.reference.resolve() if args.reference else None
    if not (worktree / ".git").exists():
        parser.error("--worktree must be a git worktree or repository root.")
    if reference and not (reference / "Assets").is_dir():
        parser.error("--reference must contain the reference Assets directory.")

    report = inspect(worktree, reference)
    if args.json_report:
        args.json_report.parent.mkdir(parents=True, exist_ok=True)
        args.json_report.write_text(
            json.dumps(report, ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8",
        )
    summary = {
        key: (len(value) if isinstance(value, list) else value)
        for key, value in report.items()
        if key not in ("worktree", "reference", "checked_at_utc")
    }
    print(json.dumps(summary, ensure_ascii=False, indent=2))
    # Signal missing artwork/meta without modifying the Unity checkout.
    return 2 if report["issues"] else 0


if __name__ == "__main__":
    sys.exit(main())
