# NAS 0.7.0: unofficial update for KSP 1.12.5 + BDArmory Plus 1.13

This is an unofficial community update. NAS 0.7.0 ("Land of Dragon") was built for KSP 1.4.3 and BDArmory Continued 1.2. This update ports its configs and its plugin (`AntiSubmarineWeapon.dll`) to KSP 1.12.5 and BDArmory Plus 1.13.x (BrettRyland). The plugin was compiled against BDArmory 1.13.1.0. NAS and ASW are licensed CC BY-SA 4.0, and this adaptation keeps that licence.

## Layout

| Path | What |
|---|---|
| `GameData/NAS/` | Patched mod folder (drop-in replacement for the 0.7.0 `GameData/NAS`) |
| `src/AntiSubmarineWeapon/` | Plugin source plus SDK-style `.csproj` (net48) |
| `src/AntiSubmarineWeapon.dll` | Built plugin (identical to `GameData/NAS/Plugins/AntiSubmarineWeapon.dll`) |
| `diffs/NAS-0.7.0-GameData_to_KSP1.12.5-BDA1.13.diff` | All config/text changes against the 0.7.0 release (`git apply`, paths `a/GameData/NAS/...`; the DLL is binary and not in the diff) |
| `diffs/ASW-c640ea3_to_0.6.6-BDA1.13.diff` | Plugin changes against https://github.com/Summerfirefly/ASW commit `c640ea3` (`git apply` in the repo root) |

All three diffs were checked with `git apply`. Applied to the originals, they reproduce the patched trees exactly; the DLL is the only exception.

## Dependencies

| Dependency | Status |
|---|---|
| BDArmory Plus 1.13.x | Required |
| ModuleManager 4.x | Required. The bundled **ModuleManager 3.0.6 was removed**. |
| Firespitter 7.17+ | Needed only by `NAS_ShipMagazine` and `NAS_UnivAmmoBox` (`FSfuelSwitch`). The bundled **Firespitter 7.6 (KSP 1.3) was removed**. |
| TweakScale | Optional. `MMPatches/BDExplosiveTweakScale.cfg` is unchanged and targets `TWEAKSCALEEXPONENTS[BDExplosivePart]`, which current TweakScale does not define, so it does nothing. |
| KIS | Optional. Two parts carry `ModuleKISItem`; without KIS this only adds a log line. |

Remove the old `GameData/NAS` before installing, and delete any `ModuleManager.3.0.6.dll` or old `Firespitter` folder that came from the NAS zip.

## Bullets (`BulletDefs/NAS_Bullets.cfg`, all 46 rewritten)

BDArmory 1.13 replaced the BDAc 1.2 bullet keys. For every bullet:

* Removed `blastPower`, `blastHeat` and `blastRadius`. All three were 0 in NAS 0.7.0, so nothing is lost.
* **Fixed `apBulletMod = 0`.** In BDArmory 1.13 penetration is multiplied by `apBulletMod` with no guard, so 0 meant 0 mm penetration for every NAS shell. This was the real "shells do nothing" bug. HE now uses `0.8` and AP/ball uses `1.0`, both BDArmory's own conventions.
* Added `DisplayName`, `incendiary = False`, `projectileCount = 1`, `fuzeType`, and `projectileColor`/`fadeColor`/`startColor`. BDArmory 1.13 reads tracer colours from the bullet, not the weapon, so the colours each weapon used are copied here: small-calibre autocannon (≤ 40 mm) fade, large calibres are solid yellow.
* `explosive = true` became `explosive = Standard`, and `false` became `None`.
* `9mmBullet` was renamed **`9mmBulletNAS`**, because BDArmory ships its own `9mmBullet` and skips duplicate names. `NAS_FPS_StenSMG_BA` was updated to match.

`tntMass` was **not** changed. Every HE shell already had a `tntMass`; the other 27 bullets are AP or ball rounds that never had explosive data, so they stay kinetic (`tntMass = 0`, `fuzeType = None`).

**Fuzes.** Old `airDetonation`/`airDetonationTiming` lived on the weapon. In 1.13 the fuze belongs to the bullet. Bullets fired by guns that had `airDetonation = true` get `fuzeType = Timed`: the shell bursts at the predicted target range (`defaultDetonationRange` is set from the target distance by the weapon) and still detonates on impact. The other HE bullets get `Impact`.

| Fuze | Bullets |
|---|---|
| Timed (13) | 37mmFlaKGER, 37mmFlaKITA, 37mmFlaKUSSR, 40mmHE2pdr, 40mmHEBofors, 3inchHELong, 90mmHEITA, 100mmHEJAP, 105mmHEGER, 4p5inchHE, 5inchHEUS, 6inchHEUK, 6inchHEUS |
| Impact HE (6) | 25mmHE, 3inchHEShort, 5inchHEJAP, 105mmHECHN, 130mmHEUSSR, 138-140mmHE |
| None, kinetic (27) | all AP/CPBC/ball rounds |

`Flak` (proximity plus timed backup) would also suit the AA shells, but BDArmory's proximity fuze does not check team and will also burst next to friendly ships, so `Timed` was chosen. `5inchHEUS` is shared by the US 5"/38 mounts and all six Iron Seas guns, so those dual-purpose guns burst at target range against ships as well. Per-bullet `tntMass` is the explosive filler; BDArmory's blast radius is 14.8 × tntMass^(1/3) m (5inchHEUS 3.135 kg ≈ 21.7 m).

## Weapons (`ModuleWeapon`)

* `airDetonation`, `airDetonationTiming` and `maxAirDetonationRange` were removed from the 15 weapons that had them, and a comment points to the bullet fuze. Those weapons: NAS_AA_DD_100x2_IJN, NAS_AA_MG_37Breda_RM, NAS_AA_MG_37SKC30_KM, NAS_AA_MG_40Boforsx2_RN, NAS_AA_MG_40Boforsx4_USN, NAS_AA_MG_66K_SN, NAS_AA_MG_QF2pdrx8_RN, NAS_AA_SC_105SKC33_KM, NAS_AA_SC_3inchMark21_USN, NAS_AA_SC_90A1938_RM, NAS_CL_152x3Mk16_USN, NAS_CL_BL6inMk21x2_ROCN, NAS_DD_127_USN, NAS_DD_127x2_USN, NAS_DD_QF45_RN. `maxAirDetonationRange` has no 1.13 equivalent.
* `defaultAirDetonationRange` became `defaultDetonationRange` (NAS_DD_QF45_RN). BDArmory also uses it to size the shell lifetime.
* `weaponType = cannon` became `ballistic` on 22 weapons. `cannon` is deprecated and some 1.13 code paths only check for `"ballistic"`.

## Radars (`ModuleRadar`)

* Removed `rangeIncrements` (1.13 derives display ranges from `radarDetectionCurve`), plus `slaveTurrets` and `boresightScan` (now runtime state, not config).
* `canRecieveRadarData` became `canReceiveRadarData`.
* **NAS_RA_FuMO61_KM:** removed the second `canLock = true`. KSP keeps the last duplicate key, so it silently overrode `canLock = false`; the 0.6.6.1 changelog says FuMO 61 cannot lock.
* Mk 25 FCS and 21-Go already had `radarLockTrackCurve`, so no curve was needed.

## Bombs, torpedoes, depth charges

* **ModuleKineticBombarder** (North Kerbin Weaponry) was removed from NAS_AB_FritzX_LW, NAS_AB_Tallboy12klb_RAF, NAS_AB_Type99No80_IJN and NAS_AB_AN-Mk1_USN. No KSP 1.12 build of that plugin exists. Each bomb still has its `BDExplosivePart` warhead.
* Removed `isTubeLoaded` (no longer in 1.13) from NAS_TO_Mark15_USN and NAS_TO_Type93_IJN.
* **Depth charges** (NAS_DC_Mark9_USN, NAS_DC_Type3_IJN) got:
  * a BDArmory-native depth fuze as a backstop at the plugin's `maxDepth`: `altitudeFuzeMode = -2` (DescendingMSL) and `altitudeDetonationAlt = -500` / `-200`;
  * an explicit AI envelope: `engageSLW = true`, air/missile/ground off, `minStaticLaunchRange = 10`, `maxStaticLaunchRange = 300`.
* Removed the empty `BDACategoryModule` from 4 parts. It no longer exists in BDArmory 1.13; BDArmory now categorises parts by their modules.

## Plugin: AntiSubmarineWeapon.dll 0.6.6.0

The plugin is ported, not replaced. The BDArmory surface it uses is small: `MissileLauncher`, `HasFired`, `Detonate()` and warhead arming. Class names are unchanged (`ModuleAntiSubmarineWeapon`, `ModuleRotor`, `FlagWave`, `NASCategoryModule`, `ModuleDepthChargeProjector`), so existing part configs and craft keep working.

The source is Summerfirefly/ASW `c640ea3`, which matches the 0.6.5.2 DLL that shipped with NAS 0.7.0. The one gap is `FlagWave` (used by `NAS_Flag`), which is not in the repository; it was reconstructed from the shipped DLL.

**Build**

* SDK-style `AntiSubmarineWeapon.csproj`, `net48` (BDArmory 1.13 targets 4.8), C# 9.
* References KSP `Assembly-CSharp`, `Assembly-CSharp-firstpass`, `UnityEngine` (Core, Physics, IMGUI, Animation, UI) and `BDArmory.dll`, all `Private=false`.
* Paths can be overridden: `dotnet build -c Release -p:KSPDir="C:\KSP" -p:BDArmoryDll="...\BDArmory.dll"`.
* Adds `KSPAssembly("AntiSubmarineWeapon", 0, 6, 6)` and `KSPAssemblyDependency("BDArmory", 1, 0)` so KSP loads it after BDArmory.

**API port**

* `using BDArmory.Parts` became `BDArmory.Weapons.Missiles` / `BDArmory.Weapons`.
* `FlightGlobals.getUpAxis()` with no arguments (the active vessel's up axis) became the up axis at the weapon's own position.
* Altitude is read from the part, not the vessel root.
* The rigidbody comes from `part.rb`.

**Behaviour fixes**

* **Depth charges keep their per-part depth.** 0.7.0 overwrote every charge's "Destroy Depth" with the toolbar value (25 m) on load. The value is now only clamped to 25…maxDepth. The toolbar window still sets every unfired charge or torpedo when moved.
* **Depth charges wait for warhead arming.** BDArmory 1.13 keeps a missile's warhead disarmed until it is 1.25 × its blast radius from the launching vessel: Mk 9 ≈ 84 m, Type 3 ≈ 100 m. `Detonate()` on a disarmed warhead deletes the charge silently. The plugin now keeps the charge sinking until it is armed, then detonates.
* **Cancelled BDArmory's false "miss" self-destruct.** BDArmory 1.13 runs its missile miss check on unguided weapons too. It places a dummy target 5 km ahead of the launch point; once the weapon points away from it or passes it, the weapon is flagged as missed and self-destructs 1.5 s later. A depth charge can trip this right after entering the water. The plugin clears that timer for NAS ASW weapons, unless the part config itself sets `isTimed`.
* Removed the `part.temperature = maxTemp*2` hack after `Detonate()`. 1.13's `Detonate()` destroys the part itself.
* Torpedo depth keeping and buoyancy only run after BDArmory has launched the torpedo (`HasFired`).
* `zeroThrustSink` (true in every NAS torpedo cfg, previously unused) now stops depth keeping once the motor has burnt out (`PostThrust`), so spent torpedoes sink.
* **ModuleRotor:** `RotateTransformName`, `IsForwardRotation`, `rotateRPM` and `torpedo` had no `[KSPField]`, so the cfg values (e.g. 1003 RPM, reverse rotation on the Mark XII) were ignored and every propeller spun at 30 RPM. Also fixed `FixedUpdate` throwing NullReferenceExceptions in the editor (no vessel there).
* `volume` is accepted as an alias for the misspelt `volum`, because NAS cfgs use `volume`.
* ModUiManager: `OnDestory` typo fixed to `OnDestroy`, so the GameEvents handlers are now removed; null guards for `ActiveVessel` and for parts without a `MissileLauncher`.
* Toolbar button: the event handler is removed on destroy, and the button is created when the launcher is already ready.
* NAS editor subcategory: the "Filter by Function" category is looked up by `#autoLOC_453547` (as BDArmory does), with the old English name as fallback; the addon survives scene changes.
* FlagWave: made public and null-safe if a flag mesh is missing.
* PID: no NaN when `deltaTime` is 0.
* The "Neither depth charge nor torpedo" warning is logged once at start, not every physics frame.

## Corrections to the task brief

* `airDetonation` appeared in **15** NAS weapon configs plus 6 in Iron Seas, not 45.
* All explosive shells already had `tntMass`. The zero-damage problem was `apBulletMod = 0`.
* There is no hedgehog part. The ASW parts are 2 depth charges (Mk 9, Type 3) and 4 torpedoes (Mk 15, Type 93, aerial Type 91, aerial Mark XII), plus the depth-charge rack, which has no modules beyond HitpointTracker.

## Not verified (no in-game test was possible here)

* All in-game behaviour. Config keys were checked against the BDArmory 1.13 KSPField list (no unknown keys remain); bullet names and ammo resources resolve against the installed GameData; brace balance was checked.
* Timed fuze effectiveness of the AA guns, and how the dual-purpose 5" guns perform against ships with a Timed fuze.
* Plugin: torpedo PID tuning under 1.13 water physics, depth-charge sink rate and arming delay, the miss-timer cancel, and the toolbar window.
* AI depth-charge use: the BDArmory AI only drops them on submerged targets at least one blast radius away (67–80 m). Rack charges land near the dropping ship, so AI attacks will often miss. Player-controlled drops are the intended use.
* Interaction with other naval mods' buoyancy or water-drag tweaks.

## In-game test checklist

1. KSP.log after loading: no `[BDArmory.BulletInfo]` warnings for NAS bullets; no "Cannot find PartModule" for NAS parts (KIS ones are expected without KIS); `AntiSubmarineWeapon` listed as loaded, version 0.6.6.0.
2. Editor: the NAS subcategory appears under Filter by Function. Weapon info for the 5"/38 shows "Bullet type: 5in HE (US)", a Timed fuze and non-zero penetration.
3. Gun test: put a 5"/38 and a 16" turret on a barge and fire at a target ship. Hits should do damage and AP rounds should penetrate armour. Fire the 5"/38 at a plane: shells should burst near it.
4. Depth charges: set "Destroy Depth" to 60 m in the editor and launch. The setting should survive the launch. Drop a charge while moving at 10+ m/s: it should detonate near 60 m (±2 m), or deeper if it was still inside the arming distance. The log shows `DepthCharge Bombed At`.
5. Torpedoes: fire a Mk 15 at 5 m cruise depth from a stationary ship. It should hold depth, its propeller should spin fast, and it should not self-destruct after about 5 km; it sinks after its run. Check that the toolbar depth window changes unfired weapons.
6. AI: an AI destroyer with a Mk 25 FCS and 5"/38 guns against an AI destroyer. Radar should lock and slave the turrets, and the ships should damage each other.
