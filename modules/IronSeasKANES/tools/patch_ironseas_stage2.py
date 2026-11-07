"""Stage 2 of the Iron Seas KANES update (called from patch_ironseas.py; idempotent).

* Six deck guns: replace the cloned NAS 5"/38 numbers with the real weapon (bullet, ammo, RPM, ranges, turret limits,
  heat, sounds, description, tags), keep everything else.
* Antenna / periscope / snorkel: own descriptions, tags and a plain-text manufacturer (no NAS localisation needed).
* Sail dive computer + Sail/Hull maxPressure: Balao depths (test ~120 m, crush ~224 m) in KSP pressure terms.
* Hull: start surfaced with the main ballast blown (IntakeLqd amount 0).
Attach nodes are NOT touched.
"""
import os

# ----------------------------------------------------------------------------------------------------------------------
# minimal node-aware cfg editor (line based; keeps BOM, EOL, indentation and untouched lines byte-identical)
# ----------------------------------------------------------------------------------------------------------------------


def _read(p):
    raw = open(p, 'rb').read()
    bom = raw.startswith(b'\xef\xbb\xbf')
    txt = (raw[3:] if bom else raw).decode('utf-8')
    eol = '\r\n' if '\r\n' in txt else '\n'
    return txt.split(eol), eol, bom


def _keyval(line):
    s = line.split('//')[0].strip()
    if '=' not in s or '{' in s or '}' in s:
        return None, None
    k, v = s.split('=', 1)
    return k.strip(), v.strip()


def _indent(line):
    return line[:len(line) - len(line.lstrip())]


class Span:
    def __init__(self, typ, header, open_line, depth, parent):
        self.type, self.header, self.open, self.depth, self.parent = typ, header, open_line, depth, parent
        self.name = None
        self.close = None


class Cfg:
    def __init__(self, path):
        self.path = path
        self.lines, self.eol, self.bom = _read(path)
        self.changes = []
        self.parse()

    # -- parsing ----------------------------------------------------------------------------------------------------
    def parse(self):
        self.spans, self.owner = [], []
        stack, last_header = [], None
        for i, line in enumerate(self.lines):
            code = line.split('//')[0]
            s = code.strip()
            self.owner.append(stack[-1] if stack else None)
            if s and '=' not in s and '{' not in s and '}' not in s:
                last_header = s
            k, v = _keyval(line)
            if k == 'name' and stack and stack[-1].name is None:
                stack[-1].name = v
            for pos, ch in enumerate(code):
                if ch == '{':
                    hdr = code[:pos].strip() or (last_header or '')
                    typ = hdr.split(':')[0].split('[')[0].strip()
                    sp = Span(typ, hdr, i, len(stack), stack[-1] if stack else None)
                    stack.append(sp)
                    self.spans.append(sp)
                    last_header = None
                elif ch == '}':
                    if stack:
                        stack.pop().close = i

    def find(self, typ, name=None, parent=None):
        for sp in self.spans:
            if sp.type == typ and (name is None or sp.name in (name if isinstance(name, (list, tuple)) else [name])) \
                    and (parent is None or sp.parent is parent):
                return sp
        return None

    def child_lines(self, sp):
        return [i for i in range(sp.open + 1, sp.close) if self.owner[i] is sp]

    def key_lines(self, sp, key):
        return [i for i in self.child_lines(sp) if _keyval(self.lines[i])[0] == key]

    def _key_indent(self, sp):
        for i in self.child_lines(sp):
            if _keyval(self.lines[i])[0]:
                return _indent(self.lines[i])
        return '\t' * (sp.depth + 1)

    # -- editing ----------------------------------------------------------------------------------------------------
    def set(self, sp_sel, key, value, comment=None, after=None):
        """Set key = value in the node returned by sp_sel(); insert it (after key `after`, else at the end) if absent."""
        sp = sp_sel()
        if sp is None:
            raise KeyError('%s: node for %s not found' % (self.path, key))
        hits = self.key_lines(sp, key)
        if len(hits) > 1:
            raise ValueError('%s: %s appears %d times' % (self.path, key, len(hits)))
        tail = ('\t' + comment) if comment else ''
        if hits:
            i = hits[0]
            new = _indent(self.lines[i]) + key + ' = ' + value + tail
            if self.lines[i] != new:
                old_v = _keyval(self.lines[i])[1]
                self.lines[i] = new
                self.changes.append('%s %s: %s -> %s' % (self._where(sp), key, old_v, value))
        else:
            new = self._key_indent(sp) + key + ' = ' + value + tail
            pos = None
            if after:
                ah = self.key_lines(sp, after)
                if ah:
                    pos = ah[-1] + 1
            if pos is None:
                pos = sp.close
                while pos - 1 > sp.open and self.lines[pos - 1].strip() == '':
                    pos -= 1
            self.lines.insert(pos, new)
            self.changes.append('%s added %s = %s' % (self._where(sp), key, value))
        self.parse()

    def remove(self, sp_sel, key):
        sp = sp_sel()
        for i in reversed(self.key_lines(sp, key)):
            self.changes.append('%s removed %s = %s' % (self._where(sp), key, _keyval(self.lines[i])[1]))
            del self.lines[i]
        self.parse()

    def remove_node(self, sp_sel):
        sp = sp_sel()
        if sp is None:
            return
        start = sp.open
        # include the header line when the brace is on its own line
        if self.lines[start].strip() == '{' and start > 0 and self.lines[start - 1].split('//')[0].strip() == sp.header:
            start -= 1
        end = sp.close
        del self.lines[start:end + 1]
        # drop one blank line left behind
        if start < len(self.lines) and start > 0 and self.lines[start].strip() == '' and self.lines[start - 1].strip() == '':
            del self.lines[start]
        self.changes.append('%s removed node %s' % (self._where(sp.parent) if sp.parent else 'file', sp.header))
        self.parse()

    def replace_comment(self, prefix, new, sp_sel=None, insert_after_key=None, insert_before_key=None):
        """Replace the first line whose stripped text starts with `prefix` by `new` (same indentation).
        If none exists and an anchor key is given, insert `new` there."""
        sp = sp_sel() if sp_sel else None
        rng = self.child_lines(sp) if sp else range(len(self.lines))
        for i in rng:
            if self.lines[i].strip().startswith(prefix):
                nl = _indent(self.lines[i]) + new
                if self.lines[i] != nl:
                    self.lines[i] = nl
                    self.changes.append('comment: %s' % new[:90])
                self.parse()
                return
        if sp and (insert_after_key or insert_before_key):
            hits = self.key_lines(sp, insert_after_key or insert_before_key)
            if hits:
                pos = hits[0] + 1 if insert_after_key else hits[0]
                self.lines.insert(pos, _indent(self.lines[hits[0]]) + new)
                self.changes.append('comment added: %s' % new[:90])
                self.parse()

    def drop_line(self, exact, sp_sel=None):
        sp = sp_sel() if sp_sel else None
        rng = self.child_lines(sp) if sp else range(len(self.lines))
        for i in reversed(list(rng)):
            if self.lines[i].strip() == exact:
                del self.lines[i]
                self.changes.append('removed line: %s' % exact)
        self.parse()

    def collapse_blanks(self, sp_sel):
        """Collapse runs of blank lines inside a node to one; drop a blank line right before its closing brace."""
        sp = sp_sel()
        if sp is None:
            return
        removed = 0
        for i in range(sp.close - 1, sp.open, -1):
            if self.lines[i].strip() == '' and (self.lines[i + 1].strip() == '' or i + 1 == sp.close - removed):
                del self.lines[i]
                removed += 1
        if removed:
            self.changes.append('%s removed %d blank line(s)' % (self._where(sp), removed))
        self.parse()

    def ensure_line_after(self, anchor_prefix, new):
        """Make sure the line right after the first top-level line starting with anchor_prefix is `new`."""
        for i, l in enumerate(self.lines):
            if l.strip().startswith(anchor_prefix):
                if i + 1 < len(self.lines) and self.lines[i + 1].strip().startswith(new.split(':')[0]):
                    if self.lines[i + 1] != new:
                        self.lines[i + 1] = new
                        self.changes.append('comment: %s' % new[:90])
                else:
                    self.lines.insert(i + 1, new)
                    self.changes.append('comment added: %s' % new[:90])
                self.parse()
                return
        raise KeyError('%s: anchor %s not found' % (self.path, anchor_prefix))

    def add_notice(self, text):
        """GPLv2 section 2(a): modified files carry a notice. Inserted once as the first line."""
        if not self.lines or not self.lines[0].startswith(text[:40]):
            self.lines.insert(0, text)
            self.changes.append('added modification notice')
            self.parse()
        elif self.lines[0] != text:
            self.lines[0] = text
            self.changes.append('updated modification notice')
            self.parse()

    def _where(self, sp):
        if sp is None:
            return 'file'
        return sp.type + ('[%s]' % sp.name if sp.name and sp.type != 'PART' else '')

    def save(self, log, root):
        self.add_notice(NOTICE)
        if self.changes:
            open(self.path, 'wb').write((b'\xef\xbb\xbf' if self.bom else b'') + self.eol.join(self.lines).encode('utf-8'))
            log.append((os.path.relpath(self.path, root), self.changes))


# ----------------------------------------------------------------------------------------------------------------------
# data
# ----------------------------------------------------------------------------------------------------------------------

NOTICE = ('// Modified in 2026-11 by the unofficial KSP 1.12.5 / BDArmory 1.13 update of Iron Seas KANES (see CHANGES.md). '
          'Original by SkunkWorksCommand, GPLv2.')
MODEL_WARNING = ('// WARNING: this model.mu has no Rotate/Pitch/fireTransform transforms (static mesh), so BDArmory cannot '
                 'aim or fire it until the model gets them. See CHANGES.md.')
OLD_NEEDS_PREFIX = '// Needs NAS'
FUZE_PREFIX = '// BDArmory 1.13'

GUN_KIND = {'20mmFF': '20mm', '20mmRF': '20mm', '50Cal': '50cal', 'Single40mm': '40mm',
            '4inchFF': '4in', '4inchRF': '4in'}

GUNS = {
    '20mm': dict(
        needs='// Needs NAS (bullet 20mmAPLong, NASCategoryModule, localisation) and BDArmory (20x102Ammo, sounds); '
              'MM drops this part if either is missing.',
        description='Single 20 mm Oerlikon Mk 4 on a Mk 10 pedestal, the close-range AA gun on the Balao\'s bridge '
                    'cigarette deck. 450 rpm from 60-round drums (8 s drum change). Fires NAS 20 mm AP (20mmAPLong); '
                    'BDArmory 20x102Ammo stands in for the 20x110 mm round. Effective to about 1.5 km.',
        tags='nas bdarmory balao submarine sub gun turret navy naval 20mm oerlikon anti aircraft aa us usn',
        turret=[('pitchSpeedDPS', '60'), ('yawSpeedDPS', '90'), ('minPitch', '-15'), ('maxPitch', '85')],
        fuze='// BDArmory 1.13 removed airDetonation/airDetonationTiming/maxAirDetonationRange; the fuze comes from the '
             'BULLET definition. 20mmAPLong is solid AP (fuzeType None), so no detonation range is set.',
        weapon=[('shortName', '20mm Oerlikon', None),
                ('roundsPerMinute', '450', None),
                ('maxDeviation', '1.0', None),
                ('maxTargetingRange', '4000', None),
                ('maxEffectiveDistance', '1500', None),
                ('bulletType', '20mmAPLong', None),
                ('ammoName', '20x102Ammo', None),
                ('BeltFed', 'false', 'requestResourceAmount'),
                ('RoundsPerMag', '60', 'BeltFed'),
                ('ReloadTime', '8', 'RoundsPerMag'),
                ('reloadAudioPath', 'BDArmory/Sounds/reload', 'ReloadTime'),
                ('tracerStartWidth', '0.12', None),
                ('tracerEndWidth', '0.12', None),
                ('tracerDeltaFactor', '2.75', None),
                ('tracerInterval', '3', 'tracerDeltaFactor'),
                ('maxHeat', '2400', None),
                ('heatPerShot', '168', None),
                ('heatLoss', '1200', None),
                ('fireSoundPath', 'BDArmory/Parts/50CalTurret/sounds/shot', None),
                ('showReloadMeter', 'false', None)],
        weapon_remove=['defaultDetonationRange'],
        ammo=('20x102Ammo', '600')),
    '50cal': dict(
        needs='// Needs NAS (NASCategoryModule, localisation) and BDArmory (bullet 12.7mmBullet, 50CalAmmo, sounds); '
              'MM drops this part if either is missing.',
        description='Water-cooled .50 cal M2 Browning on a portable deck mount, used against aircraft and small craft. '
                    '500 rpm, BDArmory 12.7mmBullet (Raufoss, impact fuze) from 50CalAmmo. Effective to about 1.8 km.',
        tags='nas bdarmory balao submarine sub gun turret navy naval 50 cal browning m2 machine gun anti aircraft aa '
             'us usn',
        turret=[('pitchSpeedDPS', '60'), ('yawSpeedDPS', '90'), ('minPitch', '-15'), ('maxPitch', '85')],
        fuze='// BDArmory 1.13 removed airDetonation/airDetonationTiming/maxAirDetonationRange; the fuze comes from the '
             'BULLET definition. 12.7mmBullet bursts on impact, so no detonation range is set.',
        weapon=[('shortName', '.50 cal M2 Browning', None),
                ('roundsPerMinute', '500', None),
                ('maxDeviation', '0.4', None),
                ('maxTargetingRange', '3000', None),
                ('maxEffectiveDistance', '1800', None),
                ('bulletType', '12.7mmBullet', None),
                ('ammoName', '50CalAmmo', None),
                ('tracerStartWidth', '0.1', None),
                ('tracerEndWidth', '0.1', None),
                ('tracerDeltaFactor', '2.75', None),
                ('tracerInterval', '5', 'tracerDeltaFactor'),
                ('maxHeat', '3600', None),
                ('heatPerShot', '100', None),
                ('heatLoss', '1000', None),
                ('fireSoundPath', 'BDArmory/Parts/50CalTurret/sounds/shot', None),
                ('showReloadMeter', 'false', None),
                ('explModelPath', 'BDArmory/Models/explosion/30mmExplosion', None),
                ('explSoundPath', 'BDArmory/Sounds/subExplode', None)],
        weapon_remove=['defaultDetonationRange'],
        ammo=('50CalAmmo', '1000')),
    '40mm': dict(
        needs='// Needs NAS (bullet 40mmHEBofors, NASCategoryModule, localisation) and BDArmory (40x311Ammo, sounds); '
              'MM drops this part if either is missing.',
        description='Single 40 mm Bofors L/60 on a Mk 3 mount, fitted to the cigarette deck of late-war fleet boats. '
                    '120 rpm from 4-round clips. Fires NAS 40 mm HE (40mmHEBofors, timed fuze) from 40x311Ammo. '
                    'Effective to about 2.5 km against aircraft and light craft.',
        tags='nas bdarmory balao submarine sub gun turret navy naval 40mm bofors anti aircraft aa us usn',
        turret=[('pitchSpeedDPS', '30'), ('yawSpeedDPS', '35'), ('minPitch', '-15'), ('maxPitch', '85')],
        fuze='// BDArmory 1.13 removed airDetonation/airDetonationTiming/maxAirDetonationRange; the fuze comes from the '
             'BULLET definition. 40mmHEBofors is fuzeType = Timed (NAS_Bullets.cfg); the burst distance is set from '
             'the target range, defaultDetonationRange is the fallback.',
        weapon=[('shortName', '40mm Bofors Mk 3', None),
                ('roundsPerMinute', '120', None),
                ('maxDeviation', '0.7', None),
                ('maxTargetingRange', '5000', None),
                ('maxEffectiveDistance', '2500', None),
                ('defaultDetonationRange', '2500', None),
                ('bulletType', '40mmHEBofors', None),
                ('ammoName', '40x311Ammo', None),
                ('tracerStartWidth', '0.24', None),
                ('tracerEndWidth', '0.24', None),
                ('tracerDeltaFactor', '2.75', None),
                ('tracerInterval', '3', 'tracerDeltaFactor'),
                ('maxHeat', '3000', None),
                ('heatPerShot', '205', None),
                ('heatLoss', '1500', None),
                ('fireSoundPath', 'BDArmory/Parts/m230ChainGun/Sounds/m230shot', None),
                ('showReloadMeter', 'false', None)],
        weapon_remove=[],
        ammo=('40x311Ammo', '200')),
    '4in': dict(
        needs='// Needs NAS (sounds, NASCategoryModule, localisation) and BDArmory (100mmShells); the bullet '
              'IronSeas4in50HC is defined in IronSeasKANES/BulletDefs. MM drops this part if either is missing.',
        description='4"/50 caliber Mk 9 deck gun on a manually trained wet mount (Modified Mk 12), the Balao\'s main '
                    'surface armament. 8-9 rounds per minute; 15 kg high-capacity shell (1.2 kg Explosive D, impact '
                    'fuze) at 884 m/s from 100mmShells. Elevation -15 to +20 degrees: surface targets only, no AA fire.',
        tags='nas bdarmory balao submarine sub gun turret navy naval deck gun 4 inch 4in 102mm surface us usn',
        turret=[('pitchSpeedDPS', '8'), ('yawSpeedDPS', '10'), ('minPitch', '-15'), ('maxPitch', '20')],
        fuze='// BDArmory 1.13 removed airDetonation/airDetonationTiming/maxAirDetonationRange; the fuze comes from the '
             'BULLET definition. IronSeas4in50HC is fuzeType = Impact (point-detonating HC shell), so no detonation '
             'range is set.',
        weapon=[('shortName', '4in/50 Mk 9', None),
                ('roundsPerMinute', '9', None),
                ('maxDeviation', '0.4', None),
                ('maxTargetingRange', '12000', None),
                ('maxEffectiveDistance', '6000', None),
                ('bulletType', 'IronSeas4in50HC', None),
                ('ammoName', '100mmShells', None),
                ('engageAir', 'false', None)],
        weapon_remove=['defaultDetonationRange'],
        ammo=('100mmShells', '60')),
}

SENSORS = {
    'Antenna': dict(
        description='Retractable radio mast for the Balao class fleet boat. Raise it on the surface or at periscope '
                    'depth and lower it before going deep or running fast. Animated only: it is not a CommNet antenna.',
        tags='balao submarine sub radio antenna mast retractable'),
    'Scope': dict(
        description='Retractable periscope for the Balao class fleet boat. Raise it at periscope depth (about 18 m keel '
                    'depth) to keep watch without surfacing; lower it before going deeper. Animated only: it adds no '
                    'camera or sensor.',
        tags='balao submarine sub periscope scope mast retractable'),
    'Snorkel': dict(
        description='Diesel induction snorkel for the Balao class fleet boat (a post-war snorkel refit). Holds the '
                    'IntakeAir that the hull\'s diesel generator burns: raise it and switch the diesel on (hull menu) '
                    'to recharge the batteries.',
        tags='balao submarine sub snorkel snorkle diesel intake air mast retractable'),
}

# Balao: test depth 400 ft (about 122 m), collapse depth estimated 200-250 m.
# KSP static pressure under water: kPa = 101.3 + 9.81 * depth(m) on Kerbin (ocean density 1.0) -> 2300 kPa = 224 m.
CRUSH_KPA = '2300'
DIVE_COMMENT = ('// Balao class: test depth 400 ft (about 120 m = 1280 kPa), collapse about 225 m. KSP: kPa = 101.3 + 9.81 '
                'x depth (m), so 2300 kPa crushes the hull at about 224 m. The Sail and Hull also carry maxPressure = '
                '2300, because this override only changes its own part.')


def run(root, log):
    base = os.path.join(root, 'BalaoClassFleetBoat')

    # --- guns ------------------------------------------------------------------------------------------------------
    for folder, kind in GUN_KIND.items():
        g = GUNS[kind]
        c = Cfg(os.path.join(base, folder, 'part.cfg'))
        part = lambda: c.find('PART')
        turret = lambda: c.find('MODULE', 'ModuleTurret')
        weapon = lambda: c.find('MODULE', 'ModuleWeapon')
        c.replace_comment(OLD_NEEDS_PREFIX, g['needs'])
        c.ensure_line_after(OLD_NEEDS_PREFIX, MODEL_WARNING)
        c.set(part, 'description', g['description'])
        c.set(part, 'tags', g['tags'])
        for k, v in g['turret']:
            c.set(turret, k, v)
        c.replace_comment(FUZE_PREFIX, g['fuze'], weapon)
        # shortName replaces the leftover '//shortName = Mk 12 5"/38 DP' comment
        if not c.key_lines(weapon(), 'shortName'):
            c.replace_comment('//shortName', 'shortName = ' + g['weapon'][0][1], weapon)
        c.drop_line('//bulletVelocity = 792', weapon)
        for k in g['weapon_remove']:
            c.remove(weapon, k)
        for k, v, after in g['weapon']:
            c.set(weapon, k, v, after=after)
        res_name, amount = g['ammo']
        res = lambda: c.find('RESOURCE', ['5/62Shell', res_name], c.find('PART'))
        c.set(res, 'name', res_name)
        c.set(res, 'amount', amount)
        c.set(res, 'maxAmount', amount)
        c.save(log, root)

    # --- antenna / periscope / snorkel -----------------------------------------------------------------------------
    for folder, s in SENSORS.items():
        c = Cfg(os.path.join(base, folder, 'part.cfg'))
        part = lambda: c.find('PART')
        c.set(part, 'manufacturer', 'Kerbin Navy')
        c.set(part, 'description', s['description'])
        c.set(part, 'tags', s['tags'])
        c.save(log, root)

    # --- sail: dive computer and crush depth -----------------------------------------------------------------------
    c = Cfg(os.path.join(base, 'Sail', 'part.cfg'))
    part = lambda: c.find('PART')
    dive = lambda: c.find('MODULE', 'WBIDiveComputer')
    c.set(part, 'maxPressure', CRUSH_KPA, comment='// kPa: hull collapse at about 224 m (see WBIDiveComputer)',
          after='maxTemp')
    c.replace_comment('// Balao class: test depth', DIVE_COMMENT, dive, insert_before_key='maxPressureOverride')
    c.set(dive, 'maxPressureOverride', CRUSH_KPA)
    # wbiGuppy1/2 upgrades (8000/10000 kPa) are not defined anywhere and would be far too deep for a Balao
    c.remove_node(lambda: c.find('UPGRADES', parent=dive()) if dive() else None)
    c.remove(dive, 'showUpgradesInModuleInfo')
    c.collapse_blanks(dive)
    c.save(log, root)

    # --- hull: crush depth and surfaced trim -----------------------------------------------------------------------
    c = Cfg(os.path.join(base, 'Hull', 'part.cfg'))
    part = lambda: c.find('PART')
    c.set(part, 'maxPressure', CRUSH_KPA, comment='// kPa: collapse at about 224 m, same as the Sail dive computer',
          after='maxTemp')
    ballast = lambda: c.find('RESOURCE', 'IntakeLqd', c.find('PART'))
    c.set(ballast, 'amount', '0', comment='// surfaced: main ballast blown (was 1100 of 1400, 79 % flooded)')
    c.save(log, root)
