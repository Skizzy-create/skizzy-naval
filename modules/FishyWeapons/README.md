# Fishy Weapons: BDArmory Plus 1.13 patch

CKAN: `SkizzyNaval-FishyWeapons` | strategy: **overlay** | licence: **CC BY-SA 4.0**

**Licence decision.** Fishy Weapons is CC BY-SA 4.0 and could be shipped in full, but it
needs Fishy Mod Core, whose licence forbids copying or re-uploading without the author's
permission, so Fishy Mod Core is never redistributed. The patch only adds one file and both
originals are on CKAN (the downloads match `FishyAntiMissileSpam` 1:0.10.0 and
`Fishy-Mod-Core` 1.1.5a byte for byte), so this module ships just the patch and depends on
`FishyAntiMissileSpam`, which pulls in Fishy Mod Core.

**How it is built.** `GameData/` holds only the files this patch adds. The zip contains just
those files; CKAN installs them next to the original mod, which users get separately. No
original file is replaced, so there is no file conflict with the original.

| Path | Contents |
|---|---|
| `GameData/fishy_weapons_dev/Patches/zz_FishyWeapons_BDA113_Compat.cfg` | All BDA 1.13 fixes (bullets, fuzes, ammo, assets, part-name clash) |
| `upstream.diff` | Adds the patch file |
| `LICENSE-upstream-FishyWeapons.txt` | Fishy Weapons' own licence note |
