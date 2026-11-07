# Aircraft Carrier Accessories: KSP 1.12.5 / BDA 1.13 patch

CKAN: `SkizzyNaval-AircraftCarrierAccessories` | strategy: **overlay** | licence: **MIT**

**Licence decision.** ACA is MIT, which would allow a full copy. It isn't needed: the fix is
one new ModuleManager file, `ACA.dll` is unchanged, and the original is on CKAN (the
downloaded zip is byte-identical to CKAN's `AircraftCarrierAccessories` 1:1.9.1). Shipping
only the patch avoids a 136 MB duplicate and lets CKAN install the original alongside it.

**How it is built.** `GameData/` holds only the files this patch adds. The zip contains just
those files; CKAN installs them next to the original mod, which users get separately. No
original file is replaced, so there is no file conflict with the original.

| Path | Contents |
|---|---|
| `GameData/KFC/Patches/zz_KSP1125_BDA113_Compat.cfg` | Removes the Enterprise's Hangar modules when Hangar is absent; CLLS `Base` transform stopgap |
| `upstream.diff` | Diff against the original, including the suggested `ACA.dll` code fix as comments |

**CKAN note.** The main repository marks ACA 1.9.1 as KSP 1.10.0 only. It resolves on this
install because CKAN treats 1.8 to 1.12 as compatible here; other users may need to tick 1.10
under *Settings > Compatible game versions*.
