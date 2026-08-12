from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Sequence

from .core import ValidationOptions, validate_content


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description=(
            "Valide un lot d'images de secteurs Bee Kingdom et genere des preuves "
            "machine-readable et visuelles."
        )
    )
    parser.add_argument("--input", required=True, type=Path, help="Dossier du lot a inspecter.")
    parser.add_argument("--output", required=True, type=Path, help="Dossier de sortie des preuves.")
    parser.add_argument("--manifest", type=Path, help="Manifest explicite; sinon manifest.json est detecte.")
    parser.add_argument("--thresholds", type=Path, help="JSON surchargeant les seuils par defaut.")
    parser.add_argument("--expected-count", type=int, help="Nombre d'images attendu.")
    parser.add_argument("--columns", type=int, help="Nombre de colonnes attendu.")
    parser.add_argument("--rows", type=int, help="Nombre de lignes attendu.")
    parser.add_argument("--label", default="lot-sans-label", help="Etiquette libre incluse dans les rapports.")
    parser.add_argument(
        "--profile",
        choices=("wave2-5x5", "wave3-continuous-5x5"),
        help=(
            "Profil strict Wave2 historique ou Wave3: master continu 2560x2560, "
            "25 slices 512x512, 40 frontieres et gutters runtime 516x516."
        ),
    )
    parser.add_argument("--reference-atlas", type=Path, help="Atlas pixel de reference pour la reconstruction.")
    parser.add_argument("--baseline-center", type=Path, help="Dossier Wave1 3x3 dont les hashes centraux sont verrouilles.")
    parser.add_argument("--baseline-manifest", type=Path, help="Manifest explicite de la baseline centrale.")
    parser.add_argument("--expected-new-ring-count", type=int, help="Nombre exact de tuiles du nouvel anneau.")
    parser.add_argument("--expected-seam-count", type=int, help="Nombre exact de coutures internes attendu.")
    parser.add_argument("--required-tile-width", type=int, help="Largeur exacte requise pour chaque tuile.")
    parser.add_argument("--required-tile-height", type=int, help="Hauteur exacte requise pour chaque tuile.")
    parser.add_argument("--forbidden-review", type=Path, help="JSON de revue humaine des contenus peints interdits.")
    parser.add_argument(
        "--require-forbidden-review",
        action="store_true",
        help="Classe WARN/FAIL tant que les six categories semantiques ne sont pas revues ABSENT.",
    )
    parser.add_argument(
        "--perceptual-review",
        type=Path,
        help="JSON de revue humaine multi-echelle de la continuite perceptuelle.",
    )
    parser.add_argument(
        "--require-perceptual-review",
        action="store_true",
        help="Bloque le PASS tant que grille, centre, anneau, motifs et continuites ne sont pas revus NO.",
    )
    parser.add_argument(
        "--gutters-dir",
        type=Path,
        help="Dossier des 25 tuiles runtime avec gutters issus des vrais voisins.",
    )
    parser.add_argument(
        "--gutter-size",
        type=int,
        default=2,
        help="Pixels ajoutes sur chaque cote; 2 produit des tuiles runtime 516x516.",
    )
    parser.add_argument(
        "--readiness-report",
        type=Path,
        help="Rapport UI-B contenant READY_FOR_WORLD_MAP_ART_WAVE3_VALIDATION=YES.",
    )
    parser.add_argument(
        "--fail-on-warn",
        action="store_true",
        help="Retourne aussi un code non nul lorsque le verdict global est WARN.",
    )
    parser.add_argument("--quiet", action="store_true", help="N'affiche que le verdict final.")
    return parser


def main(argv: Sequence[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    is_wave2_5x5 = args.profile == "wave2-5x5"
    is_wave3_5x5 = args.profile == "wave3-continuous-5x5"
    is_strict_5x5 = is_wave2_5x5 or is_wave3_5x5
    options = ValidationOptions(
        input_dir=args.input,
        output_dir=args.output,
        manifest_path=args.manifest,
        thresholds_path=args.thresholds,
        expected_count=args.expected_count if args.expected_count is not None else (25 if is_strict_5x5 else None),
        columns=args.columns if args.columns is not None else (5 if is_strict_5x5 else None),
        rows=args.rows if args.rows is not None else (5 if is_strict_5x5 else None),
        label=args.label,
        profile=args.profile,
        reference_atlas_path=args.reference_atlas,
        baseline_center_dir=args.baseline_center,
        baseline_manifest_path=args.baseline_manifest,
        expected_new_ring_count=(
            args.expected_new_ring_count
            if args.expected_new_ring_count is not None
            else (16 if is_wave2_5x5 else None)
        ),
        expected_seam_count=(
            args.expected_seam_count if args.expected_seam_count is not None else (40 if is_strict_5x5 else None)
        ),
        required_tile_width=(
            args.required_tile_width if args.required_tile_width is not None else (512 if is_strict_5x5 else None)
        ),
        required_tile_height=(
            args.required_tile_height if args.required_tile_height is not None else (512 if is_strict_5x5 else None)
        ),
        forbidden_review_path=args.forbidden_review,
        require_forbidden_review=args.require_forbidden_review or is_strict_5x5,
        perceptual_review_path=args.perceptual_review,
        require_perceptual_review=args.require_perceptual_review or is_strict_5x5,
        require_signed_perceptual_review=is_wave3_5x5,
        gutters_dir=args.gutters_dir,
        gutter_size=args.gutter_size,
        require_gutters=is_wave3_5x5 and args.gutters_dir is not None,
        required_master_width=2560 if is_wave3_5x5 else None,
        required_master_height=2560 if is_wave3_5x5 else None,
        readiness_report_path=args.readiness_report,
        require_wave3_ready_marker=is_wave3_5x5,
    )

    try:
        report = validate_content(options)
    except Exception as exc:  # CLI boundary: keep failures explicit and machine visible.
        args.output.mkdir(parents=True, exist_ok=True)
        failure = {
            "schema": "bee-kingdom.world-map-content-validation.v1",
            "overall_status": "FAIL",
            "fatal_error": f"{type(exc).__name__}: {exc}",
        }
        (args.output / "validation.json").write_text(
            json.dumps(failure, indent=2, ensure_ascii=False) + "\n", encoding="utf-8"
        )
        print(f"FAIL: {failure['fatal_error']}")
        return 2

    if not args.quiet:
        print(f"Rapport JSON : {args.output / 'validation.json'}")
        print(f"Rapport Markdown : {args.output / 'report.md'}")
        print(f"Planche contact : {args.output / 'contact_sheet.png'}")
        print(f"Chaleur coutures : {args.output / 'seam_heatmap.png'}")
        print(f"Grille QA : {args.output / 'qa_grid.png'}")
    print(f"WORLD_MAP_CONTENT_VALIDATION = {report['overall_status']}")

    if report["overall_status"] == "FAIL":
        return 2
    if report["overall_status"] == "WARN" and args.fail_on_warn:
        return 1
    return 0
