# Iron Seas KANES Balao class fleet boat (SkizzyNaval update)

CKAN: `SkizzyNaval-IronSeasKANES` | strategy: **bundle** (full copy committed) | licence: **GPL-2.0** (inherited)

This is an unofficial update of **Iron Seas KANES FIGHTING SHIPS 1** by **SkunkWorksCommand**
(the WWII US Navy Balao-class fleet submarine, version 1.0.0.1, 2020-09-17, KSP 1.10.1). It
targets KSP 1.12.5, BDArmory Plus 1.13 and the SkizzyNaval NAS. SkunkWorksCommand did not
make or endorse it, so report problems here.
Original: https://spacedock.info/mod/2528/Iron%20Seas%20KANES%20FIGHTING%20SHIPS%201

## Known issue: the guns do not fire

**The six gun models (20 mm front and rear, .50 cal, 40 mm, 4" forward and rear) have no
turret or fire transforms** (`Rotate`, `Pitch`, `fireTransform`). Each is a single static mesh,
so BDArmory can't turn or fire them, and they will not fire until the models are fixed in
Unity/PartTools. The gun configs are already set up for when the models get those transforms;
see `CHANGES.md` for what is needed.

## Licence decision

SpaceDock lists the mod as GPLv2. That allows modified copies to be redistributed under the
same licence, so the complete patched mod is shipped and **committed in full** here.

- `LICENSE.txt` holds the GPLv2 text, also installed as `GameData/IronSeasKANES/LICENSE.txt`.
  The original zip had no licence file.
- `GameData/IronSeasKANES/README.txt` carries the credits.
- Every modified file starts with a comment saying it was modified and when.

The original is not on CKAN, so there is nothing to provide or conflict with.

## What changed

The full list, with reasons, is in `CHANGES.md`. In short:

- BDArmory 1.13 keys, plus `NEEDS[NAS,BDArmory]` guards on the guns.
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
  - SkizzyNaval-NAS and BDArmory Plus 1.13: the guns.
  - BonVoyage.
  - Buffalo: ballast effects.
  - SubTrim: automatic trim and depth hold.

## Files

| Path | Contents |
|---|---|
| `GameData/IronSeasKANES/` | The complete mod (56 files), as shipped |
| `CHANGES.md` | Every change, the dependency table, pre-existing issues and an in-game test checklist |
| `diffs/IronSeasKANES_to_KSP1.12.5-BDA1.13.diff` | Every change against the original zip (`git apply` in an extracted original) |
| `tools/patch_ironseas.py`, `tools/patch_ironseas_stage2.py` (+ `kspcfg.py`) | Scripts that make the config changes (`python -I tools/patch_ironseas.py <dir>`) |
| `tools/verify_ironseas_guns.py` | Checks every bullet, ammo, sound and model name the guns use |
| `manifest.json` | Size and SHA-256 of every shipped file |

The release zip also carries `CHANGES.md`, the diff and the tools under `SkizzyNaval-docs/`,
so the GPL source travels with the download.
