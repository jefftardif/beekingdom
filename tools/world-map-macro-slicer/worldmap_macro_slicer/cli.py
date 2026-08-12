from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Sequence

from .core import MacroSlicerError, slice_master, verify_bundle


def _print_verdicts(result: dict) -> None:
    verdicts = result["verdicts"]
    print(f"WORLD_MAP_MACRO_SLICER_WAVE3 = {verdicts['WORLD_MAP_MACRO_SLICER_WAVE3']}")
    print(
        "CANONICAL_RECONSTRUCTION_PIXEL_IDENTICAL = "
        f"{verdicts['CANONICAL_RECONSTRUCTION_PIXEL_IDENTICAL']}"
    )
    print(
        "RUNTIME_GUTTERS_FROM_TRUE_NEIGHBORS = "
        f"{verdicts['RUNTIME_GUTTERS_FROM_TRUE_NEIGHBORS']}"
    )
    print(
        "READY_FOR_UIB_WAVE3_MASTER_INGEST = "
        f"{verdicts['READY_FOR_UIB_WAVE3_MASTER_INGEST']}"
    )


def _failure_result(code: str, message: str) -> dict:
    return {
        "issues": [{"code": code, "message": message, "scope": "pipeline"}],
        "status": "FAIL",
        "verdicts": {
            "CANONICAL_RECONSTRUCTION_PIXEL_IDENTICAL": "NO",
            "READY_FOR_UIB_WAVE3_MASTER_INGEST": "NO",
            "RUNTIME_GUTTERS_FROM_TRUE_NEIGHBORS": "NO",
            "WORLD_MAP_MACRO_SLICER_WAVE3": "FAIL",
        },
    }


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Découpe et vérifie un macro master Bee Kingdom 2560x2560 hors Unity."
    )
    subparsers = parser.add_subparsers(dest="command", required=True)

    slice_parser = subparsers.add_parser("slice", help="Génère les bundles canonique et runtime.")
    slice_parser.add_argument("--input", required=True, type=Path, help="Master PNG 2560x2560 RGB/RGBA.")
    slice_parser.add_argument("--output", required=True, type=Path, help="Dossier absent ou vide.")
    slice_parser.add_argument("--version", default="wave3", help="Version déterministe du lot.")
    slice_parser.add_argument("--json", action="store_true", help="Affiche aussi la validation JSON.")

    verify_parser = subparsers.add_parser("verify", help="Vérifie un bundle sans le modifier.")
    verify_parser.add_argument("--input", required=True, type=Path, help="Master PNG d'origine.")
    verify_parser.add_argument("--bundle", required=True, type=Path, help="Bundle à contrôler.")
    verify_parser.add_argument("--json", action="store_true", help="Affiche la validation JSON.")
    return parser


def main(argv: Sequence[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        if args.command == "slice":
            result = slice_master(args.input, args.output, args.version)
            print(f"Bundle canonique : {args.output.resolve() / 'canonical'}")
            print(f"Bundle runtime : {args.output.resolve() / 'runtime'}")
            print(f"Validation : {args.output.resolve() / 'validation.json'}")
        else:
            result = verify_bundle(args.input, args.bundle)
    except MacroSlicerError as exc:
        result = _failure_result(exc.code, str(exc))
        print(f"{exc.code}: {exc}")
    except Exception as exc:  # Frontière CLI: transforme toute panne en verdict explicite.
        result = _failure_result("UNEXPECTED_ERROR", f"{type(exc).__name__}: {exc}")
        print(result["issues"][0]["message"])

    if args.json:
        print(json.dumps(result, indent=2, sort_keys=True, ensure_ascii=False))
    _print_verdicts(result)
    return 0 if result["status"] == "PASS" else 2
