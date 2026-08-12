#!/usr/bin/env python3
"""Exercise gutter corruption detection in a disposable QA copy."""

from __future__ import annotations

import argparse
import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--master", required=True, type=Path)
    parser.add_argument("--source-run", required=True, type=Path)
    parser.add_argument("--ui-tiles", required=True, type=Path)
    parser.add_argument("--auditor", required=True, type=Path)
    parser.add_argument("--producer-verifier", required=True, type=Path)
    parser.add_argument("--artifact-root", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    return parser.parse_args()


def run_command(command: list[str]) -> subprocess.CompletedProcess[str]:
    return subprocess.run(command, check=False, capture_output=True, text=True)


def main() -> int:
    args = parse_args()
    artifact_root = args.artifact_root.resolve()
    artifact_root.mkdir(parents=True, exist_ok=True)
    args.output.parent.mkdir(parents=True, exist_ok=True)

    result: dict[str, object] = {
        "schema": "bee-kingdom.builder-c-wave3-runtime-negative-sample.v1",
        "status": "FAIL",
        "source_run": str(args.source_run.resolve()),
        "mutation": {},
        "independent_auditor": {},
        "producer_verifier": {},
        "qa_copy_removed": False,
    }

    with tempfile.TemporaryDirectory(prefix="negative_qa_", dir=artifact_root) as temp_name:
        temp_root = Path(temp_name).resolve()
        if artifact_root not in temp_root.parents:
            raise RuntimeError(f"Temporary QA path escaped artifact root: {temp_root}")

        qa_bundle = temp_root / "bundle"
        shutil.copytree(args.source_run.resolve(), qa_bundle)
        target = qa_bundle / "runtime" / "tiles" / "R2C2_g2.png"
        with Image.open(target) as image:
            pixels = image.convert("RGB")
            x, y = 0, 258
            before = pixels.getpixel((x, y))
            after = ((before[0] + 1) % 256, before[1], before[2])
            pixels.putpixel((x, y), after)
            pixels.save(target, format="PNG")

        result["mutation"] = {
            "file": "runtime/tiles/R2C2_g2.png",
            "coordinate": [x, y],
            "region": "left_gutter",
            "before": list(before),
            "after": list(after),
            "source_bundle_modified": False,
        }

        negative_audit_path = temp_root / "negative_audit.json"
        independent = run_command(
            [
                sys.executable,
                str(args.auditor.resolve()),
                "--master",
                str(args.master.resolve()),
                "--run1",
                str(qa_bundle),
                "--ui-tiles",
                str(args.ui_tiles.resolve()),
                "--output",
                str(negative_audit_path),
            ]
        )
        independent_payload = (
            json.loads(negative_audit_path.read_text(encoding="utf-8"))
            if negative_audit_path.exists()
            else {}
        )
        issue_codes = sorted(
            {
                issue.get("code", "")
                for issue in independent_payload.get("run1", {}).get("issues", [])
                if issue.get("code")
            }
        )
        result["independent_auditor"] = {
            "exit_code": independent.returncode,
            "status": independent_payload.get("status"),
            "issue_codes": issue_codes,
            "rejected": independent.returncode != 0
            and independent_payload.get("status") == "FAIL"
            and "RUNTIME_PIXEL_ALTERATION" in issue_codes,
        }

        producer = run_command(
            [
                sys.executable,
                str(args.producer_verifier.resolve()),
                "verify",
                "--input",
                str(args.master.resolve()),
                "--bundle",
                str(qa_bundle),
                "--json",
            ]
        )
        result["producer_verifier"] = {
            "exit_code": producer.returncode,
            "reported_fail": "WORLD_MAP_MACRO_SLICER_WAVE3 = FAIL" in producer.stdout,
            "rejected": producer.returncode != 0,
        }

        passed = bool(
            result["independent_auditor"]["rejected"]
            and result["producer_verifier"]["rejected"]
        )
        result["status"] = "PASS" if passed else "FAIL"

    result["qa_copy_removed"] = not any(artifact_root.glob("negative_qa_*"))
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(result, indent=2))
    return 0 if result["status"] == "PASS" and result["qa_copy_removed"] else 2


if __name__ == "__main__":
    raise SystemExit(main())
