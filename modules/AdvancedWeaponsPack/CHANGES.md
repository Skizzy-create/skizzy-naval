# Advanced Weapons Pack 0.22: KSP 1.12.5 / BDArmory Plus 1.13 compatibility

## Install

Copy `GameData\Advanced Weapons Pack\` into KSP's `GameData\`. It needs ModuleManager and BDArmory Plus 1.13. NKD Reborn (`NKDR`, already installed) is optional and only used for one explosion effect. The pack ships no DLLs, ModuleManager or Firespitter.

## What the pack expects from NKD

The pack has no MM patches and no references to NKD part names. Its only NKD dependency is one part, `狂飙一号` (shortName "Castle Bravo NUCLEAR", author "hentai chou"). It is a copy of North Kerbin Dynamics' Castle Bravo.

| what it uses | NKD (original) | in your install |
|---|---|---|
| `BDExplosivePart.explModelPath = NKD/effects/Explosion_Castle` | NKD folder | NKD Reborn has it as `NKDR/effects/Explosion_Castle` (the folder was renamed) |
| `BDExplosivePart.explSoundPath = NKD/sounds/explosion_MOAB` | NKD folder | `NKDR/sounds/explosion_MOAB` |
| `MODULE { name = ModuleShockwave; explosiveYield = 15000000000 }` | old NKD plugin | **missing**: NKDR has no plugin, and nothing installed provides it. KSP drops the module ("Cannot find a PartModule of typename 'ModuleShockwave'"). |
| `BDExplosivePart.tntMass = 15000000000` (15 Mt as kg of TNT) | | BDA treats this as a conventional charge with a blast radius of about 36 km (14.8·∛m). |

NKDR builds its own Castle Bravo (`harpCastleBravo`) with BDA's nuke module instead (`BDModuleNuke`, yield 15000 kt, thermalRadius 7200).

## What changed

All changes are in one ModuleManager file, `GameData/Advanced Weapons Pack/Patches/zz_AWP_BDA113_Compat.cfg`. No original file is edited. `upstream.diff` adds this file.

**ModuleShockwave is converted, not dropped.** The bomb's `ModuleShockwave` and `BDExplosivePart` are replaced with a `BDModuleNuke`:

- `yield = 15000` (15e9 kg TNT = 15 Mt), `thermalRadius = 7200`, `fluence = 2.0`;
- BDA's nuke flash, shock, blast, plume, scatter and sound models.

These are the same values NKDR uses for its Castle Bravo, so the bomb now gets BDA's proper nuclear handling: delayed blast wave, thermal radius and nuke FX. The old version was a conventional 36 km-radius blast plus a module that no longer exists.

When NKDR is installed, the bomb's `MissileLauncher.explModelPath` is also set to `NKDR/effects/Explosion_Castle`, exactly as on NKDR's own Castle Bravo.

The patch matches the part by `author = hentai chou` plus the presence of ModuleShockwave, so the Chinese part name never appears in a patch. No other part matches.

### Left alone

- `blastRadius = 0` / `blastPower = 0` on the bomb's MissileLauncher (author's choice). Because `blastRadius >= 0`, `MissileLauncher.GetBlastRadius()` returns 0. The bomb arms immediately, and BDA's AI doesn't hold it back for its own safety. **A 15 Mt bomb dropped by the AI will very likely kill the dropper.** Remove the line if you'd rather BDA compute a yield-based standoff, as NKDR's part does. Note that the computed standoff is about 61 km, so the AI then effectively never drops it.
- Part names: `AIM*54 Phoenix` contains a `*`, and `一眼丁真` / `狂飙一号` are non-ASCII. They work, but renaming would break existing craft, so they're kept.
- `mesh = …` mismatches on the AIM-54, AIM120B and PL2. KSP ignores `mesh` and loads the folder's single `.mu`.
- `ADMM`, `一眼丁真` and the bomb are joke or test parts (balance not touched).
- `explSoundPath` on the GBU-12's MissileLauncher isn't a KSPField there; it's ignored and BDA's default is used.

## Verification done (static only)

- Every PART was scanned for model, texture and sound paths (case-sensitive), module keys against BDA 1.13 KSPFields, and name clashes with the installed GameData. Nothing else needed fixing: all 14 missiles and bombs use valid `homingType`/`targetingType` values and existing BDA exhaust and sound assets.
- The patch was run through a ModuleManager-subset simulator. Both blocks matched only `狂飙一号`, and the result has a `BDModuleNuke` with no `ModuleShockwave` or `BDExplosivePart`.
- `BDModuleNuke`'s fields were checked against BDA 1.13 (`thermalRadius`, `yield`, `fluence`, `isEMP`, `reportingName`, `*ModelPath`, `blastSoundPath`), and the BDA nuke assets exist in your install.

## Unverified

- Not run in KSP.
- MM matching on `#author[hentai?chou]` follows MM's documented wildcard rules.

## In-game test

1. Start KSP and open `KSP.log`. There should be no "Cannot find a PartModule of typename 'ModuleShockwave'".
2. In the editor, open the "Castle Bravo NUCLEAR" (`狂飙一号`) PAW. It shows the BDA nuke module (yield 15000) and no BDExplosivePart.
3. Test in a sandbox: drop it from altitude on a target far away, more than 10 km from anything you care about. You should see BDA's nuke flash and mushroom cloud, and the delayed shockwave.
4. Check that the 12 AAMs fire normally from a BDA weapon manager.
