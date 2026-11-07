# NAS - Naval Artillery System (SkizzyNaval update)

CKAN: `SkizzyNaval-NAS` | strategy: **rebuild** | licence: **CC BY-SA 4.0** (inherited)

NAS 0.7.0 configs and the AntiSubmarineWeapon plugin ported to KSP 1.12.5 and BDArmory Plus
1.13. See `CHANGES.md`.

**Licence decision.** NAS and ASW are CC BY-SA 4.0, which allows redistributing the modified
mod with attribution under the same licence, so the full patched `GameData/NAS` is shipped.
The NAS zip also bundles ModuleManager 3.0.6 and Firespitter 7.6; neither is shipped (only
`GameData/NAS` is extracted).

**How it is built.** The original art is not committed. At release time CI downloads the
upstream archive listed in `registry.toml`, checks its SHA-256, extracts the listed folders,
deletes the paths in `delete.txt`, copies `GameData/` from this folder on top, and checks
every resulting file against `manifest.json`. If any byte differs, the release fails.
Local build: `python scripts/skizzy.py package --version 0.0.0 --only NAS`.

Upstream: NAS 0.7.0 from SpaceDock, the exact file CKAN lists for `NAS` 1:v0.7.0
(SHA-256 `94BD6789...F1A5`), with the archive.org mirror as a fallback.

| Path | Contents |
|---|---|
| `GameData/NAS/` | Only the changed files: 41 configs/texts plus `Plugins/AntiSubmarineWeapon.dll` 0.6.6.0 |
| `delete.txt` | Upstream files removed (none) |
| `manifest.json` | All 378 files of the finished `GameData/NAS` with their SHA-256 |
| `src/AntiSubmarineWeapon/` | Plugin source (SDK-style csproj, net48, C# 9) |
| `diffs/` | `git apply` diffs against NAS 0.7.0 and against Summerfirefly/ASW `c640ea3` |

**CKAN.** Provides and conflicts with `NAS` (the original's metadata targets KSP 1.2.9-1.4.3 and
BDArmory Continued). Recommends FirespitterCore (needed only by the two magazine parts'
`FSfuelSwitch`).
