"""Check every bullet, ammo resource, sound and model path referenced by the Iron Seas guns. Reads files only.
usage: python -I verify_ironseas_guns.py <IronSeasKANES dir> <BDArmory dir> <NAS dir>"""
import sys, os, re
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from kspcfg import parse_file
iron, bda, nas = sys.argv[1:4]
gamedata = {'BDArmory': os.path.dirname(bda), 'NAS': os.path.dirname(nas), 'IronSeasKANES': os.path.dirname(iron)}

def nodes(root, kind):
    out = {}
    for dp, dn, fn in os.walk(root):
        for f in fn:
            if f.endswith('.cfg'):
                p = os.path.join(dp, f)
                for n in parse_file(p).nodes:
                    if n.name.split(':')[0] == kind:
                        nm = n.get('name')
                        out.setdefault(nm, []).append(os.path.relpath(p, root))
    return out

bullets = {}
for r in (bda, nas, iron):
    for k, v in nodes(r, 'BULLET').items():
        bullets.setdefault(k, []).extend([os.path.basename(r) + '/' + x for x in v])
resources = {}
for r in (bda, nas):
    for k, v in nodes(r, 'RESOURCE_DEFINITION').items():
        resources.setdefault(k, []).extend(v)
dups = {k: v for k, v in bullets.items() if len(v) > 1}
print('duplicate bullet names:', dups or 'none')

def asset(path, exts):
    top = path.split('/')[0]
    base = gamedata.get(top)
    if not base: return False
    return any(os.path.exists(os.path.join(base, path + e)) for e in exts)

ok = True
for gun in ['20mmFF', '20mmRF', '50Cal', 'Single40mm', '4inchFF', '4inchRF']:
    part = parse_file(os.path.join(iron, 'BalaoClassFleetBoat', gun, 'part.cfg')).nodes[0]
    w = [m for m in part.nodes if m.name == 'MODULE' and m.get('name') == 'ModuleWeapon'][0]
    res = [r.get('name') for r in part.nodes if r.name == 'RESOURCE']
    bt, am = w.get('bulletType'), w.get('ammoName')
    checks = [('bullet ' + bt, bt in bullets), ('ammo ' + am, am in resources), ('RESOURCE matches ammo', am in res)]
    for k in ('fireSoundPath', 'overheatSoundPath', 'reloadCompletePath', 'reloadAudioPath', 'explSoundPath'):
        if w.get(k): checks.append((k + ' ' + w.get(k), asset(w.get(k), ['.ogg', '.wav'])))
    if w.get('explModelPath'): checks.append(('explModelPath ' + w.get('explModelPath'), asset(w.get('explModelPath'), ['.mu'])))
    bad = [c for c, good in checks if not good]
    ok &= not bad
    print('%-11s %-16s %-11s rpm=%-4s range=%s/%s  %s' % (gun, bt, am, w.get('roundsPerMinute'), w.get('maxEffectiveDistance'),
          w.get('maxTargetingRange'), 'OK' if not bad else 'MISSING: ' + ', '.join(bad)))
    print('            bullet defined in', bullets.get(bt))
print('ALL OK' if ok else 'PROBLEMS')
