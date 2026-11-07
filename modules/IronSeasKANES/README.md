# Iron Seas KANES Balao class fleet boat (SkizzyNaval update)

CKAN: `SkizzyNaval-IronSeasKANES` | strategy: **bundle** (full copy committed) | licence: **GPL-2.0** (inherited), plugin SkizzyRig **MIT**

This is an unofficial update of **Iron Seas KANES FIGHTING SHIPS 1** by **SkunkWorksCommand**
(the WWII US Navy Balao-class fleet submarine, version 1.0.0.1, 2020-09-17, KSP 1.10.1). It
targets KSP 1.12.5, BDArmory Plus 1.13 and the SkizzyNaval NAS. SkunkWorksCommand did not
make or endorse it, so report problems here.
Original: https://spacedock.info/mod/2528/Iron%20Seas%20KANES%20FIGHTING%20SHIPS%201

## Deck guns: they aim and fire, the gun meshes stay still

The six gun models (20 mm front and rear, .50 cal, 40 mm, 4" forward and rear) have no turret
or fire transforms (`Rotate`, `Pitch`, `fireTransform`), and each is one merged mesh. Since
0.1.1, the included plugin **SkizzyRig** (`GameData/IronSeasKANES/Plugins/SkizzyRig.dll`,
written for this update, MIT, source in `src/SkizzyRig/`) builds invisible yaw, pitch and fire
transforms at load time, placed from each model's own geometry. BDArmory now aims the guns and
fires from their muzzles, but **the visible barrels and mounts do not turn**: a merged mesh
can't be split without editing the model. `CHANGES.md` ("Turret rig") has the design and the
measured pivot, trunnion and muzzle of every gun.

## Licence decision

SpaceDock lists the mod as GPLv2. That allows modified copies to be redistributed under the
same licence, so the complete patched mod is shipped and **committed in full** here.

- `LICENSE.txt` holds the GPLv2 text, also installed as `GameData/IronSeasKANES/LICENSE.txt`.
  The original zip had no licence file.
- `GameData/IronSeasKANES/README.txt` carries the credits.
- Every modified file starts with a comment saying it was modified and when.
- `Plugins/SkizzyRig.dll` is our own plugin, MIT (`Plugins/SkizzyRig-LICENSE.txt`), with its
  source in `src/SkizzyRig/`. MIT code can ship next to the GPLv2 mod.

The original is not on CKAN, so there is nothing to provide or conflict with.

## What changed

The full list, with reasons, is in `CHANGES.md`. In short:

- BDArmory 1.13 keys, plus `NEEDS[NAS,BDArmory]` guards on the guns.
- The guns aim and fire through SkizzyRig's invisible turret transforms (the meshes stay still).
- The deck guns are rebalanced to their real weapons, including a new 4"/50 HC shell.
- The hull collapses at about 224 m, and the boat spawns surfaced.
- The antenna, periscope and snorkel get real descriptions.
- Fallback patches cover missing optional mods.
- The folders are grouped under `GameData/IronSeasKANES/`.

**Install note.** Remove an old manual install first (`GameData/BalaoClassFleetBoat`,
`GameData/4BladeFastScrew`), or every part loads twice.

## Dependencies

- Required: ModuleManager.
- To dive: WildBlueTools (with CommunityResourcePack), or Kerbal Powers Naval.
- Optional:
  - Firespitter: reverse thrust.
  - SkizzyNaval-NAS and BDArmory Plus 1.13: the guns (their turret plugin is included).
  - BonVoyage.
  - Buffalo: ballast effects.
  - SubTrim: automatic trim and depth hold.

## Files

| Path | Contents |
|---|---|
| `GameData/IronSeasKANES/` | The complete mod (58 files, including `Plugins/SkizzyRig.dll`), as shipped |
| `CHANGES.md` | Every change, the dependency table, pre-existing issues and an in-game test checklist |
| `diffs/IronSeasKANES_to_KSP1.12.5-BDA1.13.diff` | Every change against the original zip, the DLL included as a git binary patch (`git apply` in an extracted original) |
| `tools/patch_ironseas.py`, `tools/patch_ironseas_stage2.py`, `tools/patch_ironseas_stage3.py` (+ `kspcfg.py`) | Scripts that make the config changes (`python -I tools/patch_ironseas.py <dir>`); stage 3 is the turret rig |
| `tools/measure_gun_rigs.py` | Measures each gun's pivot, trunnion, bore and muzzle from its model and checks the cfgs |
| `src/SkizzyRig/` | Source, project file and MIT licence of `SkizzyRig.dll` (`dotnet build -c Release -p:DebugType=None -p:DebugSymbols=false`) |
| `tools/verify_ironseas_guns.py` | Checks every bullet, ammo, sound and model name the guns use |
| `manifest.json` | Size and SHA-256 of every shipped file |

The release zip also carries `CHANGES.md`, the diff, the tools and the plugin source under
`SkizzyNaval-docs/`, so the source travels with the download.
