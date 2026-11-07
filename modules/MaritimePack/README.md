# Maritime Pack (SkizzyNaval NRE-fix build)

CKAN: `SkizzyNaval-MaritimePack` | strategy: **rebuild** | licence: **CC BY-NC 4.0** (inherited)

**Licence decision.** The pack and Fengist's plugins (the MPHover repository carries the same
licence for MPUtils) are CC BY-NC 4.0: adapted material may be shared with attribution, for
non-commercial use. So the full patched pack is shipped. The Maritime Pack is not on CKAN, so
this is the only CKAN source for it.

**How it is built.** The original art is not committed. At release time CI downloads the
upstream archive listed in `registry.toml`, checks its SHA-256, extracts the listed folders,
deletes the paths in `delete.txt`, copies `GameData/` from this folder on top, and checks
every resulting file against `manifest.json`. If any byte differs, the release fails.
Local build: `python scripts/skizzy.py package --version 0.0.0 --only MaritimePack`.

Upstream: the GitHub archive of `Fengist/Maritime-Pack` at commit
`2822417dcaac9ed1cd331546c5fa74951470a4cd` (= `main`). GitHub archives aren't guaranteed to be
byte-stable, so the archive itself has no hash; the per-file manifest check covers it.

| Path | Contents |
|---|---|
| `GameData/` | The four rebuilt plugins (`MPUtils.dll`, `MPEngineSounds.dll`, `MPHover.dll`, `FengistAnim.dll`) and `Maritime Pack/Patches/zz_MP_KSP1125_Compat.cfg` |
| `delete.txt` | `MPUtils/Plugins/PluginData/MPLog.dat` (the author's leftover log) |
| `manifest.json` | All 382 files of the finished build |
| `src/` | Patched C# sources and SDK-style projects for the four plugins |
| `upstream.diff` | Per-plugin diffs against the upstream sources |

**Bundled DLL note.** `MPUtils/Plugins/FSResizer.dll` ships unchanged from upstream. It is
Fengist's renamed copy of a resizer by Allis Tauri and 16 hull parts need it. It is not a
shared library available from CKAN, so it was kept. To drop it, add its path to `delete.txt`
and run `python scripts/skizzy.py manifest MaritimePack`.
