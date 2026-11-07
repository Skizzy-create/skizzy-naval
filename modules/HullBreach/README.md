# HullBreach (SkizzyNaval port)

CKAN: `SkizzyNaval-HullBreach` | strategy: **bundle** | licence: **CC BY-NC-SA 3.0** (inherited)

Full port of HullBreach 1.7.0.5 to KSP 1.12.5 and BDArmory Plus 1.13 (internal version
1.8.0): a new flood model, the BDArmory 1.13 damage API, a vessel module instead of per-part
updates, and patches for 333 parts. See `CHANGES.md`.

**Licence decision.** CC BY-NC-SA allows sharing adapted material with attribution, under the
same licence, for non-commercial use. The port is small (1.5 MB), so it is committed in full.

| Path | Contents |
|---|---|
| `GameData/HullBreach/` | The installable mod: `Plugins/HullBreach.dll`, `Patches/`, `Settings.cfg`, the pump part |
| `src/` | Plugin source from branch `port/ksp1.12-bda1.13` (SDK-style csproj, net48), `Tests/FloodModelSim`, `Tools/gen_hb_patches.py` |
| `CHANGES.md` | Changes against 1.7.0.5, the build command and the in-game test checklist |
| `manifest.json` | SHA-256 of every shipped file (checked by CI) |

**CKAN.** Provides and conflicts with `HullBreach`, so it replaces the original. Depends on
ModuleManager and BDArmory Plus 1.13 (`BDArmoryForRunwayProject` >= 1.13) or `BDArmory-Skizzy`.

**After rebuilding the DLL**, copy it to `GameData/HullBreach/Plugins/` and run
`python scripts/skizzy.py manifest HullBreach`.
