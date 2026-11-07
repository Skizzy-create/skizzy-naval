# Skizzy Naval

Compatibility builds of naval and combat mods for **Kerbal Space Program 1.12.5** with
**BDArmory Plus 1.13**, released through a CKAN metadata repository.

Each mod has its own folder under `modules/`, with its own GameData tree, changelog, licence
and attribution. A version tag (`v0.1.0`) makes GitHub Actions build one zip per module,
publish it as a release asset and regenerate the matching `.ckan` files in
[Skizzy-create/skizzy-ckan](https://github.com/Skizzy-create/skizzy-ckan).

These builds are unofficial. Please report problems here, not to the original authors.

## Install with CKAN

1. CKAN > *Settings* > *Metadata repositories* > *New*, name it `SkizzyNaval`, URL:

   ```
   https://github.com/Skizzy-create/skizzy-ckan/archive/refs/heads/main.tar.gz
   ```

2. Refresh. Install **Skizzy Naval Pack** (`SkizzyNavalPack`) for everything, or pick single
   `SkizzyNaval-*` modules.

The pack depends on every module marked "pack" below, plus ModuleManager and BDArmory Plus
1.13 (either `BDArmoryForRunwayProject` from the main CKAN repository or `BDArmory-Skizzy`).
It recommends Kerbal Powers Naval, Buoyancy Adjuster and S.A.C.D., and suggests Hangar and
the TBAE patch.

Shared libraries (ModuleManager, Firespitter, Harmony, PhysicsRangeExtender and so on) are
never shipped here. CKAN installs them from the main repository.

## Modules and licence decisions

The rules:

- If the original licence allows modified copies (permissive or CC BY-SA type), the full
  patched mod is shipped with attribution.
- If the licence is restrictive or unclear, only our own files are shipped, and the module
  depends on the original.
- An overlay is also used whenever the change only adds files and CKAN already installs the
  original.

| Module (CKAN identifier) | Original, author | Licence | Shipped as | Why |
|---|---|---|---|---|
| `SkizzyNaval-HullBreach` | HullBreach 1.7.0.5, gomker, SpannerMonkey, Gedas-S, zzhwudiaa | CC BY-NC-SA 3.0 | full port, committed ("bundle") | NC-SA allows adapted copies; the plugin is rewritten. Provides and conflicts with `HullBreach` |
| `SkizzyNaval-NAS` | NAS 0.7.0, Acea, Summerfirefly | CC BY-SA 4.0 | full mod, rebuilt from SpaceDock ("rebuild") | SA allows it. The bundled ModuleManager and Firespitter are dropped. Replaces `NAS` |
| `SkizzyNaval-LargeBoatPartsPack` | Large Boat Parts Pack, SM Industries | CC BY-SA 4.0 + GPL-3.0 | full pack, rebuild | Both allow it. `SMIndustries.dll` (Firespitter code, no redistribution) is removed |
| `SkizzyNaval-MaritimePack` | Maritime Pack, Fengist | CC BY-NC 4.0 | full pack, rebuild from GitHub | NC allows adapted copies; not on CKAN |
| `SkizzyNaval-AdvancedWeaponsPack` | Advanced Weapons Pack | MIT | full pack, rebuild | CKAN's package needs NorthKerbinDynamics (KSP 1.3.1) and can't install on 1.12.5 |
| `SkizzyNaval-MNWS` | [MNWS] Modern Naval Weapon System 2.4, Johnny005611 | CC BY-SA 4.0 | full pack, rebuild | CKAN's package targets KSP 1.4.4 and BDArmory Continued. Replaces it |
| `SkizzyNaval-IronSeasKANES` | Iron Seas KANES Fighting Ships 1, SkunkWorksCommand | GPL-2.0 (plugin: MIT) | full pack, committed (bundle) with diff, patch scripts and plugin source | GPLv2 allows modified copies; not on CKAN. Licence text added. Our SkizzyRig plugin (MIT) gives the guns turret transforms |
| `SkizzyNaval-AircraftCarrierAccessories` | Aircraft Carrier Accessories | MIT (patch: MIT) | overlay, depends on `AircraftCarrierAccessories` | One added patch, and the original is on CKAN |
| `SkizzyNaval-FishyWeapons` | Fishy Weapons, Fishy Mod Core | CC BY-SA 4.0 / restricted | overlay, depends on `FishyAntiMissileSpam` | Fishy Mod Core forbids redistribution; both are on CKAN |
| `SkizzyNaval-TitansBDArmoryExtension` | TBAE | unclear (SpaceDock says MIT, but the models are BDArmory's) | overlay, original installed **by hand** | Licence unclear and not on CKAN. Suggested by the pack only |
| `BDArmory-Skizzy` | BDArmory Plus 1.13.1.0, BahamutoD, Papa_Joe, DocNappers and others | CC BY-SA 2.0 | upstream release zip with our `BDArmory.dll` and `en-us.cfg` | SA allows it. Provides and conflicts with `BDArmory`; optional alternative |
| `SubTrim` | SubTrim, Skizzy-create | MIT | external: released from [Skizzy-create/SubTrim](https://github.com/Skizzy-create/SubTrim); only its `.ckan` is generated here | Own repository and releases |
| `SkizzyNavalPack` | metapackage | MIT | `.ckan` only | |

The pack depends on every module in the table except TBAE (suggested) and BDArmory-Skizzy
(an alternative). Each `modules/<name>/README.md` explains the decision
in more detail, and `CHANGES.md` lists every change.

### What "rebuild" means

The original art is large, and some of it is only licensed as part of the original download,
so it is not committed. For a rebuild module, `modules/<name>/` holds only:

- our new and changed files under `GameData/`;
- `delete.txt`, the files to remove;
- `manifest.json`, the size and SHA-256 of every file in the finished mod.

At release time CI downloads the exact original archive (pinned by SHA-256 in
`registry.toml`), applies the changes and refuses to ship unless the result matches the
manifest.

## Repository layout

```
registry.toml              one [[module]] entry per module (CKAN metadata, upstream source, strategy)
modules/<key>/             GameData/ (shipped files), CHANGES.md, LICENSE*, README.md, ATTRIBUTION.md,
                           manifest.json, delete.txt, src/ (plugin source)
scripts/skizzy.py          build tool: check, import, package, ckan, validate, verify-downloads, notes
scripts/skizzylib/         its library
.github/workflows/ci.yml       checks on every push and pull request
.github/workflows/release.yml  tag v* -> zips, GitHub release, .ckan files, push to skizzy-ckan
```

## Building locally

Python 3.11 or newer:

```sh
pip install -r scripts/requirements.txt
python scripts/skizzy.py check                       # registry, manifests, sizes, forbidden libraries
python scripts/skizzy.py package --version 0.0.0     # downloads originals, writes dist/*.zip
python scripts/skizzy.py ckan --version 0.0.0        # writes dist/ckan/<id>/<id>-<ver>.ckan
python scripts/skizzy.py validate dist/ckan
```

`--archive KEY=path.zip` uses an archive you already have. The tool checks it against the
pinned hash. `RELEASING.md` covers the release flow, adding a module and the token secret.

## Known issues

- No in-game testing was done for these builds. Each module's `CHANGES.md` has a test
  checklist.
- **Iron Seas** guns aim and fire through invisible turret transforms (SkizzyRig), but the gun
  meshes don't turn: each original gun model is one merged mesh.
- **TBAE** needs the original installed by hand. One upstream file needs a manual fix; see
  `modules/TitansBDArmoryExtension/not-shipped.txt`.
- **Maritime Pack** includes `FSResizer.dll` (Allis Tauri), as the original does. It is
  flagged in its README.
- **BDArmory-Skizzy**: mods that depend on `BDArmoryForRunwayProject` by name (for example
  NKD Reborn Extended) can't be installed alongside it through CKAN.

## Licence

See `LICENSE.md`. Each module keeps its original licence (its `LICENSE*` file is also inside
its release zip, under `SkizzyNaval-docs/`). The build scripts and workflows are MIT.
