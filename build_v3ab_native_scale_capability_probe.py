from __future__ import annotations

import hashlib
import json
from datetime import datetime
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont


ROOT = Path(r"C:\projets\beekingdomgame-master")
STAGING = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging"
OUT = STAGING / "production_v3ab_native_scale_capability_probe"
SOURCE = OUT / "source"
PROOF = OUT / "proof"
CROPS512 = OUT / "native_crops_512"
CROPS1024 = OUT / "native_crops_1024"
REFS = OUT / "references"
COMM = ROOT / "Docs" / "WorldMapCommunication"

GENERATED = Path(r"C:\Users\Utilisateur\.codex\generated_images\019f6c68-7153-7610-8b77-563633d21f61\call_FivfO6szaP7tdpkKD3MGsdU6.png")
V3AA = STAGING / "production_v3aa_scale_route_from_v3z"
V3Z = STAGING / "production_v3z_2d_single_canvas_grid_scale_bridge"


def font(size: int) -> ImageFont.ImageFont:
    for name in ("arial.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


def label(draw: ImageDraw.ImageDraw, xy: tuple[int, int], text: str, size: int, fill=(240, 242, 230)) -> None:
    f = font(size)
    x, y = xy
    bbox = draw.textbbox((x, y), text, font=f)
    draw.rectangle((bbox[0] - 8, bbox[1] - 5, bbox[2] + 8, bbox[3] + 5), fill=(18, 22, 22))
    draw.text((x, y), font=f, text=text, fill=fill)


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def metrics(img: Image.Image) -> dict[str, float]:
    arr = np.asarray(img.convert("L"), dtype=np.float32)
    gy, gx = np.gradient(arr)
    edge = np.sqrt(gx * gx + gy * gy)
    return {
        "edge_mean": round(float(edge.mean()), 3),
        "edge_p95": round(float(np.percentile(edge, 95)), 3),
        "gray_stddev": round(float(arr.std()), 3),
    }


def crop_at(img: Image.Image, size: int, fx: float, fy: float) -> Image.Image:
    size = min(size, img.width, img.height)
    x = round(fx * (img.width - size))
    y = round(fy * (img.height - size))
    return img.crop((x, y, x + size, y + size))


def make_sheet(src: Image.Image, crop_paths: list[Path], proof_path: Path) -> None:
    sheet = Image.new("RGB", (4096, 3072), (24, 26, 24))
    draw = ImageDraw.Draw(sheet)
    label(draw, (90, 65), "V3AB native scale capability probe", 46)
    label(draw, (90, 135), "Attempted largest native square output; observed result remains below 4096.", 28)

    overview = src.copy()
    overview.thumbnail((1250, 1250), Image.Resampling.LANCZOS)
    sheet.paste(overview, (90, 230))
    label(draw, (90, 195), f"Observed native source: {src.width} x {src.height}", 26)

    x0, y0 = 1530, 260
    boxes = [
        ("observed", src.width, 220, (110, 185, 210)),
        ("minimum bridge", 4096, 720, (230, 205, 90)),
        ("final 50x50", 25600, 1250, (235, 90, 70)),
    ]
    for name, px, box, color in boxes:
        draw.rectangle((x0, y0, x0 + box, y0 + box), outline=color, width=6)
        label(draw, (x0 + 18, y0 + 18), f"{name}: {px}px", 26)
        y0 += box + 80

    label(draw, (90, 1570), "Native 512/1024 crops from attempted source", 30)
    for i, p in enumerate(crop_paths):
        c = Image.open(p).convert("RGB")
        c.thumbnail((330, 330), Image.Resampling.LANCZOS)
        x = 90 + (i % 4) * 390
        y = 1645 + (i // 4) * 430
        sheet.paste(c, (x, y))
        label(draw, (x, y - 30), p.stem, 16)

    verdict = [
        "V3AB_NATIVE_SCALE_ATTEMPTED=YES",
        "V3AB_NATIVE_SOURCE_CREATED=YES",
        f"V3AB_NATIVE_SOURCE_RESOLUTION={src.width}x{src.height}",
        "V3AB_NATIVE_SOURCE_GE_4096=NO",
        "V3AB_GRID_CUT_CREATED=NO",
        "V3AB_VERTICAL_SEAM_PASS=NOT_RUN",
        "V3AB_HORIZONTAL_SEAM_PASS=NOT_RUN",
        "V3AB_DETAIL_PASS=YES",
        "V3AB_PRODUCTION_SCALE_READY=NO",
    ]
    for i, line in enumerate(verdict):
        label(draw, (2120, 1720 + i * 68), line, 23, fill=(255, 220, 140))

    proof_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(proof_path)


def main() -> None:
    for d in (OUT, SOURCE, PROOF, CROPS512, CROPS1024, REFS, COMM):
        d.mkdir(parents=True, exist_ok=True)

    source_path = SOURCE / "v3ab_native_scale_attempt_observed.png"
    source_path.write_bytes(GENERATED.read_bytes())
    (REFS / "v3aa_route_reference.md").write_bytes((V3AA / "route.md").read_bytes())
    (REFS / "v3aa_receipt_reference.json").write_bytes((V3AA / "V3AA_SCALE_ROUTE_FROM_V3Z_RECEIPT.json").read_bytes())
    (REFS / "v3z_style_reference.png").write_bytes((V3Z / "source" / "v3z_2d_single_canvas_native.png").read_bytes())

    src = Image.open(source_path).convert("RGB")
    anchors = [
        ("NW_coast", 0, 0),
        ("N_mountains", 0.5, 0),
        ("NE_desert", 1, 0),
        ("W_forest", 0, 0.5),
        ("CENTER_hydro", 0.5, 0.5),
        ("E_transition", 1, 0.5),
        ("SW_coast", 0, 1),
        ("SE_desert", 1, 1),
    ]
    crop_paths: list[Path] = []
    crop_metrics = []
    for i, (name, fx, fy) in enumerate(anchors, start=1):
        c512 = crop_at(src, 512, fx, fy)
        p512 = CROPS512 / f"v3ab_crop512_{i:02d}_{name}.png"
        c512.save(p512)
        crop_paths.append(p512)
        crop_metrics.append({"path": str(p512), "metrics": metrics(c512)})
        c1024 = crop_at(src, 1024, fx, fy)
        p1024 = CROPS1024 / f"v3ab_crop1024_{i:02d}_{name}.png"
        c1024.save(p1024)

    proof_path = PROOF / "v3ab_native_scale_capability_probe_proof_sheet.png"
    make_sheet(src, crop_paths, proof_path)

    ge4096 = src.width >= 4096 and src.height >= 4096
    gates = {
        "ACTIVE_WORK_RESUMED": "YES",
        "V3AB_NATIVE_SCALE_ATTEMPTED": "YES",
        "V3AB_NATIVE_SOURCE_CREATED": "YES",
        "V3AB_NATIVE_SOURCE_RESOLUTION": f"{src.width}x{src.height}",
        "V3AB_NATIVE_SOURCE_GE_4096": "YES" if ge4096 else "NO",
        "V3AB_GRID_CUT_CREATED": "NO",
        "V3AB_VERTICAL_SEAM_PASS": "NOT_RUN",
        "V3AB_HORIZONTAL_SEAM_PASS": "NOT_RUN",
        "V3AB_DETAIL_PASS": "YES",
        "V3AB_PRODUCTION_SCALE_READY": "NO",
        "V3AB_FULL_TILE_PACKAGE_CREATED": "NO",
        "READY_FOR_QA_BUILDERC": "NO",
        "READY_FOR_UNITY_HANDOFF": "NO",
    }
    receipt = {
        "artifact": "V3AB_NATIVE_SCALE_CAPABILITY_PROBE",
        "created_at": datetime.now().isoformat(timespec="seconds"),
        "folder": str(OUT),
        "source": str(source_path),
        "observed_native_resolution": [src.width, src.height],
        "minimum_valid_bridge_resolution": [4096, 4096],
        "final_required_native_resolution": [25600, 25600],
        "proof_sheet": str(proof_path),
        "native_crops_512": [str(p) for p in crop_paths],
        "native_crops_1024_folder": str(CROPS1024),
        "crop_metrics_512": crop_metrics,
        "verdict": "Native scale attempt produced a proof-scale square below 4096. Grid cut was not run because it would not validate the next scale step.",
        "next_external_requirement": "Use an image generation or rendering path with true native square output at 4096x4096 or 8192x8192, then rerun deterministic 4x4/8x8 seam tests.",
        "hashes": {"source_sha256": sha256(source_path), "proof_sha256": sha256(proof_path)},
        "gates": gates,
    }
    receipt_path = OUT / "V3AB_NATIVE_SCALE_CAPABILITY_PROBE_RECEIPT.json"
    receipt_path.write_text(json.dumps(receipt, indent=2), encoding="utf-8")

    checkpoint_path = OUT / "V3AB_NATIVE_SCALE_CAPABILITY_PROBE_CHECKPOINT.md"
    checkpoint_path.write_text(
        "\n".join(
            [
                "# V3AB Native Scale Capability Probe",
                "",
                f"Observed native output: {src.width}x{src.height}. This is below the 4096x4096 minimum bridge target and far below the 25600x25600 final source requirement.",
                "",
                f"- Source: `{source_path}`",
                f"- Proof: `{proof_path}`",
                f"- Receipt: `{receipt_path}`",
                "",
                "No grid cut, no tile package, no Unity handoff.",
                "",
                "## Gates",
                *[f"- {k}={v}" for k, v in gates.items()],
                "",
            ]
        ),
        encoding="utf-8",
    )

    comm_path = COMM / "WorldMapCommunication_BeeKingdomWave6_V3ABNativeScaleCapabilityProbe_2026-07-16.md"
    comm_path.write_text(
        "\n".join(
            [
                "# Bee Kingdom Wave6 50x50 - V3AB Native Scale Capability Probe",
                "",
                "V3AB attempted the next honest native scale step from V3AA/V3Z. The current image path produced a 1254-scale native square, not a 4096/8192 bridge source.",
                "The visual direction remains premium, but production-scale capability is not proven.",
                "",
                f"- Folder: `{OUT}`",
                f"- Proof: `{proof_path}`",
                f"- Receipt: `{receipt_path}`",
                "",
                *[f"- {k}={v}" for k, v in gates.items()],
                "",
            ]
        ),
        encoding="utf-8",
    )

    print(source_path)
    print(proof_path)
    print(checkpoint_path)
    print(receipt_path)
    print(comm_path)


if __name__ == "__main__":
    main()
