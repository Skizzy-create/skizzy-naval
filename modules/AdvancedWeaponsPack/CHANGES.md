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

**Manual use only (0.1.2).** BDArmory's AI never selects the bomb; players can still use it. The patch turns off the bomb's four engagement flags (`engageAir`, `engageMissile`, `engageGround`, `engageSLW` on its MissileLauncher) and adds a sentence saying so to the part description. How BDArmory uses them (read in the BDArmory 1.13.1 source, `Control/MissileFire.cs` and `Weapons/EngageableWeapon.cs`):

- `MissileFire.UpdateList` puts every weapon into `weaponArray`, the weapon manager's list that players cycle through. It adds a weapon to the per-target-type lists `weaponTypesAir`, `weaponTypesMissile`, `weaponTypesGround` and `weaponTypesSLW` only if `GetEngageAirTargets()` (and so on) is true, and those just return the four flags.
- `SmartPickWeapon_EngagementEnvelope`, which guard mode, the AI pilots and the fork's parallel salvo use to choose a weapon for a target, only picks from those four lists. With all four flags off the bomb is in none of them, so the AI never selects it.
- Manual use is unchanged: select "Castle Bravo NUCLEAR" in the weapon manager and fire it with the fire key, or use the part's own "Fire Missile" button or action group, which does not go through weapon selection.
- Not used: "Disable Engage Options" (`engageEnabled = false`) makes `CheckEngagementEnvelope` accept every target, which is the opposite of what is wanted. A high `weaponChannel` hides the weapon from the weapon manager completely, manual selection included.

Limits: craft saved before 0.1.2 keep the flags they were saved with (re-place the bomb, or switch the four "Engage ..." toggles off in its part menu). The toggles stay in the part menu, so a player can turn them back on. The weapon manager's global "Engage Air / Missile / Surface / SLW" buttons copy their state to every weapon on the vessel, the bomb included.

### Left alone

- `blastRadius = 0` / `blastPower = 0` on the bomb's MissileLauncher (author's choice). Because `blastRadius >= 0`, `MissileLauncher.GetBlastRadius()` returns 0, so the bomb arms immediately. Since the AI no longer selects the bomb, this only matters when you drop it yourself: **a 15 Mt bomb will very likely kill the aircraft that drops it** unless it is far away and fast.
- Part names: `AIM*54 Phoenix` contains a `*`, and `一眼丁真` / `狂飙一号` are non-ASCII. They work, but renaming would break existing craft, so they're kept.
- `mesh = …` mismatches on the AIM-54, AIM120B and PL2. KSP ignores `mesh` and loads the folder's single `.mu`.
- `ADMM`, `一眼丁真` and the bomb are joke or test parts (balance not touched).
- `explSoundPath` on the GBU-12's MissileLauncher isn't a KSPField there; it's ignored and BDA's default is used.

## Verification done (static only)

- Every PART was scanned for model, texture and sound paths (case-sensitive), module keys against BDA 1.13 KSPFields, and name clashes with the installed GameData. Nothing else needed fixing: all 14 missiles and bombs use valid `homingType`/`targetingType` values and existing BDA exhaust and sound assets.
- The patch was run through a ModuleManager-subset simulator. Both blocks matched only `狂飙一号`, and the result has a `BDModuleNuke` with no `ModuleShockwave` or `BDExplosivePart`, all four engage flags `false`, and the extra description sentence (with and without NKDR).
- `BDModuleNuke`'s fields were checked against BDA 1.13 (`thermalRadius`, `yield`, `fluence`, `isEMP`, `reportingName`, `*ModelPath`, `blastSoundPath`), and the BDA nuke assets exist in your install.

## Unverified

- Not run in KSP.
- MM matching on `#author[hentai?chou]` follows MM's documented wildcard rules.
- The AI exclusion comes from reading the BDArmory source, not from play-testing.

## In-game test

1. Start KSP and open `KSP.log`. There should be no "Cannot find a PartModule of typename 'ModuleShockwave'".
2. In the editor, open the "Castle Bravo NUCLEAR" (`狂飙一号`) PAW. It shows the BDA nuke module (yield 15000) and no BDExplosivePart.
3. Its part menu shows Engage Air, Missile, Surface and SLW all `false`.
4. Test in a sandbox: drop it from altitude on a target far away, more than 10 km from anything you care about. You should see BDA's nuke flash and mushroom cloud, and the delayed shockwave.
5. Give an AI aircraft the bomb plus another bomb and a ground target, in guard mode. It should use the other bomb and never select "Castle Bravo NUCLEAR". With only the nuke, it should report no weapon for the target.
6. Check that the 12 AAMs fire normally from a BDA weapon manager.
