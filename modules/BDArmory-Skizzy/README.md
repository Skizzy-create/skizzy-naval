# BDArmory Plus (Skizzy fork build)

CKAN: `BDArmory-Skizzy` | strategy: **rebuild** | licence: **CC BY-SA 2.0** (inherited)

BDArmory Plus 1.13.1.0 with the plugin from
[Skizzy-create/BDArmory `multitarget-1.13.1`](https://github.com/Skizzy-create/BDArmory/tree/multitarget-1.13.1)
at commit `01d0c9676585731e6fe9df85ee2be0ab802a20bf` (multi-target, parallel salvo,
depth-charge fix).

**How it is built.** CI downloads the upstream BDArmory Plus 1.13.1.0 release zip (the file
CKAN lists for `BDArmoryForRunwayProject` 2:v1.13.1.0, SHA-256 checked), keeps its whole
layout, swaps in the two files from `GameData/` here and checks all 653 files against
`manifest.json`.

| Path | Contents |
|---|---|
| `GameData/BDArmory/Plugins/BDArmory.dll` | Fork build (FileVersion 1.13.1.0), SHA-256 `C0C5B82F...75A8` |
| `GameData/BDArmory/Localization/UI/en-us.cfg` | Upstream strings plus the fork's new UI strings |
| `manifest.json` | All 653 files of the finished `GameData/BDArmory` |

**CKAN metadata.** Same install directives as upstream (`find: BDArmory` to GameData,
`find: craft` to `Ships/SPH`) and the same dependencies (ModuleManager, PhysicsRangeExtender).
`provides: [BDArmory]` and `conflicts: [BDArmory]`, so CKAN swaps it for
BDArmoryForRunwayProject. The version is `v1.13.1.0-skizzy.<release>`.

**Known side effect.** Some mods depend on `BDArmoryForRunwayProject` by name rather than on
`BDArmory`, e.g. NorthKerbinDynamicsRenewedExtended (installed here). When CKAN replaces
BDArmoryForRunwayProject with this build, it will also offer to remove those mods. Adding
`"BDArmoryForRunwayProject"` to `provides` in `registry.toml` would keep them installed.

**Why not a CI compile.** Building BDArmory.dll needs KSP's `Assembly-CSharp.dll` and Unity
assemblies, which are proprietary and can't be downloaded by a public GitHub runner. A
self-hosted runner on a machine with KSP installed could do it. Until then, build locally and
update the module:

1. Build the fork (`dotnet build -c Release` in the BDArmory repo).
2. Copy `BDArmory.dll` (and `en-us.cfg` if strings changed) into `GameData/BDArmory/...` here.
3. `python scripts/skizzy.py manifest BDArmory-Skizzy`, update the commit hash in
   `registry.toml` and this file, commit, tag.

Set `enabled = false` in `registry.toml` to stop releasing it.
