# Large Boat Parts Pack (SkizzyNaval compatibility build)

CKAN: `SkizzyNaval-LargeBoatPartsPack` | strategy: **rebuild** | licence: **CC BY-SA 4.0** (parts) and **GPL-3.0** (SM_Industries)

**Licence decision.** The three `LShipParts*` folders are CC BY-SA 4.0. `SM_Industries` is
GPL-3.0; its notice also quotes Firespitter's "no redistribution of the whole pack" terms,
but those apply only to the texture-switch code inside `SMIndustries.dll`, which this build
removes. Both licences allow redistributing the modified pack, so the full pack is shipped.

**How it is built.** The original art is not committed. At release time CI downloads the
upstream archive listed in `registry.toml`, checks its SHA-256, extracts the listed folders,
deletes the paths in `delete.txt`, copies `GameData/` from this folder on top, and checks
every resulting file against `manifest.json`. If any byte differs, the release fails.
Local build: `python scripts/skizzy.py package --version 0.0.0 --only LargeBoatPartsPack`.

Upstream: LBP v3.9.2 from SpaceDock, the exact file CKAN lists for the three
`LargeBoatPartsPack*` modules (SHA-256 `B174A2E0...7740`), archive.org mirror as fallback.

| Path | Contents |
|---|---|
| `GameData/LShipPartsRequired/` | `LBP_tweakableComplete.cfg`, `LBP_Hangar_MM.cfg` (edited) and `Patches/zz_KSP1125_BDA113_Compat.cfg` (new) |
| `delete.txt` | `SMIndustries.dll` and the two archived DLL zips |
| `manifest.json` | All 505 files of the finished build |
| `upstream.diff` | Unified diff against the original folders |

**Install layout.** The four folders go to the GameData root, as the original zip is meant to
be used. CKAN's own LBP metadata installs into `GameData/LShipParts/` and leaves out
`SM_Industries`, which breaks the localised part titles and HullBreach's
`NEEDS[LShipPartsModern]` patches. This module provides and conflicts with
`LargeBoatPartsPack`, `LargeBoatPartsPack-Modern` and `LargeBoatPartsPack-WW2`.
