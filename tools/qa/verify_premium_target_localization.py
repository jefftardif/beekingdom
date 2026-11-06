#!/usr/bin/env python3
"""BeeKingdom premium target UI FR/EN parity and safety gate.

Read-only: validates both Unity localization catalogs and the built-in
world-map premium action copy. No gameplay/network writes.
"""
from __future__ import annotations

import argparse
from collections import Counter
import json
from pathlib import Path
import re
import sys


def inspect(root: Path) -> dict:
    catalogs = {}
    for locale in ("fr-CA", "en-US"):
        path = (root / "Assets/_Project/Data/Localization/Resources/"
                     f"Localization/strings.{locale}.json")
        data = json.loads(path.read_text(encoding="utf-8-sig"))
        assert data["locale"] == locale, "Catalog locale mismatch"
        entries = [e for e in data["entries"] if
                   e["key"].startswith("worldmap.ui.target.")]
        counts = Counter(e["key"] for e in entries)
        duplicates = [key for key, count in counts.items() if count != 1]
        if duplicates:
            raise ValueError(f"Duplicate {locale} premium target keys: {duplicates}")
        mapping = {e["key"]: e["value"] for e in entries}
        if not mapping or any(not isinstance(value, str) or not value.strip()
                              for value in mapping.values()):
            raise ValueError(f"Missing or blank {locale} premium translation")
        if any("\ufffd" in value or "Ã©" in value or "Ã¨" in value
               for value in mapping.values()):
            raise ValueError(f"Broken UTF-8 premium target text in {locale}")
        catalogs[locale] = mapping

    french, english = catalogs["fr-CA"], catalogs["en-US"]
    if set(french) != set(english):
        raise ValueError("Premium target keys differ across French/English")
    prefix = "worldmap.ui."
    used = set()
    for relative in (
        "Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.TargetPremium.cs",
        "Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs",
    ):
        source = (root / relative).read_text(encoding="utf-8-sig")
        for key in re.findall(r'MapText\s*\(\s*"(target\.[^"]+)"', source):
            used.add(prefix + key)
    missing = sorted(used - set(french))
    if missing:
        raise ValueError("Missing premium translations: " + ", ".join(missing))
    if len(french) < 65:
        raise ValueError("Unexpectedly incomplete premium target copy")
    must = [
        "target.reason.preview_only",
        "target.action.preview",
        "target.reason.placement_missing",
        "target.reason.server_offline",
        "target.action.prepare_army",
        "target.action.plan_attack",
        "target.action.gather",
        "target.action.claim",
        "target.action.recall",
        "target.bonus.rare",
        "target.bonus.daily",
        "target.metric.remaining",
    ]
    for key in must:
        if prefix + key not in french:
            raise ValueError("Missing critical action/status text: " + key)
    return {
        "schema": "bk-premium-world-target-localization-v1",
        "pass": True,
        "localeCounts": {locale: len(mapping) for locale, mapping in catalogs.items()},
        "referencedTargetKeys": len(used),
        "missingKeys": missing,
        "unpairedKeys": [],
        "unicodeReplacementCharacters": 0,
        "productionActionsUsed": [
            "OpenCombatPatrolOverlayForWorldMap",
            "OpenBarrackArmyTabForExternalHost",
            "TryCollectOrLaunch",
            "ClaimOfficialWorldResourceCollectionForWorldMap",
            "RecallOfficialWorldResourceCollectionForWorldMap",
        ],
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, required=True)
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()
    report = inspect(args.root)
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n",
                               encoding="utf-8")
    print("PREMIUM_TARGET_LOCALES=PASS")
    print("TARGET_KEYS_FR_EN=" + str(report["localeCounts"]["fr-CA"]))
    print("USED_TARGET_KEYS=" + str(report["referencedTargetKeys"]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
