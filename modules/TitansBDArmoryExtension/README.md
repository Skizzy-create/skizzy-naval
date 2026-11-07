# Titans BDArmory Extension: BDArmory Plus 1.13 patch

CKAN: `SkizzyNaval-TitansBDArmoryExtension` | strategy: **overlay** (original is a manual install) | licence: **CC BY-SA 4.0** (our patch)

**Licence decision: unclear, so overlay only.** SpaceDock lists TBAE as MIT, but the archive
contains no licence file and its readme says the models are the BDArmory developers' work
(BDArmory is CC BY-SA 2.0, which can't be relicensed as MIT). Until that is clarified, only
our own patch file is shipped.

**Manual install.** TBAE is not on CKAN, so this module can't depend on it. Download
TBAE 0.1.4 from https://spacedock.info/mod/3399 and copy its `TBAE` folder into `GameData`.
The patch installs into `GameData/TBAE/Patches/`, which works whether TBAE is copied in
before or after.

**How it is built.** `GameData/` holds only the files this patch adds. The zip contains just
those files; CKAN installs them next to the original mod, which users get separately. No
original file is replaced, so there is no file conflict with the original.

**Not shipped** (`not-shipped.txt`): the one-line fix to `Parts/J-627/j627ares_wf.cfg`. An
overlay can't replace an original file. To apply it, edit line 1 of that file by hand to:

    @PART[j627ares]:NEEDS[Waterfall,StockWaterfallEffects]:AFTER[StockWaterfallEffects]

With Stock Waterfall Effects installed (as on this install) the game behaves the same either
way; the edit only matters without it.

If TBAE's licence is confirmed as MIT for the whole archive, switch this module to
`strategy = "rebuild"` (the SpaceDock URL and hash are already in `registry.toml`) and
re-import it with the edited file included.
