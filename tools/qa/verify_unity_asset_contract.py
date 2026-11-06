#!/usr/bin/env python3
"""BeeKingdom Unity binary-asset completeness gate (read-only).

A tracked Unity .meta file is not proof that the corresponding binary exists.
Use on the target checkout AFTER restoring the internally stored artwork.
The gate never downloads, writes, deletes, or tracks artwork.
"""
from __future__ import annotations
import argparse, collections, hashlib, json, re, subprocess, sys
from pathlib import Path

def tracked_assets(root: Path) -> list[str]:
    result=subprocess.run(
        ['git','-C',str(root),'ls-files','-z','--','Assets'],
        capture_output=True,check=True)
    return [x.decode('utf-8') for x in result.stdout.split(b'\0') if x]

def inspect(root:Path) -> dict:
    tracked=tracked_assets(root)
    metas=[x for x in tracked if x.endswith('.meta')]
    missing=[];ignored=[]
    guids=collections.defaultdict(list)
    for rel in metas:
        base=rel[:-5]
        src=root/Path(rel)
        if not (root/Path(base)).exists():
            missing.append(base)
        if src.is_file():
            # Parse only the tiny header to avoid loading big binary assets.
            with src.open('rb') as f:
                header=f.read(900)
            found=re.search(rb'(?m)^guid:\s*([0-9a-fA-F]{32})\s*$',header)
            if found:
                guids[found.group(1).decode('ascii').lower()].append(rel)
    duplicates={g:files for g,files in guids.items() if len(files)>1}
    runtime_missing=[x for x in missing if not x.startswith((
        'Assets/Screenshots/','Assets/_Recovery/','Assets/InitTestScene'
    ))]
    groups=collections.Counter((('/'.join(x.split('/')[:5])) for x in runtime_missing))
    return {
      'project_root':str(root),
      'tracked_meta_count':len(metas),
      'missing_assets_all':len(missing),
      'missing_assets_runtime':len(runtime_missing),
      'missing_runtime_groups':dict(groups.most_common(12)),
      'duplicate_unity_guid_count':len(duplicates),
      'missing_asset_examples':runtime_missing[:15],
      'duplicate_guid_examples':[{'guid':g,'meta':files} for g,files in list(duplicates.items())[:5]],
      'complete':not runtime_missing and not duplicates
    }

def verify_manifest(root: Path, source: Path) -> dict:
    data=json.loads(source.read_text(encoding='utf-8'))
    if data.get('schemaVersion')!=1 or not isinstance(data.get('files'),list):
        raise ValueError('unsupported asset manifest format')
    failures=[];missing=[];seen=set()
    for entry in data['files']:
        raw=entry['path']
        rel=Path(raw)
        if rel.is_absolute() or '..' in rel.parts or not raw.startswith('Assets/'):
            raise ValueError('invalid asset manifest path')
        if raw in seen:raise ValueError('duplicate asset manifest path')
        seen.add(raw)
        path=(root/rel)
        if not path.is_file():
            missing.append(raw)
            continue
        digest=hashlib.sha256()
        with path.open('rb') as f:
            for block in iter(lambda:f.read(1024*1024),b''):
                digest.update(block)
        if digest.hexdigest()!=entry['sha256'] or path.stat().st_size!=entry['bytes']:
            failures.append(raw)
    return {
       'manifest_files_checked':len(data['files']),
       'manifest_missing_count':len(missing),
       'manifest_checksum_mismatch_count':len(failures),
       'manifest_missing_examples':missing[:10],
       'manifest_mismatched_examples':failures[:10],
       'manifest_complete':not missing and not failures
    }

def main(argv=None):
    p=argparse.ArgumentParser()
    p.add_argument('--root',type=Path,required=True)
    p.add_argument('--report',type=Path)
    p.add_argument('--strict',action='store_true',help='Exit 2 if assets missing or GUIDs duplicated')
    p.add_argument('--manifest',type=Path,help='Optional repository-relative SHA256 binary-asset manifest')
    args=p.parse_args(argv)
    root=args.root.resolve()
    if not (root/'.git').exists():p.error('Expected Git worktree/project root')
    if not (root/'Assets').is_dir():p.error('Expected Unity Assets folder')
    result=inspect(root)
    if args.manifest:
        manifest_path=(args.manifest if args.manifest.is_absolute()
            else root/args.manifest).resolve()
        result.update(verify_manifest(root,manifest_path))
        result['complete']=result['complete'] and result['manifest_complete']

    if args.report:
        target=args.report.resolve()
        if str(target).lower().startswith(str(root/'Assets').lower()):
            p.error('Report must not be stored inside Unity Assets')
        target.parent.mkdir(parents=True,exist_ok=True)
        target.write_text(json.dumps(result,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    print('BEEKINGDOM_UNITY_ASSET_CONTRACT='+('PASS' if result['complete'] else 'INCOMPLETE'))
    for key in ('tracked_meta_count','missing_assets_all','missing_assets_runtime','duplicate_unity_guid_count'):
        print(key.upper()+'='+str(result[key]))
    if args.manifest:
        print('CHECKSUM_FILES_VERIFIED='+str(result['manifest_files_checked']))
        print('CHECKSUM_ASSETS_MISSING='+str(result['manifest_missing_count']))
        print('CHECKSUM_ASSETS_MISMATCHED='+str(result['manifest_checksum_mismatch_count']))
    if result['missing_assets_runtime']:
        print('FIRST_MISSING='+','.join(result['missing_asset_examples'][:4]))
    if result['duplicate_unity_guid_count']:
        print('GUID_COLLISION_SAMPLE='+','.join(x['guid'] for x in result['duplicate_guid_examples'][:3]))
    if args.strict and not result['complete']:
        return 2
    return 0

if __name__=='__main__':
    sys.exit(main())
