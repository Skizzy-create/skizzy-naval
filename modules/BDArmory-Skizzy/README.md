# BDArmory Plus (Skizzy fork build)

CKAN: `BDArmory-Skizzy` | strategy: **rebuild** | licence: **CC BY-SA 2.0** (inherited)

BDArmory Plus 1.13.1.0 with the plugin from
[Skizzy-create/BDArmory `multitarget-1.13.1`](https://github.com/Skizzy-create/BDArmory/tree/multitarget-1.13.1)
at commit `ff5130c19ffbe0ee4dc0eacde1a5ca6d6a59cf8e`:

- multi-target missile engagement, parallel salvo firing, depth charges over submerged targets
  (since 0.1.0);
- memory-leak fixes for long battles (0.1.2): destroyed vessels pruned from the vessel module
  registry, fire and fired-missile lists; `sharedMesh` instead of mesh copies; material copies
  freed; radar event subscriptions no longer stacked; PhysicsRangeExtender 1.21.0's leaked
  GameEvents callbacks removed on scene start;
- the debris clean-up delay slider goes from 1 s to 900 s (1 s steps to 60 s, then 15 s steps)
  and no longer resets a longer delay set in `settings.cfg` (0.1.2).

**How it is built.** CI downloads the upstream BDArmory Plus 1.13.1.0 release zip (the file
CKAN lists for `BDArmoryForRunwayProject` 2:v1.13.1.0, SHA-256 checked), keeps its whole
layout, swaps in the two files from `GameData/` here and checks all 653 files against
`manifest.json`.

| Path | Contents |
|---|---|
| `GameData/BDArmory/Plugins/BDArmory.dll` | Fork build (FileVersion 1.13.1.0, Release, no debug info), SHA-256 `B70B1DED...B2F9` |
| `GameData/BDArmory/Localization/UI/en-us.cfg` | Upstream strings plus the fork's new UI strings |
| `manifest.json` | All 653 files of the finished `GameData/BDArmory` |

**CKAN metadata.** Same install directives as upstream (`find: BDArmory` to GameData,
`find: craft` to `Ships/SPH`) and the same dependencies (ModuleManager, PhysicsRangeExtender).
`provides` and `conflicts` list both `BDArmory` and `BDArmoryForRunwayProject`, so CKAN swaps
it for BDArmoryForRunwayProject and mods that depend on either name stay installable. The
version is `2:v1.13.1.0-skizzy.<release>`: epoch 2, the same as BDArmoryForRunwayProject's.

**Version constraints.** Because this build provides `BDArmoryForRunwayProject`, mods that
depend on it by name (e.g. NorthKerbinDynamicsRenewedExtended) accept it. A dependency with a
`max_version` below 1.13.1 would still not accept it.

**Why not a CI compile.** Building BDArmory.dll needs KSP's `Assembly-CSharp.dll` and Unity
assemblies, which are proprietary and can't be downloaded by a public GitHub runner. A
self-hosted runner on a machine with KSP installed could do it. Until then, build locally and
update the module:

1. Build the fork as Release without debug info and without its post-build deploy step, e.g.
   `MSBuild BDArmory.csproj -p:Configuration=Release -p:DebugType=None -p:DebugSymbols=false
   -p:PostBuildEvent=` (the post-build event copies the plugin into the KSP folder listed in
   `_LocalDev/ksp_dir.txt`).
2. Copy `BDArmory.dll` (and `en-us.cfg` if strings changed) into `GameData/BDArmory/...` here.
3. `python scripts/skizzy.py manifest BDArmory-Skizzy`, update the commit hash in
   `registry.toml` and this file, commit, tag.

Set `enabled = false` in `registry.toml` to stop releasing it.
