# Iron Seas KANES (Balao class fleet boat): unofficial update for KSP 1.12.5 + BDArmory Plus 1.13

## Layout and install

The original zip has two top-level folders, `BalaoClassFleetBoat` and `4BladeFastScrew`, meant to go straight into `GameData`. Here they are grouped under **`GameData/IronSeasKANES/`**, together with new `Patches/`, `BulletDefs/` and `Plugins/` folders, `LICENSE.txt` and `README.txt`. No config uses absolute paths, so the move is safe. If you installed the old version, delete `GameData/BalaoClassFleetBoat` and `GameData/4BladeFastScrew` first; otherwise every part loads twice.

`diffs/IronSeasKANES_to_KSP1.12.5-BDA1.13.diff` holds every change against the original zip contents (paths relative to the zip root / `GameData/IronSeasKANES`), including `Plugins/SkizzyRig.dll` as a git binary patch. It was checked with `git apply` on a clean copy of the original zip, and the result is byte-identical to this folder.

## Dependencies (looked up on 2026-11-07, nothing installed)

| Needed for | Mod / version found | KSP 1.12.5? |
|---|---|---|
| Deck guns | **NAS**, the patched 0.7.0 in `../NAS` (bullets `20mmAPLong` and `40mmHEBofors`, sounds, localisation, `NASCategoryModule`) plus **BDArmory Plus 1.13** (bullet `12.7mmBullet`; ammo `20x102Ammo`, `50CalAmmo`, `40x311Ammo` and `100mmShells`; sounds) | Yes, with the patched NAS |
| Ballast tank (`WBIBallastTank`), dive computer (`WBIDiveComputer`), SAS upgrades (`WBIFlightControl2/3`) | **WildBlueTools 1.90.6** (2025-07-18), `KSP_VERSION_MIN` 1.12.2. https://github.com/Angel-125/WildBlueTools (`.version` on master) | Yes |
| `WBIAirParkController` | **KerbalActuators 1.9.0** (KSP ≥ 1.12.2), bundled with WildBlueTools. https://github.com/Angel-125/KerbalActuators | Yes |
| Ballast FX (`WildBlueIndustries/Buffalo/FX/BallastVent`) | **Buffalo 2.12.0** (KSP ≥ 1.12.2). https://github.com/Angel-125/Buffalo. Cosmetic only; without it the log reports a missing model. | Yes |
| `BonVoyageModule` | **BonVoyage 1.4.1** (jarosm, 2022-10-02, built for 1.12.3; `.version` min = max = 1.12.3, so KSP-AVC will warn on 1.12.5). https://github.com/jarosm/KSP-BonVoyage, https://spacedock.info/mod/950/BonVoyage. Alternative: **Bon Voyage /L 1.5.1.3** (net-lisias-ksp, 2026-04-26, KSP ≥ 1.8). https://github.com/net-lisias-ksp/BonVoyage, https://spacedock.info/mod/3979/BonVoyage%20/L | 1.4.1 should work but is untested on 1.12.5; /L claims support |
| Screw reverse thrust (`FSswitchEngineThrustTransform`) | **Firespitter 7.17+** | Yes |
| Ballast fallback | **KerbalPowers Naval** (`Cavitation.dll`, `ModuleShipBallast`) | Yes |
| `IntakeLqd` ballast resource | CommunityResourcePack (already in your GameData) | Yes |
| Automatic trim / depth hold (optional) | **SubTrim 1.0** (`../SubTrim`) flags `BalaoClassHull` as a submarine and drives the WBI ballast, the KP fallback or virtual ballast | Yes |
| Deck gun turret transforms | **SkizzyRig 1.0.0**, included as `Plugins/SkizzyRig.dll` (written for this update, see "Turret rig" below) | Yes (built against the 1.12.5 assemblies) |

Every dependency for 1.12.5 exists, so the WBI, BonVoyage and Firespitter modules stay in the part configs. Fallbacks (below) cover the cases where they are not installed. Your current GameData has CRP, Firespitter and BDArmory, but **no WildBlueTools, KerbalActuators, BonVoyage, KerbalPowers Naval or NAS**. As installed today, the fallbacks would therefore make the boat a surface-only ship without guns. Install WildBlueTools (1.90.6) or KerbalPowers Naval to dive.

## Changes

### Deck guns (20mmFF, 20mmRF, 4inchFF, 4inchRF, 50Cal, Single40mm)

BDArmory 1.13 compatibility:

* `PART` became `PART:NEEDS[NAS,BDArmory]`, so ModuleManager drops the guns cleanly if NAS or BDArmory is missing. Without that they would load with an undefined bullet, missing sounds and a missing `NASCategoryModule`.
* Removed `airDetonation = true`, `airDetonationTiming = true` and `maxAirDetonationRange = 7500` (gone in BDArmory 1.13). The fuze now comes from each bullet's `fuzeType` (see the table).
* `weaponType = cannon` became `ballistic`.

Rebalance. All six were clones of the NAS 5"/38 (5" HE `5inchHEUS` at 30 rpm from `5/62Shell`, with the 5"/38 description). Each now matches its real weapon:

| Part | Weapon (`shortName`) | Bullet (fuze) | Ammo resource, amount | RPM | Effective / max targeting range | Turret (pitch / yaw speed, elevation) | Other |
|---|---|---|---|---|---|---|---|
| `20MMAAGunBalaoClassFront`, `...Rear` | 20 mm Oerlikon Mk 4 (`20mm Oerlikon`) | NAS `20mmAPLong` (none, solid AP) | `20x102Ammo`, 600 (BDArmory's stand-in for the 20×110 mm round) | 450 | 1500 / 4000 m | 60 / 90 °/s, -15 to +85° | 60-round drums: `BeltFed = false`, `RoundsPerMag = 60`, `ReloadTime = 8`, reload sound. Heat 168/shot, loss 1200/s, max 2400. `maxDeviation` 1.0 |
| `50CalGunBalaoClass` | .50 cal M2 Browning, water-cooled (`.50 cal M2 Browning`) | BDArmory `12.7mmBullet` (impact) | `50CalAmmo`, 1000 | 500 | 1800 / 3000 m | 60 / 90 °/s, -15 to +85° | Heat 100/shot, loss 1000/s (sustained fire). `maxDeviation` 0.4. 30 mm explosion FX |
| `40MMAAGunBalaoClass` | 40 mm Bofors L/60, Mk 3 single (`40mm Bofors Mk 3`) | NAS `40mmHEBofors` (Timed) | `40x311Ammo`, 200 | 120 | 2500 / 5000 m, `defaultDetonationRange` 2500 | 30 / 35 °/s, -15 to +85° | Heat as NAS's Bofors (205 / 1500 / 3000). `maxDeviation` 0.7 |
| `4InchGunBalaoClassForward`, `...Rear` | 4"/50 Mk 9 on a wet Mk 12 mount (`4in/50 Mk 9`) | **new** `IronSeas4in50HC` (Impact) | `100mmShells` (BDArmory "100 mm (4") Shell"), 60 | 9 | 6000 / 12000 m | 8 / 10 °/s, -15 to +20° | `engageAir = false` (surface gun). `maxDeviation` 0.4 |

* The forward and rear variants of the 20 mm and the 4" share a `shortName`, so the weapon manager treats each pair as one weapon group.
* New descriptions and tags on all six (no more NAS 5"/38 text). Sounds: 20 mm and .50 use BDArmory's `50CalTurret/sounds/shot`, the 40 mm uses `m230ChainGun/Sounds/m230shot` (as NAS's Bofors does), and the 4" keeps NAS `dd_fire`. The stale `5inchHEUS` comments and `//bulletVelocity = 792` are gone.
* Ammo mass changed with the ammo: per gun it was 0.82 t of `5/62Shell`. Now it is 0.16 t (20 mm), 0.12 t (.50), 0.42 t (40 mm) and 1.12 t (4"). The parts themselves still weigh 0.01 t (not changed).
* Every bullet, ammo resource, sound and explosion-model path was checked against BDArmory 1.13.1 (installed GameData), the patched NAS and this folder. The check found no missing names and no duplicate bullet names.

### Deck guns: turret rig (`Plugins/SkizzyRig.dll`, new)

**The problem.** None of the six gun models has a `Rotate`, `Pitch` or `fireTransform` transform. BDArmory logged `has no pitchTransform`, `Turret unlikely to function properly` and `Weapon missing fireTransform`, and the guns never turned or fired. Each model is **one merged mesh**: a single mesh node (plus a convex mesh collider built from the same geometry) holds the base, the mount and the barrel. Split into connected pieces it is 17 to 188 loose islands, but there are no separate nodes to reparent, so the visible mount and barrel cannot be made to move without editing the models.

**The fix.** A small plugin, SkizzyRig (`Plugins/SkizzyRig.dll`, written for this update, MIT licence, source in `src/SkizzyRig/`), adds the part module `ModuleRigTurret`. From numbers in the part cfg it builds these transforms under the part's `model` transform:

| Transform | Where | Used by |
|---|---|---|
| `SkizzyRigBase` | on the yaw axis (`pivot`); +Y = up (`upAxis`), +Z = the barrel's horizontal direction | ModuleTurret `baseTransformName`. Yaw 0, and the centre of `yawRange`, is along +Z |
| `SkizzyRigYaw` | same place, child of the base | ModuleTurret `yawTransformName` |
| `SkizzyRigPitch` | on the trunnion axis (`trunnion`), child of the yaw transform | ModuleTurret `pitchTransformName` |
| `SkizzyRigFire` | at the muzzle, child of the pitch transform, +Z along the bore | ModuleWeapon `fireTransformName` |

* BDArmory's rest pose is pitch 0 (barrel level), but some barrels are modelled elevated (`boreDirection`). So the fire transform goes where the modelled muzzle ends up when the barrel is swung down about the trunnion to level. When BDArmory elevates the gun to the modelled angle, the fire transform sits exactly on the visible muzzle and points along the visible bore. This was checked numerically for all six guns.
* The module can also move named mesh nodes under the yaw and pitch transforms (`yawMeshes`, `pitchMeshes`; pitch meshes are swung down by the same angle), so that a mount and barrel visibly turn. None of these models has separate nodes, so **all six guns use the invisible-transform fallback: the gun aims and fires from the right point, but its mesh does not move.**
* Timing: the transforms are built in `OnLoad` on the part prefab while KSP compiles the part. Every copy of the part therefore has them before BDArmory's `OnStart`, whatever the module order. `OnAwake` and `OnStart` repeat an idempotent check. KSP.log gets one line per gun at load: `[SkizzyRig.ModuleRigTurret]: <part>: built SkizzyRigBase/SkizzyRigYaw/SkizzyRigPitch + 1 x SkizzyRigFire; ... rest fire point(s) (...); no mesh nodes reparented: invisible rig, the model's mesh stays static`.
* If a model already has transforms with these names, the module builds nothing and logs a warning.
* Build: `dotnet build -c Release -p:DebugType=None -p:DebugSymbols=false` in `src/SkizzyRig` (net48; references `Assembly-CSharp`, `UnityEngine` and `UnityEngine.CoreModule` from `KSP_x64_Data/Managed`; set `-p:KSPRoot=<KSP folder>`). There is no BDArmory reference. The DLL contains no local paths and no PDB reference.

Per gun (part frame, metres; up on the boat is −Z, and the guns stack-attach with +Y toward the bow; x = 0 on all six, measured |x| < 2 mm):

| Part folder | Rig | Pivot (on the yaw axis) | Trunnion | Bore as modelled | Muzzle as modelled | Fire point at rest (pitch 0) |
|---|---|---|---|---|---|---|
| `20mmFF` | invisible (one mesh, 44 islands) | (0, 0, −0.666): top of the fixed column | (0, 0.006, −0.8316): flat top of the cradle fork arms (no pin is modelled) | 12.86° up, toward +Y | (0, 0.8442, −1.175): flash hider tip | (0, 0.8996, −0.9798) |
| `20mmRF` | invisible (one mesh, 45 islands) | (0, 0, −0.666) | (0, −0.006, −0.8316) | 12.86° up, toward −Y | (0, −0.8442, −1.175) | (0, −0.8996, −0.9798) |
| `50Cal` | invisible (one mesh, 17 islands) | (0, 0, −0.026): top of the pintle | (0, 0.0548, −0.1413): round lug at the top of the yoke (r 13 mm) | level, toward −Y | (0, −0.6832, −0.1355): muzzle tip | same as the muzzle |
| `Single40mm` | invisible (one mesh, 55 islands) | (0, 0, −0.187): top of the base plate | (0, −0.0814, −0.852): trunnion bearings (three coaxial pairs) | 30.22° up, toward −Y | (0, −1.3224, −1.9608): muzzle tip | (0, −1.7118, −1.1855) |
| `4inchFF` | invisible (one mesh, 188 islands) | (0, 0, −0.179): top of the fixed pedestal | (0, −0.0006, −0.7652): trunnion pins (r 83 mm) | 0.33° up, toward +Y | (0, 2.4298, −0.7643): muzzle face | (0, 2.4298, −0.7504) |
| `4inchRF` | invisible (one mesh, 188 islands) | (0, 0, −0.179) | (0, 0.0006, −0.7652) | 0.33° up, toward −Y | (0, −2.4298, −0.7643) | (0, −2.4298, −0.7504) |

How the numbers were measured (`tools/measure_gun_rigs.py` repeats this and checks the cfgs; all agree within 1 mm and 0.07°): the mesh is split into islands (connected triangles, vertices welded by position). The barrel is the longest island that is at least 6× longer than it is thick, and the bore axis is its principal axis. The muzzle is the furthest point along that axis over the barrel and any muzzle-device islands on the axis. Trunnions come from circle fits on the trunnion pins, bearings or yoke lug (in the Y-Z plane, as every trunnion axis is parallel to X), and for the 20 mm from the fork arms. The rear 20 mm and rear 4" models are the front ones turned 180° about the vertical axis (vertex check; the rear 20 mm adds one 8-triangle sliver).

Config changes on the six guns (stage 3 of the patch scripts):

* New `MODULE:NEEDS[SkizzyRig]` block `ModuleRigTurret` before `ModuleTurret`, with the numbers above. If the DLL is missing, ModuleManager drops only this block, and the gun loads but cannot aim or fire, as before.
* `ModuleTurret`: `yawTransformName = SkizzyRigYaw` (was `Rotate`), `pitchTransformName = SkizzyRigPitch` (was `Pitch`), new `baseTransformName = SkizzyRigBase`. Turret speeds and limits are unchanged.
* `ModuleWeapon`: `fireTransformName = SkizzyRigFire` (was `fireTransform`).
* The header comment `// WARNING: this model.mu has no Rotate/Pitch/fireTransform ...` is replaced by a `// Turret rig: ...` note.
* All key names were checked against the BDArmory 1.13.1 `ModuleTurret` and `ModuleWeapon` sources.

What it looks like in game: at yaw 0 and the modelled elevation, the rounds leave the visible muzzle. At any other angle they leave from where the muzzle would be if the barrel had turned, which can be up to about 4.8 m from the visible muzzle (a 4" firing aft). The static gun mesh does not block its own fire, because BDArmory ignores hits on the part that fired. BDArmory's own-vessel check still applies: a gun does not fire while its line of fire crosses its own boat. There is no muzzle flash, shell ejection or recoil movement, because the models have none.

### `BulletDefs/IronSeasKANES_Bullets.cfg` (new)

`IronSeas4in50HC` (`BULLET:NEEDS[BDArmory]`): the US 4"/50 Mk 9 H.C. Mk 15 shell. It has caliber 102 mm, 14.97 kg, 884 m/s, `tntMass = 1.2` (1.23 kg Explosive D), `fuzeType = Impact` and `apBulletMod = 0.8`. Data from navweaps.com (WNUS_4-50_mk9). Neither BDArmory nor NAS has a 4"/50 shell.

### Antenna, periscope, snorkel (`AntennaBalaoClass`, `PeriscopeBalaoClass`, `SnorkelBalaoClass`)

* Each has its own description in place of the copied 5"/38 text, and tags that fit the part in place of the copied gun tags.
* `manufacturer` is plain `Kerbin Navy` instead of NAS's localisation tag, so these parts no longer need NAS for readable text.
* The descriptions say what the parts actually do. The antenna and periscope are animation only (no CommNet antenna, no camera). The snorkel holds the diesel generator's `IntakeAir`.
* **Snorkel intake (SkizzyNaval 0.1.2).** The snorkel's `ModuleResourceIntake` named `intakeTransformName = Intake`, but the model has no `Intake` transform: its transforms are `GameObject` > `Snorkel` > `BowShape`, `MidFrontShape` (read from `model.mu`; the clip `Snorkel` slides `MidFrontShape` 3.05 m up and down). Without its transform the intake stayed Closed and never refilled its 100 `IntakeAir`, so the diesel ran only on the stored air (about 28 h at 0.001/s). It now uses `intakeTransformName = Snorkel`, the mast's static root at the attach point. KSP's intake closes (and empties the part's `IntakeAir`) while that transform is under water (`disableUnderwater = true`), so the diesel breathes while the snorkel's base is above the surface, whether the mast is raised or lowered.

### Sail (`BalaoClassSail`)

* Removed the two `PARTUPGRADE` copies (`BonVoyageUpgrade_v2/v3`). BonVoyage's own `part.cfg` defines them; with BonVoyage installed they were duplicates, and without it they were orphan tech-tree entries.
* Removed the stray closing `}` at the end of the file (line 198).
* **Crush depth.** A Balao's test depth was 400 ft (about 120 m); its collapse depth is estimated at 200–250 m. KSP's underwater static pressure on Kerbin is `101.3 + 9.81 × depth(m)` kPa (ocean density 1.0), so:
  * `WBIDiveComputer maxPressureOverride` 26000 → **2300 kPa**, which crushes at about 224 m (it was about 2.6 km). Test depth (120 m) is about 1280 kPa, which the dive computer's hull-integrity readout shows as about 56 %.
  * The part gets `maxPressure = 2300` as well. WildBlueTools syncs only the dive computer's own part, and to the *higher* of the two values, so without this the part's default 4000 kPa would win.
  * Removed the `wbiGuppy1/2` upgrades (8000/10000 kPa; not defined in WildBlueTools or Buffalo, and far too deep for a Balao) and `showUpgradesInModuleInfo`.

### Hull (`BalaoClassHull`)

* `maxPressure = 2300` (collapse at about 224 m, same as the Sail). Every other part keeps KSP's default 4000 kPa (about 398 m), so the pressure hull fails first.
* `IntakeLqd amount` 1100 → **0** (of 1400). Under WildBlueTools' formula (`buoyancy = base × (1 − fill)`) the boat used to spawn 79 % flooded, at 21 % buoyancy. It now spawns surfaced with the main ballast blown; flood to dive.

### `Patches/IronSeasKANES_Fallbacks.cfg` (new)

Each patch only runs when its dependency is **missing** (`:NEEDS[!X]`, checked against plugin assembly names), so a full install is unchanged.

| Missing | Effect |
|---|---|
| WildBlueTools | Hull: remove `WBIBallastTank`, its `EFFECTS` and the `IntakeLqd` ballast. Sail: remove `WBIDiveComputer` and the `ModuleSAS` `UPGRADES` that point at WildBlueTools' `WBIFlightControl2/3`. The 2300 kPa `maxPressure` on Sail and Hull stays. |
| WildBlueTools, **but KerbalPowers Naval present** (`Cavitation`) | Hull gets `ModuleShipBallast`: `maxBuoyancy = 1.0` (the hull's default buoyancy, surfaced), `minBuoyancy = 0`, `maxSpeed = 3` m/s, `maxDepth = 190` m (below the 224 m crush depth), `pumpRate = 5` %/s, `ECRequirement = 5` EC/s. These are starting values scaled from KP's own ballast parts; tune them in flight. |
| WildBlueTools and KerbalPowers Naval | Stock KSP has no ballast module, so the boat floats and runs on the surface only (SubTrim's virtual ballast can still dive it). |
| KerbalActuators | Sail: remove `WBIAirParkController`. |
| BonVoyage | Sail: remove `BonVoyageModule`. |
| Firespitter | Both screws: remove `FSswitchEngineThrustTransform` (forward thrust still works, reverse switch is gone). |
| NAS or BDArmory | Nothing to patch. The guns are not loaded (`PART:NEEDS[NAS,BDArmory]`), the 4" bullet is `BULLET:NEEDS[BDArmory]`, and the antenna/periscope/snorkel no longer use NAS tags. |

### License, credits and notices

* `LICENSE.txt` (GNU GPL v2, the original mod's license, verbatim from gnu.org) is in the package root and in `GameData/IronSeasKANES/`. `GameData/IronSeasKANES/README.txt` credits SkunkWorksCommand and links the original.
* GPLv2 §2(a): every modified part cfg now starts with `// Modified in 2026-11 by the unofficial KSP 1.12.5 / BDArmory 1.13 update ... Original by SkunkWorksCommand, GPLv2.` The new files (`Patches/`, `BulletDefs/`) carry a similar line.
* `Plugins/SkizzyRig.dll` is a separate plugin written for this update, under the MIT licence (`Plugins/SkizzyRig-LICENSE.txt`, and `src/SkizzyRig/LICENSE` next to its source). MIT is compatible with the GPLv2 of the rest of the mod.

### Reproducing

`tools/patch_ironseas.py` (stage 1: BDArmory 1.13 compatibility), `tools/patch_ironseas_stage2.py` (rebalance, descriptions, snorkel intake, depths, trim, notices) and `tools/patch_ironseas_stage3.py` (turret rig) turn the original zip contents into this tree byte for byte: `python -I tools/patch_ironseas.py <extracted original folder>`. Running them a second time changes nothing. `Patches/`, `BulletDefs/`, `LICENSE.txt` and `README.txt` are hand-written or copied. `Plugins/SkizzyRig.dll` is built from `src/SkizzyRig/`, whose `LICENSE` is copied as `Plugins/SkizzyRig-LICENSE.txt`. `tools/verify_ironseas_guns.py <IronSeasKANES> <BDArmory> <NAS>` checks every name the guns reference, and `tools/measure_gun_rigs.py <IronSeasKANES>` measures the rig numbers from the gun models again and compares them with stage 3.

## Pre-existing issues left alone (upstream content, reported only)

* **The gun models are single static meshes.** None of the six gun `model.mu` files has a `Rotate`, `Pitch` or `fireTransform` transform, and in each the base, mount and barrel are one merged mesh (checked in the original zip). SkizzyRig (see "Turret rig") now makes the guns aim and fire, but the meshes cannot turn without editing the models. The real fix is in Unity/PartTools: split each gun into base, mount and barrel nodes. `ModuleRigTurret`'s `yawMeshes` and `pitchMeshes` can then move those nodes without a code change.
* **Duplicate attach-node IDs**: the hull has 12 nodes and the sail 6, all named `node_stack_top1`. KSP looks nodes up by ID, so re-attaching or reloading a craft can link to the wrong node. Fixing this means renaming the nodes, which changes existing craft files, so it was left for the author.
* The guns weigh 0.01 t each and have `maxHitPoints = 100000` (copied from the 5"/38 clone). A real 4"/50 on its mount weighs about 5 t, so a full-weight gun would change the boat's trim.

## Not verified

* No in-game test was possible here. Unverified: WildBlueTools 1.90.6 ballast behaviour with this hull and the new 0-unit start, the 2300 kPa crush depth in flight, the KerbalPowers fallback numbers, BonVoyage 1.4.1 on 1.12.5, and the snorkel intake refilling in flight (the transform name was checked against the model, and the intake logic against KSP's decompiled ModuleResourceIntake).
* The turret rig has not been seen in game. The transform positions and the pitch maths were checked numerically (`tools/measure_gun_rigs.py`, plus a re-implementation of the module's maths that puts every fire transform on its muzzle at the modelled elevation), and the DLL builds against the KSP 1.12.5 assemblies, but KSP never ran it here. The gun numbers (rate of fire, ranges, drum reloads) are untested for the same reason. `BeltFed = false` drum reloading on the 20 mm is a BDArmory 1.13 feature (the fields exist in 1.13.1), but it has not been seen in game on these parts.
* Bon Voyage /L may use a different assembly or folder name. If it does not load as `BonVoyage`, fallback 3 removes `BonVoyageModule` even when /L is installed; check the Sail's PAW.
* The NEEDS checks assume assembly names `WildBlueTools`, `KerbalActuators`, `BonVoyage`, `Firespitter` and `Cavitation`.

## In-game test checklist

1. ModuleManager's loading screen should show no patch errors. In `ModuleManager.ConfigCache`, check that `BalaoClassHull` has either `WBIBallastTank` (WildBlueTools installed) or `ModuleShipBallast` (KP Naval only), never both, and `maxPressure = 2300`.
2. Editor: all 17 parts appear; the six guns appear only with NAS + BDArmory. Gun info should show the new bullets: `20mm AP (long)`, `Raufoss Mk211`, `40mm HE (Bofors)` and `4in/50 HC (US)`. The antenna, periscope and snorkel should show their own descriptions.
3. Splash the boat (VesselMover or launch from water). It should spawn **surfaced** (IntakeLqd 0/1400). Flood the ballast (WBI ballast, KP "Target Depth", or SubTrim): it should submerge, hold depth and resurface when blown. The WBI dive computer should show about 56 % hull pressure at 120 m. Going below about 224 m should destroy the Sail and Hull (overpressure).
4. Guns, at load: KSP.log should show six `[SkizzyRig.ModuleRigTurret]: ... built SkizzyRigBase/...` lines (one per gun part) and no `has no pitchTransform`, `Turret unlikely to function properly` or `Weapon missing fireTransform` lines for these parts. `ModuleManager.ConfigCache` should show `ModuleRigTurret` on all six guns.
5. Guns, in flight: put the Balao on the surface with a weapon manager (guard mode on, a team set) and an AI boat or destroyer of another team within 2-5 km. The guns traverse and elevate invisibly (the meshes stay still), and the tracers should start at the muzzles: exactly at the visible muzzle when the gun aims along its modelled direction (for example the 4" at a level target dead ahead of a forward gun), otherwise where the muzzle would be. Check that the 4" fires about 9 rounds per minute and ignores aircraft, that the 20 mm pauses about 8 s every 60 rounds, and that no gun fires through the sail or hull.
6. Without BonVoyage/KerbalActuators: the Sail's PAW must not show broken module entries, and KSP.log must not list "Cannot find a PartModule" for Iron Seas parts.
