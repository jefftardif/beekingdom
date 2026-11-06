#!/usr/bin/env python3
"""BeeKingdom: fail closed before importing an incomplete Unity Git worktree.

The repository intentionally excludes large binary art and retains .meta GUIDs.
Unity can remove tracked .meta files when the matching image binaries are
missing in a clean worktree. This gate prevents a dangerous headless import.
"""
from pathlib import Path
import subprocess
import collections
import sys

root = Path(__file__).resolve().parents[2]
result = subprocess.run(
    ['git','-C',str(root),'ls-files','-z'],
    capture_output=True,check=True
)
paths=[p.decode('utf-8') for p in result.stdout.split(b'\0') if p]
extensions={'.png','.jpg','.jpeg','.gif','.psd','.tga','.bmp','.webp','.exr','.hdr','.tif','.tiff'}
missing=[]
by_type=collections.Counter()
for rel in paths:
    if not rel.endswith('.meta'):continue
    owner=Path(rel[:-5])
    if owner.suffix.lower() not in extensions:continue
    if not (root/owner).is_file():
        missing.append(owner.as_posix())
        by_type[owner.suffix.lower()]+=1
print('UNITY_ASSET_PREFLIGHT_CHECKED_META_FILES='+str(sum(p.endswith('.meta') for p in paths)))
print('UNITY_MISSING_BINARY_OWNERS='+str(len(missing)))
if missing:
    print('UNITY_IMPORT_PRECONDITION=BLOCKED')
    print('EXTENSION_BREAKDOWN='+str(dict(sorted(by_type.items()))))
    for path in missing[:12]:
        print('MISSING_IMAGE_EXAMPLE='+path)
    print('Restore the project-approved internal binary asset archive, verify')
    print('the SHA-256 manifest, then rerun before opening/importing Unity.')
    sys.exit(2)
print('UNITY_IMPORT_PRECONDITION=PASS')
