from __future__ import annotations

import hashlib
import json
import math
from datetime import datetime
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(r"C:\projets\beekingdomgame-master")
OUT = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging" / "production_v3ae_existing_hd_source_audit"

CANDIDATES = {
    "V3D_8192": ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging" / "production_v3d_highres_worker" / "v3d_highres_prototype_8192.png",
    "V3E_8192": ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging" / "production_v3e_reduced_candidate_package" / "v3e_reduced_candidate_8192.png",
    "V3H_8192": ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging" / "production_v3h_global_filtered_tile_package" / "v3h_global_filtered_source_8192.png",
}

REFERENCE = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging" / "production_v3t_actual_hd_panel_proof" / "proof" / "v3t_actual_hd_panel_proof_sheet.png"


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest().upper()


def crop_points(size: tuple[int, int], crop: int = 512) -> list[tuple[str, int, int]]:
    w, h = size
    positions = [
        ("NW", 0.05, 0.05),
        ("N", 0.50, 0.08),
        ("NE", 0.90, 0.08),
        ("W", 0.10, 0.50),
        ("CENTER", 0.50, 0.50),
        ("E", 0.90, 0.50),
        ("SW", 0.10, 0.90),
        ("S", 0.50, 0.90),
        ("SE", 0.90, 0.90),
    ]
    out = []
    for label, fx, fy in positions:
        x = min(max(0, int(w * fx - crop / 2)), w - crop)
        y = min(max(0, int(h * fy - crop / 2)), h - crop)
        out.append((label, x, y))
    return out


def make_sheet(items: list[tuple[str, Path]], out: Path, cell: int = 256) -> None:
    pad = 24
    cols = 9
    rows = math.ceil(len(items) / cols)
    sheet = Image.new("RGB", (cols * cell + (cols + 1) * pad, rows * (cell + 44) + pad), (14, 18, 19))
    draw = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype("arial.ttf", 16)
    except Exception:
        font = ImageFont.load_default()
    for idx, (label, path) in enumerate(items):
        row, col = divmod(idx, cols)
        x = pad + col * (cell + pad)
        y = pad + row * (cell + 44)
        im = Image.open(path).convert("RGB").resize((cell, cell), Image.Resampling.LANCZOS)
        sheet.paste(im, (x, y + 22))
        draw.text((x, y), label, fill=(235, 222, 165), font=font)
        draw.rectangle((x, y + 22, x + cell - 1, y + 22 + cell - 1), outline=(215, 179, 53), width=2)
    sheet.save(out)


def main() -> None:
    crops_dir = OUT / "crops_512"
    proof_dir = OUT / "proof"
    for d in (crops_dir, proof_dir):
        d.mkdir(parents=True, exist_ok=True)

    items: list[tuple[str, Path]] = []
    manifest = {
        "artifact": "V3AE_EXISTING_HD_SOURCE_AUDIT",
        "created_at": datetime.now().isoformat(timespec="seconds"),
        "folder": str(OUT),
        "candidates": {},
        "reference": str(REFERENCE),
        "manual_verdict": "PENDING_VISUAL_REVIEW",
    }

    for name, path in CANDIDATES.items():
        with Image.open(path) as im:
            size = im.size
            manifest["candidates"][name] = {
                "path": str(path),
                "size": list(size),
                "sha256": sha256(path),
            }
            for label, x, y in crop_points(size):
                crop_path = crops_dir / f"{name}_{label}_512.png"
                im.crop((x, y, x + 512, y + 512)).save(crop_path)
                items.append((f"{name} {label}", crop_path))

    proof = proof_dir / "v3ae_existing_hd_source_audit_sheet.png"
    make_sheet(items, proof)

    receipt = {
        **manifest,
        "proof_sheet": str(proof),
        "gates": {
            "ACTIVE_WORK_RESUMED": "YES",
            "V3AE_HD_SOURCES_FOUND": "YES",
            "V3AE_CROP_AUDIT_CREATED": "YES",
            "V3AE_PREMIUM_VISUAL_PASS": "PENDING_VISUAL_REVIEW",
            "V3AE_SELECTED_FOR_UNITY_PACKAGE": "NO",
            "READY_FOR_QA_BUILDERC": "NO",
            "READY_FOR_UNITY_HANDOFF": "NO",
        },
        "hashes": {"proof_sha256": sha256(proof)},
    }
    receipt_path = OUT / "V3AE_EXISTING_HD_SOURCE_AUDIT_RECEIPT.json"
    receipt_path.write_text(json.dumps(receipt, indent=2), encoding="utf-8")

    checkpoint = OUT / "V3AE_EXISTING_HD_SOURCE_AUDIT_CHECKPOINT.md"
    checkpoint.write_text(
        "\n".join(
            [
                "# V3AE Existing HD Source Audit",
                "",
                f"Created: {datetime.now().isoformat(timespec='seconds')}",
                "",
                "Audited existing 8192 sources V3D, V3E, and V3H using consistent 512px crops.",
                "",
                f"Proof sheet: `{proof}`",
                f"Receipt: `{receipt_path}`",
                "",
                "No candidate is authorized for Unity until perceptual review is written into the receipt.",
            ]
        ),
        encoding="utf-8",
    )

    print(json.dumps({"receipt": str(receipt_path), "proof": str(proof)}, indent=2))


if __name__ == "__main__":
    main()
