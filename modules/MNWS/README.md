# [MNWS] Modern Naval Weapon System (SkizzyNaval update)

CKAN: `SkizzyNaval-MNWS` | strategy: **rebuild** | licence: **CC BY-SA 4.0** (inherited)

MNWS 2.4 ("Johnny") updated for KSP 1.12.5 and BDArmory Plus 1.13. See `CHANGES.md`.

**Licence decision.** MNWS ships a CC BY-SA 4.0 licence file (`Johnny/LICENSE.txt`), and CKAN
lists it the same way. That allows redistributing the modified pack with attribution under the
same licence. An overlay depending on the CKAN original was ruled out: CKAN's
`MNWSModernNavalWeaponSystem` targets KSP 1.4.4 only and depends on BDArmory Continued, which
conflicts with BDArmory Plus. So the full patched pack is shipped and replaces it (provides and
conflicts with `MNWSModernNavalWeaponSystem`).

**How it is built.** The 581 MB of original art is not committed. At release time CI
downloads MNWS v2.4 from SpaceDock (the exact file CKAN lists, SHA-256 `DD254150...D588`;
archive.org mirror as fallback), extracts `Johnny/` to `GameData/Johnny/`, copies the three new
files from `GameData/` here on top, and checks all 574 files against `manifest.json`.
Local build: `python scripts/skizzy.py package --version 0.0.0 --only MNWS`.

| Path | Contents |
|---|---|
| `GameData/Johnny/MMPatches/MNWS_BDA113.cfg` | Required compatibility and AI fixes |
| `GameData/Johnny/MMPatches/MNWS_BDA113_Balance.cfg` | Optional balance pass |
| `GameData/Johnny/Plugins/MNWSFix.dll` | Helper plugin (VLS launch transforms, hull bay links, SSBN002 door labels) |
| `src/MNWSFix/` | Plugin source and project (net48) |
| `manifest.json` | All 574 files of the finished `GameData/Johnny` |

Optional companions: Aircraft Carrier Accessories (tail hooks, reloadable rails), Firespitter.
