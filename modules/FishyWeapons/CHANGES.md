# Fishy Weapons 0.10.0 (+ Fishy Mod Core 1.1.5a): KSP 1.12.5 / BDArmory Plus 1.13 compatibility

## Install

Copy both folders from `GameData\` into KSP's `GameData\`:

- `fishy_weapons_dev\`: Fishy Weapons, unchanged apart from the new `Patches\` folder.
- `Fishy-Mod-Core\`: Fishy Mod Core, byte-identical to the 1.1.5a download. Fishy Weapons uses its sounds and textures.

You need ModuleManager and BDArmory Plus 1.13, both already installed. No DLLs are shipped; neither pack has any. No ModuleManager or Firespitter copies are included.

## What changed

All fixes are in one ModuleManager file:
`GameData/fishy_weapons_dev/Patches/zz_FishyWeapons_BDA113_Compat.cfg`.
No original file is edited. `upstream.diff` adds this file to the pack.

### 1. BULLET definitions (all 52)

Every Fishy bullet lacked 7 of the keys BDA 1.13's `BulletInfo` reads (`Ammo/BulletInfo.cs`, `ParseField`). That logged 364 "Using default value" warnings at every start. All 52 already had `tntMass`, so HE damage is fine.

| key | value added (`&` = only if missing) | why |
|---|---|---|
| `incendiary` | False | BDA default |
| `fuzeType` | None | On an HE round BDA treats None as an **Impact** fuze, the same as the old behaviour |
| `guidanceRange` | -1 | BDA default (unguided) |
| `projectileCount` | 1 | BDA default |
| `projectileColor`, `startColor`, `fadeColor` | copied from the first Fishy `ModuleWeapon` that fires the bullet; BDA's def colours for the 12 bullets no weapon uses | In BDA 1.13, ballistic tracer colours come from the BULLET (`ModuleWeapon.ParseAmmoStats` overwrites the weapon's colours). Without these keys every Fishy gun had BDA's default orange tracers. This restores the colours the author put on each gun (e.g. the green active-protection tracers). |

The legacy `blastPower`/`blastHeat`/`blastRadius` keys are removed. BDA 1.13 has ignored them since it switched to `tntMass`.

None of the Fishy bullet or resource names clash with anything in the installed GameData (checked against all `BULLET` and `RESOURCE_DEFINITION` nodes).

### 2. Removed `airDetonation` converted to BULLET `fuzeType` (2 weapons)

BDA removed `ModuleWeapon.airDetonation`, `airDetonationTiming`, `maxAirDetonationRange` and `proximityDetonation`. Fuzing is now set per bullet with `fuzeType` (`BulletInfo.ParseTypes`).

| part | old ModuleWeapon keys | conversion |
|---|---|---|
| `anti missile system` (bullet `anti-missile`) | airDetonation=true, airDetonationTiming=false, proximityDetonation=true, maxAirDetonationRange=1800 | `fuzeType = Proximity`; `defaultDetonationRange = 1800` |
| `active_protection_sys` (bullet `active_laser_missile_BULLET`) | same, maxAirDetonationRange=100 | `fuzeType = Proximity`; `defaultDetonationRange = 100` |

- Both bullets are fired only by these parts, so changing them affects nothing else.
- "Proximity, no timer" maps directly to BDA's `Proximity` fuze.
- `maxAirDetonationRange` maps to `defaultDetonationRange`. BDA sets a round's lifetime from `max(maxTargetingRange, defaultDetonationRange) / velocity`, so rounds expire at roughly the old maximum airburst range instead of flying 3.5 km.
- `active_laser_missile_BULLET` had `bulletMass = 0.0`. It is now 0.0001 kg, which avoids a 0/0 in `PooledBullet`'s post-penetration mass calculation and changes nothing else.
- Both parts' `explModelPath` pointed at a `.png`. It now points at `BDArmory/Models/explosion/30mmExplosion`.

### 3. Broken ammo, bullet and resource references

A `bulletType` that doesn't exist leaves `bulletInfoList[i]` null, which throws a NullReferenceException in `ModuleWeapon.ParseAmmoStats`. An undefined `ammoName` or RESOURCE means the gun never has ammo.

| part | problem | fix |
|---|---|---|
| `7mm_fast_gun2` "death-ray-edition" | `bulletType`, `ammoName` and RESOURCE `7mm fast` don't exist (NRE) | `7mm-fast`, the existing bullet and resource of the same name, with the death-ray stats |
| `7mm_fast_gun` (Hu-7k) | fires `fast7mm` but carried `7mm-fast2` | carries `fast7mm` |
| `102XE` | carried undefined resource `102XE` (weapon uses `102mm`) | `102mm` |
| `322mm` | `ammoName = 322m` (undefined) | `322mm` |
| `3mmXE` | carried undefined `3mmHE` | `3mmXE` |
| `m74` | `ammoName = 74_RESOURCE` (undefined); 6th ammo type `m74_BULLET_N-XE` undefined (NRE when selected) | `m74_RESOURCE`, `74_BULLET_N-XE`. The duplicate `74_BULLET_XE` entry is kept so ammo-type indices saved in craft don't shift. |
| `FishyAmmoBox` | resources `12mmfullsteel`, `7mm fast`, `102HE` undefined | `12mmfullsteel-`, `7mm-fast`, `102mm` |

### 4. Missing or wrong-case assets

KSP's `GameDatabase.GetModel`/`GetAudioClip` compare URLs with `==`, so case matters.

| part(s) | was | now |
|---|---|---|
| `1shot`, `modern r-60 radar`, `nuclear powered aim-120` | `BDArmory/Models/exhaust/LargeExhaust` | `.../largeExhaust` |
| `1shot`, `modern r-60 radar` | `BDArmory/Models/CmSmoke/cmSmokeModel` | `.../CMSmoke/cmSmokeModel` |
| `7mm_fast_gun`, `7mm_fast_gun2`, `6mm shotgun` | `BDArmory/Parts/browninganm2/Sounds/fire` | `.../sounds/fire` |
| `102XE`, `2mm` | `Fishy-Mod-Core/Sounds/102mm_shot` (never shipped) | `BDArmory/Parts/m1Abrams/sounds/shot` (substitute) |
| `322mm` | `Fishy-Mod-Core/Sounds/322mm_shot` (never shipped) | `BDArmory/Sounds/Artillery_Shot` (substitute) |
| `47HE gun` | `Fishy-Mod-Core/Sounds/40mm` (never shipped) | `BDArmory/Parts/Sidam/shot` (substitute) |
| `modern aim-120 LR` | `Fishy-Mod-Core/Sounds/rocketBoom2_07` (never shipped) | `BDArmory/Sounds/rocketLoop`, like the other AIM-120s |
| `bloody-Alpha`, `emp bomb`, `modern emp missile` | `explModelPath = BDArmory/Models/explosion/fire4t` (a texture) | BDA's default explosion, which BDA already fell back to with an error |
| `R-23T` | `targetingType = infrared` (not a BDA type, so None; the missile could never lock) | `heat` |
| `radarDataTransmitterImproved` | MODEL `BDArmory/Parts/radarDataReceiver` is a folder, not a model, so **the part never loaded** | the pack's own `fishy_weapons_dev/Parts/radarDataReceiver/model` |
| `FlareFAST`, `FlareFASTER`, `chaffBIG` | the same model stacked twice; the 2nd swapped in `tex_flareDispenser`, which BDA no longer ships | one model. The chaff box uses BDA's `tex_chaffDispenser`. |

### 5. Parts that couldn't load, and a part-name clash

- **`Small-Bomb0000` ("mini bomb")**: its model block had no `MODEL` keyword and pointed at `Squad/Parts/FuelTank/miniFuselage/model`, which doesn't exist. With no `.mu` in the folder either, KSP logged "Cannot clone model" and dropped the part. The patch adds the correct stock mesh, `.../miniFuselage/Fuselage`.
- **`AV-R9_1` ("AV-R9 Winglet")**: no MODEL, no mesh, and no `.mu` anywhere in the pack, so it never loaded. There's nothing to restore it from, so it is deleted (`!PART[AV-R9_1]`). This removes the load error and changes nothing in game.
- **`bird strike missile x`** used the same part name as `bird strike missile`. KSP registers only the first-loaded part per name (`PartLoader.APFinderByName`), so craft loaded the wrong one. The X variant is renamed to `bird_strike_missile_x`. Existing craft containing either variant now load the normal `bird strike missile`. Before, both resolved to whichever of the two cfgs KSP loaded first, probably the X one, since "…missile X.cfg" sorts before "…missile.cfg". Re-place the X variant in such craft.

### Left alone (harmless or out of scope)

- Unknown keys BDA ignores: `terminalManeuvering`, `radarCrossSection`, `isTubeLoaded` (MissileLauncher).
- On `laser_gun182`, `graviticWeapon`/`impulseWeapon` are not read for lasers in BDA 1.13. BDA's own GravityGun uses the same keys, so this matches BDA itself; `Impulse`/`massAdjustment` still apply.
- `Partlist/PartCategories.cfg` (`CUSTOM_PARTLIST_CATEGORY`) is read by nothing installed.
- The `.version` files are not valid JSON. Nothing installed reads them.
- `.lnk`, `desktop.ini` and `list.log` are junk files that KSP ignores. They're kept so the folder matches upstream.
- Balance is unchanged: calibres like 300 mm on the "7 mm" rounds, huge `tntMass` joke weapons, and so on.

## Verification done (static only)

- All 52 BULLETs were checked against the BDA 1.13 `BulletInfo` keys, before and after patching. After patching, no keys are missing or unknown.
- Every PART was scanned for model, texture and sound paths (case-sensitive), `bulletType` and `ammoName` references, RESOURCE names, and module keys against BDA 1.13 KSPFields. After patching, only harmless unknown keys remain.
- The patch was run through a ModuleManager-subset simulator against the pack. Every patch block matched exactly the intended parts and bullets, and no edit missed its target.
- Part-name duplicates were checked within the pack and against the installed GameData.

## Unverified

- Not run in KSP. ModuleManager behaviour is inferred from MM's documented operators (`&`, `%`, `!`, node indices, `:HAS`).
- Substitute sounds are my choice; the author's sound files never existed.
- `defaultDetonationRange` is persistent, so craft saved before the patch keep their stored value (3500).
- The active-protection round's proximity radius is auto-calculated from `tntMass = 0.0001`, giving about 0.46 m (impact-like). You can raise it with the "Proximity Fuze Radius" slider in the editor.

## In-game test

1. Start KSP and open `KSP.log`. Check that:
   - `[BDArmory.BulletInfo]: Using default value ...` warnings for Fishy bullets are gone;
   - there are no `PartCompiler: Cannot clone model` lines for `radarDataTransmitterImproved`, `Small-Bomb0000` or `AV-R9_1`;
   - there are no `SoundUtils ... did not give a valid audioclip` lines for Fishy paths.
2. **Editor:** the "mini bomb", the improved radar data receiver and both bird strike missiles show up as separate parts.
3. **Tracers and ammo:** put the m74, 322mm, 102XE, 3mmXE and both 7mm fast guns on a craft.
   - Each gun shows ammo in its PAW.
   - On the m74, cycle all 6 ammo types; there should be no NRE.
   - Fire each gun. The sound plays and tracers use the gun's colours.
4. **CIWS:** put the anti missile system or active protection on a ship with a weapon manager and fire a BDA missile at it. The PAW shows "Proximity Fuze Radius", and rounds detonate near the missile.
5. **Missiles:** launch an R-23T at a hot target; it should lock with heat. Launch a 1shot or R-60 and check that exhaust and boost smoke render.
