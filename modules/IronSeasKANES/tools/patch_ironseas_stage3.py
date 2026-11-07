"""Stage 3 of the Iron Seas KANES update (called from patch_ironseas.py after stage 2; idempotent): turret rig.

The six gun models are single merged meshes with no Rotate/Pitch/fireTransform transforms. Each gun cfg gets a
ModuleRigTurret block (SkizzyRig.dll, GameData/IronSeasKANES/Plugins, source in src/SkizzyRig) that builds
SkizzyRigBase/SkizzyRigYaw/SkizzyRigPitch/SkizzyRigFire at load time, and ModuleTurret/ModuleWeapon are pointed at
those names. The numbers come from the mesh vertices (see CHANGES.md, "Turret rig").
"""
import os
from patch_ironseas_stage2 import Cfg, OLD_NEEDS_PREFIX, _indent

RIG_NOTE = ('// Turret rig: this model.mu is one static mesh without Rotate/Pitch/fireTransform. ModuleRigTurret '
            '(IronSeasKANES/Plugins/SkizzyRig.dll) adds invisible SkizzyRig* yaw, pitch and fire transforms at load '
            'time, so BDArmory aims the gun and fires from its muzzle, but the mesh itself does not turn. See CHANGES.md.')
OLD_WARNING_PREFIX = '// WARNING: this model.mu has no Rotate/Pitch/fireTransform'

NAMES = dict(base='SkizzyRigBase', yaw='SkizzyRigYaw', pitch='SkizzyRigPitch', fire='SkizzyRigFire')

# Part frame (= model frame, rescaleFactor 1), metres. Up on the boat is -Z; the guns stack-attach with +Y along
# the hull's +Y (bow). x is 0 on every gun (all measured |x| < 2 mm).
RIG = {
    '20mmFF': dict(pivot=('0, 0, -0.666', 'yaw axis through the pedestal centre, at the top of the fixed column'),
                   trunnion=('0, 0.006, -0.8316', 'top face of the cradle fork arms (the model has no trunnion pin)'),
                   bore=('0, 0.9749, -0.2226', 'barrel as modelled: 12.86 deg up, toward +Y'),
                   muzzle=('0, 0.8442, -1.175', 'flash hider tip, on the bore axis')),
    '20mmRF': dict(pivot=('0, 0, -0.666', 'yaw axis through the pedestal centre, at the top of the fixed column'),
                   trunnion=('0, -0.006, -0.8316', 'top face of the cradle fork arms (the model has no trunnion pin)'),
                   bore=('0, -0.9749, -0.2226', 'barrel as modelled: 12.86 deg up, toward -Y'),
                   muzzle=('0, -0.8442, -1.175', 'flash hider tip, on the bore axis')),
    '50Cal': dict(pivot=('0, 0, -0.026', 'yaw axis through the pintle, at its top'),
                  trunnion=('0, 0.0548, -0.1413', 'centre of the round lug at the top of the cradle yoke'),
                  bore=('0, -1, 0', 'barrel as modelled: level, toward -Y'),
                  muzzle=('0, -0.6832, -0.1355', 'muzzle tip, on the bore axis')),
    'Single40mm': dict(pivot=('0, 0, -0.187', 'yaw axis through the base centre, at the top of the base plate'),
                       trunnion=('0, -0.0814, -0.852', 'axis of the trunnion bearings on the carriage side plates'),
                       bore=('0, -0.8641, -0.5033', 'barrel as modelled: 30.22 deg up, toward -Y'),
                       muzzle=('0, -1.3224, -1.9608', 'muzzle tip, on the bore axis')),
    '4inchFF': dict(pivot=('0, 0, -0.179', 'yaw axis through the pedestal centre, at the top of the fixed pedestal'),
                    trunnion=('0, -0.0006, -0.7652', 'centre of the trunnion pins'),
                    bore=('0, 1, -0.0057', 'barrel as modelled: 0.33 deg up, toward +Y'),
                    muzzle=('0, 2.4298, -0.7643', 'muzzle face, on the bore axis')),
    '4inchRF': dict(pivot=('0, 0, -0.179', 'yaw axis through the pedestal centre, at the top of the fixed pedestal'),
                    trunnion=('0, 0.0006, -0.7652', 'centre of the trunnion pins'),
                    bore=('0, -1, -0.0057', 'barrel as modelled: 0.33 deg up, toward -Y'),
                    muzzle=('0, -2.4298, -0.7643', 'muzzle face, on the bore axis')),
}


def rig_block(folder, ind):
    r = RIG[folder]
    i1 = ind + '\t'
    return [
        ind + 'MODULE:NEEDS[SkizzyRig]',
        ind + '{',
        i1 + 'name = ModuleRigTurret',
        i1 + '// SkizzyRig.dll (MIT, source in src/SkizzyRig) builds the turret transforms this model lacks.',
        i1 + '// The model is one merged mesh, so no mesh node is moved: the transforms are invisible and the gun mesh stays still.',
        i1 + '// Part frame, metres; up on the boat is -Z. Measured from the mesh vertices (see CHANGES.md).',
        i1 + 'baseName = ' + NAMES['base'],
        i1 + 'yawName = ' + NAMES['yaw'],
        i1 + 'pitchName = ' + NAMES['pitch'],
        i1 + 'fireName = ' + NAMES['fire'],
        i1 + 'upAxis = 0, 0, -1',
        i1 + 'pivot = %s\t// %s' % r['pivot'],
        i1 + 'trunnion = %s\t// %s' % r['trunnion'],
        i1 + 'boreDirection = %s\t// %s' % r['bore'],
        i1 + 'muzzles = %s\t// %s' % r['muzzle'],
        ind + '}',
    ]


def run(root, log):
    base = os.path.join(root, 'BalaoClassFleetBoat')
    for folder in RIG:
        c = Cfg(os.path.join(base, folder, 'part.cfg'))
        before = list(c.lines)
        turret = lambda: c.find('MODULE', 'ModuleTurret')
        weapon = lambda: c.find('MODULE', 'ModuleWeapon')

        # header comment: the rig note replaces the old "cannot aim or fire" warning
        idx =[i for i, l in enumerate(c.lines) if l.startswith(OLD_WARNING_PREFIX)]
        for i in reversed(idx):
            del c.lines[i]
            c.changes.append('removed the "cannot aim or fire" warning')
        c.parse()
        c.ensure_line_after(OLD_NEEDS_PREFIX, RIG_NOTE)

        # ModuleRigTurret block, right before ModuleTurret (regenerated if present)
        rig = c.find('MODULE', 'ModuleRigTurret')
        if rig is not None:
            start = rig.open - 1 if c.lines[rig.open].strip() == '{' else rig.open
            ind = _indent(c.lines[start])
            del c.lines[start:rig.close + 1]
        else:
            t = turret()
            start = t.open - 1 if c.lines[t.open].strip() == '{' else t.open
            ind = _indent(c.lines[start])
        c.lines[start:start] = rig_block(folder, ind)
        c.parse()

        c.set(turret, 'yawTransformName', NAMES['yaw'])
        c.set(turret, 'pitchTransformName', NAMES['pitch'])
        c.set(turret, 'baseTransformName', NAMES['base'], after='pitchTransformName')
        c.set(weapon, 'fireTransformName', NAMES['fire'])

        if c.lines != before:
            if not any('ModuleRigTurret' in ch for ch in c.changes):
                c.changes.append('ModuleRigTurret block (SkizzyRig) before ModuleTurret')
        else:
            c.changes = []
        c.save(log, root)
