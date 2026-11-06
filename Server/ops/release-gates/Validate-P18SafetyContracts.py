#!/usr/bin/env python3
"""Fail-closed source tripwire for BeeKingdom P18 read-only protections.

This is a conservative *preflight* test, not an alternative to executing API,
SQL, concurrency and authorization tests. It never touches production.
"""
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[3]
checks = {
 "CivilPopulation service has read-only gate": (
  "Server/src/BeeKingdom.HiveOperations/CivilPopulationService.cs",
  ["ReadOnlyPilot"]
 ),
 "Floral alliance project model separates read from create": (
  "Server/src/BeeKingdom.HiveOperations/FloralAllianceProjectModels.cs",
  ["ReadOnlyPilot", "CanRead", "CanCreate"]
 ),
 "API blocks civilian mutations in read-only mode": (
  "Server/src/BeeKingdom.Server/Program.cs",
  ["ReadOnlyPilot", "civil_population_readonly"]
 )
}
missing = []
for label, (relative, needles) in checks.items():
 path = root / relative
 if not path.exists():
  missing.append((label, "file absent: "+relative))
  continue
 code = path.read_text(encoding="utf-8-sig", errors="replace")
 for keyword in needles:
  if keyword not in code:
   missing.append((label, "required source guard missing: "+keyword))
if missing:
 print("P18_READONLY_COMPATIBILITY=NO_GO")
 for label, reason in missing:
  print("BLOCKED: "+label+": "+reason)
 sys.exit(2)
print("P18_READONLY_COMPATIBILITY=SOURCE_TRIPWIRE_PASS")
print("NOTICE: Still require full P18 read-only, SQL, API and gameplay tests.")
