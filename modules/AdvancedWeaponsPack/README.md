# Advanced Weapons Pack (SkizzyNaval compatibility build)

CKAN: `SkizzyNaval-AdvancedWeaponsPack` | strategy: **rebuild** | licence: **MIT** (inherited)

**Licence decision.** MIT allows redistribution with the copyright notice, so the full pack is
shipped. The only change is one new patch file, so an overlay would normally do, but the CKAN
`AdvancedWeaponsPack` depends on `NorthKerbinDynamics`, whose only release targets KSP 1.3.1,
so CKAN can't install it on 1.12.5. This build replaces it (provides and conflicts with
`AdvancedWeaponsPack`) and recommends NKD Reborn instead.

**How it is built.** The original art is not committed. At release time CI downloads the
upstream archive listed in `registry.toml`, checks its SHA-256, extracts the listed folders,
deletes the paths in `delete.txt`, copies `GameData/` from this folder on top, and checks
every resulting file against `manifest.json`. If any byte differs, the release fails.
Local build: `python scripts/skizzy.py package --version 0.0.0 --only AdvancedWeaponsPack`.

Upstream: AWP 0.22 from SpaceDock, the exact file CKAN lists (SHA-256 `008C167E...E303`).

| Path | Contents |
|---|---|
| `GameData/Advanced Weapons Pack/Patches/zz_AWP_BDA113_Compat.cfg` | The new patch (Castle Bravo: ModuleShockwave to BDModuleNuke; BDArmory AI never selects it, manual use only) |
| `manifest.json` | All 47 files of the finished build |
| `upstream.diff` | Adds the patch file |
