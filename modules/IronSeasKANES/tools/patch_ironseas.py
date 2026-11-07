"""Patch a COPY of Iron Seas KANES (GameData/IronSeasKANES) for KSP 1.12.5 + BDArmory Plus 1.13 + updated NAS.
usage: python -I patch_ironseas.py <patched GameData/IronSeasKANES dir>
Edits files in place, preserving BOM and line endings per file. Idempotent: running it again changes nothing.
Stage 1: BDA 1.13 compatibility (NEEDS, removed air-detonation keys, Sail cleanup).
Stage 2 (patch_ironseas_stage2.py): gun rebalance, sensor descriptions, crush depth, surfaced hull trim.
"""
import os, re, sys

ROOT = sys.argv[1]
FUZE_NOTE = '// BDArmory 1.13: airDetonation/airDetonationTiming/maxAirDetonationRange were removed. The fuze now comes from fuzeType in the BULLET definition (NAS/BulletDefs/NAS_Bullets.cfg; 5inchHEUS = Timed).'
WEAPON_NEEDS_NOTE = '// Needs NAS (bullet 5inchHEUS, sounds, localisation, NASCategoryModule) and BDArmory; MM drops this part if either is missing.'


def read(p):
    raw = open(p, 'rb').read()
    bom = raw.startswith(b'\xef\xbb\xbf')
    txt = (raw[3:] if bom else raw).decode('utf-8')
    eol = '\r\n' if '\r\n' in txt else '\n'
    return txt.split(eol), eol, bom


def write(p, lines, eol, bom):
    open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + eol.join(lines).encode('utf-8'))


def keyval(line):
    s = line.split('//')[0].strip()
    if '=' not in s:
        return None, None
    k, v = s.split('=', 1)
    return k.strip(), v.strip()


def indent_of(line):
    return line[:len(line) - len(line.lstrip())]


log = []


def patch_weapon(path):
    lines, eol, bom = read(path)
    out, changed, noted = [], [], False
    first_part_done = False
    for line in lines:
        k, v = keyval(line)
        if not first_part_done and line.split('//')[0].strip() == 'PART':
            out.append(WEAPON_NEEDS_NOTE)
            out.append(line.replace('PART', 'PART:NEEDS[NAS,BDArmory]', 1))
            changed.append('PART -> PART:NEEDS[NAS,BDArmory]')
            first_part_done = True
            continue
        if k in ('airDetonation', 'airDetonationTiming', 'maxAirDetonationRange'):
            if not noted:
                out.append(indent_of(line) + FUZE_NOTE)
                noted = True
            changed.append('removed %s = %s' % (k, v))
            continue
        if k == 'defaultAirDetonationRange':
            out.append(line.replace('defaultAirDetonationRange', 'defaultDetonationRange', 1))
            changed.append('defaultAirDetonationRange -> defaultDetonationRange')
            continue
        if k == 'weaponType' and v.lower() == 'cannon':
            out.append(re.sub(r'(weaponType\s*=\s*)cannon', r'\1ballistic', line))
            changed.append('weaponType cannon -> ballistic')
            continue
        out.append(line)
    if changed:
        write(path, out, eol, bom)
        log.append((os.path.relpath(path, ROOT), changed))


def patch_sail(path):
    """Drop the PARTUPGRADE copies (BonVoyage defines BonVoyageUpgrade_v2/v3 itself) and the stray closing brace."""
    lines, eol, bom = read(path)
    out, changed = [], []
    depth = 0
    i = 0
    while i < len(lines):
        line = lines[i]
        code = line.split('//')[0]
        if depth == 0 and code.strip() == 'PARTUPGRADE':
            # skip the whole node
            j, d, seen = i, 0, False
            while True:
                c = lines[j].split('//')[0]
                d += c.count('{') - c.count('}')
                seen = seen or '{' in c
                j += 1
                if seen and d == 0:
                    break
            name = next((keyval(l)[1] for l in lines[i:j] if keyval(l)[0] == 'name'), '?')
            if not any('PARTUPGRADE copies removed' in o for o in out):
                out.append('// PARTUPGRADE copies removed: BonVoyage defines BonVoyageUpgrade_v2/v3 itself (duplicates otherwise).')
            changed.append('removed duplicate PARTUPGRADE %s' % name)
            i = j
            continue
        if depth == 0 and code.strip() == '}':
            changed.append('removed stray closing brace at line %d' % (i + 1))
            i += 1
            continue
        depth += code.count('{') - code.count('}')
        out.append(line)
        i += 1
    # trim trailing blank lines left behind, keep one final empty line
    while len(out) > 1 and out[-1].strip() == '' and out[-2].strip() == '':
        out.pop()
    if changed:
        write(path, out, eol, bom)
        log.append((os.path.relpath(path, ROOT), changed))


WEAPONS = ['20mmFF', '20mmRF', '4inchFF', '4inchRF', '50Cal', 'Single40mm']
base = os.path.join(ROOT, 'BalaoClassFleetBoat')
for w in WEAPONS:
    patch_weapon(os.path.join(base, w, 'part.cfg'))
patch_sail(os.path.join(base, 'Sail', 'part.cfg'))

# Stage 2: gun rebalance, sensor descriptions, dive computer crush depth, surfaced hull trim.
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import patch_ironseas_stage2
patch_ironseas_stage2.run(ROOT, log)

for p, ch in log:
    print(p)
    for c in ch:
        print('    -', c)
print(len(log), 'files changed')
