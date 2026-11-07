# Iron Seas KANES (Balao class fleet boat): unofficial update for KSP 1.12.5 + BDArmory Plus 1.13

## Layout and install

The original zip has two top-level folders, `BalaoClassFleetBoat` and `4BladeFastScrew`, meant to go straight into `GameData`. Here they are grouped under **`GameData/IronSeasKANES/`**, together with new `Patches/` and `BulletDefs/` folders, `LICENSE.txt` and `README.txt`. No config uses absolute paths, so the move is safe. If you installed the old version, delete `GameData/BalaoClassFleetBoat` and `GameData/4BladeFastScrew` first; otherwise every part loads twice.

`diffs/IronSeasKANES_to_KSP1.12.5-BDA1.13.diff` holds every change against the original zip contents (paths relative to the zip root / `GameData/IronSeasKANES`). It was checked with `git apply` on a clean copy of the original zip, and the result is byte-identical to this folder.

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

### `BulletDefs/IronSeasKANES_Bullets.cfg` (new)

`IronSeas4in50HC` (`BULLET:NEEDS[BDArmory]`): the US 4"/50 Mk 9 H.C. Mk 15 shell. It has caliber 102 mm, 14.97 kg, 884 m/s, `tntMass = 1.2` (1.23 kg Explosive D), `fuzeType = Impact` and `apBulletMod = 0.8`. Data from navweaps.com (WNUS_4-50_mk9). Neither BDArmory nor NAS has a 4"/50 shell.

### Antenna, periscope, snorkel (`AntennaBalaoClass`, `PeriscopeBalaoClass`, `SnorkelBalaoClass`)

* Each has its own description in place of the copied 5"/38 text, and tags that fit the part in place of the copied gun tags.
* `manufacturer` is plain `Kerbin Navy` instead of NAS's localisation tag, so these parts no longer need NAS for readable text.
* The descriptions say what the parts actually do. The antenna and periscope are animation only (no CommNet antenna, no camera). The snorkel holds the diesel generator's `IntakeAir` (see the snorkel note under pre-existing issues).

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

### Reproducing

`tools/patch_ironseas.py` (stage 1: BDArmory 1.13 compatibility) and `tools/patch_ironseas_stage2.py` (rebalance, descriptions, depths, trim, notices) turn the original zip contents into this tree byte for byte: `python -I tools/patch_ironseas.py <extracted original folder>`. Running them a second time changes nothing. `Patches/`, `BulletDefs/`, `LICENSE.txt` and `README.txt` are hand-written or copied. `tools/verify_ironseas_guns.py <IronSeasKANES> <BDArmory> <NAS>` checks every name the guns reference.

## Pre-existing issues left alone (upstream content, reported only)

* **The gun models cannot aim or fire.** None of the six gun `model.mu` files contains a `Rotate`, `Pitch` or `fireTransform` transform; each is one static mesh (checked in the original zip). BDArmory therefore logs `has no pitchTransform` / `Turret unlikely to function properly` and `Weapon missing fireTransform [fireTransform]! Please fix your model`, and the guns never turn or shoot, whatever the numbers above say. The fix needs the models: in Unity/PartTools add an empty `Rotate` (yaw axis at the mount's pivot), a child `Pitch` (at the trunnions) holding the moving barrel meshes, and a child `fireTransform` at the muzzle (+Z along the bore). The configs above are ready for that.
* **Duplicate attach-node IDs**: the hull has 12 nodes and the sail 6, all named `node_stack_top1`. KSP looks nodes up by ID, so re-attaching or reloading a craft can link to the wrong node. Fixing this means renaming the nodes, which changes existing craft files, so it was left for the author.
* **Snorkel intake**: its `ModuleResourceIntake` uses `intakeTransformName = Intake`, but the snorkel model has no `Intake` transform. KSP then reports the intake as Closed and never refills its 100 `IntakeAir`. The hull's diesel generator burns 0.001 IntakeAir/s, so the stored air lasts about 28 h of running. Probable one-line fix (untested): `intakeTransformName = Snorkel` (that transform exists in the model).
* The guns weigh 0.01 t each and have `maxHitPoints = 100000` (copied from the 5"/38 clone). A real 4"/50 on its mount weighs about 5 t, so a full-weight gun would change the boat's trim.

## Not verified

* No in-game test was possible here. Unverified: WildBlueTools 1.90.6 ballast behaviour with this hull and the new 0-unit start, the 2300 kPa crush depth in flight, the KerbalPowers fallback numbers, and BonVoyage 1.4.1 on 1.12.5.
* The gun numbers can only be tested once the gun models have turret transforms (see above). `BeltFed = false` drum reloading on the 20 mm is a BDArmory 1.13 feature (the fields exist in 1.13.1), but it has not been seen in game on these parts.
* Bon Voyage /L may use a different assembly or folder name. If it does not load as `BonVoyage`, fallback 3 removes `BonVoyageModule` even when /L is installed; check the Sail's PAW.
* The NEEDS checks assume assembly names `WildBlueTools`, `KerbalActuators`, `BonVoyage`, `Firespitter` and `Cavitation`.

## In-game test checklist

1. ModuleManager's loading screen should show no patch errors. In `ModuleManager.ConfigCache`, check that `BalaoClassHull` has either `WBIBallastTank` (WildBlueTools installed) or `ModuleShipBallast` (KP Naval only), never both, and `maxPressure = 2300`.
2. Editor: all 17 parts appear; the six guns appear only with NAS + BDArmory. Gun info should show the new bullets: `20mm AP (long)`, `Raufoss Mk211`, `40mm HE (Bofors)` and `4in/50 HC (US)`. The antenna, periscope and snorkel should show their own descriptions.
3. Splash the boat (VesselMover or launch from water). It should spawn **surfaced** (IntakeLqd 0/1400). Flood the ballast (WBI ballast, KP "Target Depth", or SubTrim): it should submerge, hold depth and resurface when blown. The WBI dive computer should show about 56 % hull pressure at 120 m. Going below about 224 m should destroy the Sail and Hull (overpressure).
4. Guns: expect the BDArmory log lines quoted above; the guns will not fire until the models are fixed. After a model fix: surface, engage an AI destroyer, and check that the 4" fires about 9 rounds per minute and ignores aircraft, and that the 20 mm pauses about 8 s every 60 rounds.
5. Without BonVoyage/KerbalActuators: the Sail's PAW must not show broken module entries, and KSP.log must not list "Cannot find a PartModule" for Iron Seas parts.
