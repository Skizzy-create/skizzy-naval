# MNWS (Modern Naval Weapons Systems 2.4, "Johnny"): KSP 1.12.5 + BDArmory Plus 1.13 update

The original pack targets KSP 1.4.4 and BDAc 1.2. This folder makes it work with KSP 1.12.5 and
BDArmory Plus 1.13.x (BrettRyland). It also fixes the problems that matter for naval AI combat.

**No original file was edited.** All changes are in new files, so there is no unified diff:

| File | Purpose |
|---|---|
| `GameData/Johnny/MMPatches/MNWS_BDA113.cfg` | Compatibility and correctness fixes. Required. |
| `GameData/Johnny/MMPatches/MNWS_BDA113_Balance.cfg` | Optional balance pass. Delete it to keep the author's numbers. |
| `GameData/Johnny/Plugins/MNWSFix.dll` | Small helper plugin (section 8): creates the launch transforms for the AVLS VLS variants and links missiles to the hull bay doors. Optional; without it the patches that need it are skipped. |
| `src/MNWSFix/` | Source and project file of `MNWSFix.dll`. Not needed in game. |

The two `.cfg` files are generated from a script. Every value below matches what is in them.

## Install

1. Requirements: KSP 1.12.5, BDArmory Plus 1.13.x and ModuleManager 4.x (4.2.3 is already in
   your GameData). Aircraft Carrier Accessories (ACA) and Firespitter are optional; see below.
   The only DLL included is `Johnny/Plugins/MNWSFix.dll`, built from `src/MNWSFix`.
   ModuleManager, Firespitter and ACA come from their own downloads.
2. Delete any old `GameData/Johnny` folder first.
3. Copy `patched/MNWS/GameData/Johnny` into `KSP/GameData/`. The result is `GameData/Johnny/...`,
   including `GameData/Johnny/Plugins/MNWSFix.dll`.
4. Start KSP. ModuleManager re-patches automatically because the config hash changes.

To run without the plugin, delete `Johnny/Plugins`. The patches marked `:NEEDS[MNWSFix]` are then
skipped: the AVLS VLS variants (AVLS11V/22V/33V) do not exist, and the automatic "In Cargo Bay"
and "Custom Bay" settings must be made by hand (sections 4 and 5).

### Disk usage

The 571 unchanged files in `GameData/Johnny` are NTFS **hard links** to
`_downloads/naval/MNWS/extracted/Johnny`, so this folder uses no extra disk space. Only the two
`MNWS_BDA113*.cfg` files, `Plugins/MNWSFix.dll`, `src/MNWSFix` and this document are new.
Copying the folder into KSP's GameData makes normal, independent copies of about 642 MB, which is
expected.

Do not edit a linked file in place under `patched/`. Doing so also changes the extracted copy.
Edit the copy inside GameData instead.

## What changed: MNWS_BDA113.cfg (required)

All patches run in `:AFTER[BDArmory]`, after BDArmory's own `HitpointTracker` patch.

### 1. Modules from plugins that do not exist for KSP 1.12 / BDA 1.13

These modules are scoped to MNWS parts with `:HAS[#author[Johnny*]]`. All 114 parts have
`author = Johnny` or `author = Johnny005611`.

| Module | Parts | Action |
|---|---|---|
| `MNWSCategoryModule` | 107 parts | Removed. The plugin is not shipped, and BDA 1.13 auto-categorises weapons. `MMPatches/MNWS_Category.cfg` only matches parts named `MNWS_*`, of which there are none, so it is inert and left alone. |
| `BDACategoryModule` | H20AR | Removed. It no longer exists in BDArmory. |
| `BurnTogether` | 39 ships and aircraft | Removed only when the BurnTogether plugin is absent (`:NEEDS[!BurnTogether]`). |
| `ModuleAileron` | 21 aircraft | Removed. No KSP 1.12 plugin provides it, so it never loaded. 12 jets (DRAGON, EDI, F15E, F16C, F16XL, F22, F35, J10, J15, J20, NATF, NATF2) already carry a stock `ModuleControlSurface`, which is the stock equivalent, and keep the author's values. CFA44, CFA44A, H20A, H20B, UCAV1, UCAV2, UH60, V22 and NH80 only have a `ModuleLiftingSurface` and steer with their reaction wheels, as they did before. |
| `Tailhook` (8 jets), `ReloadableRail`, `FuelNozzle`, `FuelCap` (MSWSWT1) | ACA modules | Kept. They are removed only when neither the ACA assembly nor the `KFC` folder exists (`:NEEDS[!ACA&!KFC]`). With ACA present, `wiretension = 15f` (C# syntax that KSP cannot parse) becomes `15`. |

### 2. Renamed or removed BDArmory modules and keys

* **R276 Launcher**: the `RocketLauncher` module was removed from BDArmory. It is replaced by a
  `ModuleWeapon` with `weaponType = rocket`, `rocketPod = true` and
  `fireTransformName = rockets`. The ammo is `S-8KOMRocket` (184 rounds) with rocket types
  `8KOMS; 8DMS`, and it fires at 1600 rpm as before. This is the same setup as BDA's own S-8 pod.
  The R276 model already has `rockets/rocket_1..rocket_184`.
* **AAMS, AAMSR, NH80GUN**: `airDetonation` and `airDetonationTiming` were removed from
  `ModuleWeapon`. Fuzing now comes from the bullet (see section 3).
* **AAMS, AAMSL, AAMSLM, AAMSR, FRADAR**: `ModuleTargetingCamera.shortName` was removed. The
  field does not exist.
* **AGM258, AGM2588, AGM46, GBBM** (Cruise): `terminalManeuvering = true` becomes
  `CruisePopup = true`. CruisePopup is the 1.13 pop-up attack.
* **HHQ16C, HHQ19, HHQ19RCS**: `terminalManeuvering` was removed. It had no effect on non-cruise
  missiles.
* **Torpedoes MK48, MK56, MK58 and ASMM**: `homingType aam/AAM` becomes `SLW`, BDA's underwater
  homing, as used by its own Stingray. `engageGround/SLW = true` and `engageAir/Missile = false`
  are set. `isTubeLoaded` and `terminalGuidanceActive` were removed because they no longer exist.
  The `terminalGuidanceType = radar` and `terminalGuidanceDistance` entries were also removed.
  They used the same seeker as the main one, so BDA disabled them anyway.

After patching, the field check reports no unknown keys. It covers every module key in the
patched configs, checked against the BDArmory source, the stock `ModuleCargoBay` and
`ModuleAnimateGeneric` fields, and the two MNWSFix modules.

### 3. Bullet definitions (`BulletDefs/Johnny_Bullets.cfg`)

* The pack's duplicate `BULLET { name = def }` is deleted
  (`!BULLET[def]:HAS[#blastPower[*]]`). BDA only reads its default bullet from `BD_Bullets.cfg`
  and logs an error for the duplicate. The patch targets only the pack's copy, because only it
  has the legacy `blastPower` key.
* **208mmRailgun, 428mmRailgun, 35mmAAMS**:
  * The legacy `blastPower`, `blastHeat` and `blastRadius` keys were removed.
  * `explosive = true` becomes `Standard`. Bullets with `tntMass > 0` explode.
  * Added `incendiary = False`, `projectileCount = 1`, `guidanceRange = -1`, `fadeColor = False`,
    `DisplayName` and the tracer colours, so BDA logs no "using default value" warnings.
* Fuzes:
  * Railguns use `fuzeType = Impact`, matching the legacy behaviour.
  * 35mmAAMS uses `fuzeType = Flak`. The guns had `airDetonation = true` and
    `airDetonationTiming = false` (proximity air burst). Flak is proximity plus automatic time
    fuzing, the closest 1.13 equivalent for a CIWS round. The literal mapping would be
    `Proximity`; edit that one line if you prefer it.

### 4. AVLS11 / AVLS22 / AVLS33 missile cells

There are now two versions of each cell block.

#### 4a. AVLS11V, AVLS22V, AVLS33V: real BDArmory VLS (needs MNWSFix.dll)

These are new parts, copied from the originals with `+PART[AVLS11]` and so on. Their titles are
"AVLS11 VLS (8 cells, BDArmory launcher)", "AVLS22 VLS (32 cells, ...)" and "AVLS33 VLS (72
cells, ...)", in the stock Coupling category and in BDA's "Missile turrets" category. They are set
up like BDArmory's own VLS (`bahaVLSQuad`):

| Module | Settings |
|---|---|
| `ModuleInjectTransforms` (MNWSFix) | Creates `mnwsVLS/missile1..missileN` in the model, one per cell (section 8). `alignMissileTail = true`. |
| `MissileLauncher` | `missileType = launcher` |
| `MultiMissileLauncher` | `launchTransformName = mnwsVLS`, `deployAnimationName = AVLS` (the cell doors), `deploySpeed = 1` (doors open in 1 s), `deployTime = 1.5`, `salvoSize = 1` (adjustable in the editor up to the cell count), `rippleRPM = 120`, `isMultiLauncher`, `overrideReferenceTransform` (aims by vessel, not by cell axis), `RailNode = missile`, `OverrideDropSettings` (eject 10 m/s along the cell axis), `ignoreLauncherColliders`, empty `subMunitionName` and `subMunitionPath` |
| `ModuleMissileRearm` | `railAmmo = maxAmmo =` cell count (8 / 32 / 72), `reloadTime = 30`, `AccountForAmmo = true` |

* **Loading.** In the editor, attach one missile to any of the cell nodes (`node_stack_missile*`).
  The launcher takes that missile type for all cells and removes the attached part, exactly like
  BDA's VLS. The loaded missiles are shown in the cells.
* **Firing.** The weapon manager sees one weapon per block, named after the loaded missile. Before
  each salvo the launcher opens the doors. It then spawns a missile in each cell in turn, with the
  tail on the cell floor, and ejects it straight up at 10 m/s. After `deployTime` it closes the doors.
* The door `ModuleAnimateGeneric` is removed from the copies, because the launcher drives the
  `AVLS` animation itself. The copies have no cargo bay.
* Cell size limits which missiles fit. AVLS11 cells are 0.90 m wide and 8.05 m deep (SM_KKV,
  HHQ17, LRASM and similar). AVLS22 cells are 0.43 m and 8.05 m (HHQ16C and similar). AVLS33
  cells are 0.27 m and 6.05 m (HHQX, CUDA). A longer missile still fires, but its nose shows
  through the closed doors.
* Mass, the 1/5 equipment mass factor and HP (6000) are the same as the rack versions.
* The original AVLS11/22/33 are unchanged racks (4b), so existing craft still load.

#### 4b. AVLS11 / AVLS22 / AVLS33 (original part names): racks with working doors

* Each cell gets a stock `ModuleCargoBay` with these settings:
  * `DeployModuleIndex = 1`, which points at the `AVLS` door animation (module 0 is
    `ModuleColorChanger`).
  * `closedPosition = 0`, `lookupCenter = 0, 4.6, 0` and `lookupRadius = 5.8`.
  * `useBayContainer = true` with `bayContainerName = Main`. `Main` is the floor collider. The
    default enclosure raycast would be blocked by the cell divider walls (collider `55` at
    x = ±0.25 m, plus the inner walls on AVLS22), and the floor never intersects that ray.
* The door events are renamed to `Open` and `Close`. BDA 1.13 only toggles a cargo-bay animation
  whose event reads exactly "Open" or "Close".
* BDA only opens a bay for a missile whose **In Cargo Bay** flag is on. BDA sets that flag
  automatically only when the weapon manager starts. At that moment the stock cargo bay has not
  yet worked out which parts it shields, because it does so after a delayed collider reset. So
  without help the flag usually stays off, and the doors stay shut. `ModuleMNWSBayLink`
  (section 8) is added with `bayAnimations = AVLS`. In flight it sets In Cargo Bay on every
  missile this cell's bay shields. Without the plugin, set **In Cargo Bay = true** on the missiles
  in the editor.
* AVLSX1/2/3 already were MissileTurrets and are unchanged.

### 5. Missile and torpedo bays built into the hulls

The hulls carry missiles and torpedoes on attach nodes behind animated doors, but nothing told
BDArmory about the doors, so it fired into closed doors. They are linked in two ways.

**Cargo-bay link.** This is a stock `ModuleCargoBay` with these settings:

* `DeployModuleIndex` points at the door's `ModuleAnimateGeneric`. It is the index in the original
  module list. The patches only append modules, or remove modules that come after the doors;
  the simulator output was checked for this.
* The door events are renamed `Open` and `Close`, which BDA requires. The action names are kept
  readable (for example "Toggle Missile Bay").
* `useBayContainer = true` points at a collider far from the bay. This turns KSP's enclosure test
  into a plain lookup-sphere test. Otherwise the hull's own colliders block the enclosure ray.
* Each sphere covers the bay's attach nodes and anything hung on them. `baycheck.py` checks the
  coverage against all 32 MNWS missiles with models, with each missile placed on every bay node.

`ModuleMNWSBayLink` sets In Cargo Bay on the shielded missiles, as in 4b. It also checks that
each bay still points at its door, and re-points it if another mod's patch has shifted the module
order.

| Hull | Door (module, animation) | Lookup centre | Radius m | Container collider | Bay nodes covered |
|---|---|---|---|---|---|
| SSBN002 | 5, `SSBN002DD` (SLBM tubes) | 0, 15, 0 | 44.5 | Box003 | 56 tube nodes, all 32 missiles |
| SSBN002 | 6, `SSBN002TD` (torpedo room) | 0, 76, 0 | 10 | Box003 | 16 torpedo nodes |
| CGNX2050 | 6, `CGNX2050AMCOPEN` (missile cells) | 0, 45, -32 | 12.5 | Cylinder001 | 28 cell nodes, all missiles except the 13.8 m BMM on 4 outer nodes |
| AMB | 2, `AMBOPEN` | 0, -2, -6.5 | 8 | ____001 | 16 canister nodes |
| AMBII | 2, `AMBIID` | 0, -2, -6.5 | 8 | ____001 | 18 canister nodes (incl. the two side-door nodes) |
| MSWS111 | 10, `MSWS111` | 0, 27, -4 | 10.5 | Box001 | 6 nodes (missile51–56) |
| FDG057 | 5, `057TD` (torpedo room) | 0, -12.8, -11.5 | 11.5 | Sphere001 | no nodes; any torpedo lying in the room (max 11.3 m from centre) |

**Action-group link (CGNX2050 and CGX065 torpedo doors).** On these hulls one door animation
opens two separate side rooms, about 80 m (CGNX2050) or 36 m (CGX065) apart. A cargo bay cannot be
used here, for two reasons:

* One sphere cannot cover both rooms without also covering the whole deck between them.
* Two cargo bays on one animation make BDA close the door it has just opened. Each bay closes
  "its" door when the current missile is not inside it.

The closest working alternative is used instead:

* The door's toggle action gets `defaultActionGroup = Custom08` (AG8, labelled "Toggle Torpedo
  Doors (AG8)"). This applies to newly placed hulls.
* `ModuleMNWSBayLink` adds AG8 to the door action at flight start, so older craft get it too.
* In flight, `ModuleMNWSBayLink` sets BDA's **Custom Bay** (`customBayGroup`) to 8 on every
  missile whose centre is inside one of the two room spheres. It only does this when the player
  left Custom Bay at 0.
* BDA then toggles AG8, which opens the doors, waits 2 s and fires.

| Hull | Door (module, animation) | Room spheres (centre; radius m) | Check |
|---|---|---|---|
| CGNX2050 | 7, `CGNX2050ATDOPEN` | ±41, -47.5, -18; 19 | torpedoes anywhere in either room (max 18.4 m) |
| CGX065 | 5, `CGX065TD` | ±17.96, -13.41, -21.45; 12.5 | torpedoes anywhere in either room (max 12.0 m) |

Keep AG8 free of other actions on these two hulls, because BDA toggles the whole group. Without
the plugin, bind the door to an action group yourself and set Custom Bay on those torpedoes.

### 6. Ships and vehicles: AI, command, sonar, mass, hitpoints, armour

Every hull below embeds `MissileFire` but had **no AI**. The AI is added only if the part has no
AI module yet.

* Surface ships get `BDModuleSurfaceAI`. All profiles use `MaxDrift 10`, `steerMult 6` and
  `steerDamping 3`.

  | Profile | Parts | SurfaceTypeName | Cruise / Max speed (m/s) | BroadsideAttack | Engagement range (m) |
  |---|---|---|---|---|---|
  | Escorts | FDG057, FDG060, DDG030, 055A, 065, 055X1, CGNX2050, CGX065 | Water | 15 / 22 | True (OrbitDirection Either) | 1500–8000 |
  | Carriers | 075A, CVN16, CVN003, CVN004, CVN21 | Water | 12 / 16 | False | 4000–8000 |
  | Missile boats | AMB, AMBII | Water | 20 / 30 | False | 1000–8000 |

* **SSBN002** gets `BDModuleSurfaceAI` with `SurfaceTypeName = Submarine` and
  `CombatAltitude -50`. It cruises at 10 m/s with a maximum of 15 m/s, and its engagement range
  is 2000–8000 m. The AI rises to −10 m to launch missiles.
* **3X** missile truck gets `SurfaceTypeName = Stationary`. It has no engines or wheels. Its
  maxHitPoints changes from 50000 to 8000.
* **MSWS111** gets `BDModuleOrbitalAI` with defaults. It looks like a space warship: 4 g of
  thrust, a size-5 docking port, a ModuleTestSubject, and weapon nodes on all sides. This is a
  judgement call. Swap in a Water `BDModuleSurfaceAI` if you use it at sea.
* **CVN003 / CVN004** had no `ModuleCommand`, so nothing could control them. They get
  `ModuleCommand` (minimumCrew 0) and `ModuleSAS` (level 3).
* **Hull sonar**: BDA 1.13 radar cannot see underwater targets, and a radar vessel below −10 m
  sees nothing.
  * The 8 escorts get an extra `ModuleRadar` "MNWS Hull Sonar": active sonar with
    `sonarType = 1` and `rwrThreatType = 6`. It is omnidirectional with up to 4 locks, and its
    curves come from BDA's Sonar Pod (assured detection out to 3 km, 150 m² at 40 km).
  * SSBN002's existing "SSBN002 Sonar" was a plain radar, so it was blind when submerged. It now
    has `sonarType = 1` and the same km-scale curves. Its old curves assured detection out to
    600 km.
* **Mass clamp, buoyancy and propulsion**: in KSP, buoyancy force and linear and angular water
  drag do not depend on part mass. Buoyancy, engine thrust and reaction-wheel torque are scaled
  by the same factor as the mass, which has these effects:
  * Draft, acceleration and turn rate stay as the author tuned them.
  * Top speed drops by √factor, about 2.2× for 1/5. The originals were far too fast, and the AI
    caps speed anyway.
  * BallastWater capacity and pump rates (075A, SSBN002) are scaled the same way, because
    BallastWater is 1 t/unit.
  * Rule used: total mass is the author's mass ÷ 5, with carriers capped at 24 000 t (about
    Nimitz ÷ 4). AMB and AMBII (400 t) are unchanged.
* **Hitpoints and armour**: the originals had 1.2–2 million HP and 800–1800 mm of armour of type
  "None". BDA 1.13 forces "None" armour to 10 mm, so those armour values never applied.
  * HP is now set explicitly. BDA uses an explicit maxHitPoints literally.
  * Armour is now **Mild Steel**. `armorVolume` is set explicitly: normally BDA uses the
    bounding-box area, about 20 000 m² on these hulls, which would add 4 000–15 000 t of steel.
    The value used keeps armour at about 10 % of the ship's mass.
  * The hull mass is lowered by the armour mass, so the total equals the target.

| Part | AI | Mass t (orig → new total) | Scale | Hull + armour t | Armour | armorVolume m² | maxHP (orig 1.2–2 M) | Buoyancy | maxThrust kN | RW torque P/Y/R |
|---|---|---|---|---|---|---|---|---|---|---|
| FDG057 | Water (escort) + sonar | 6000 → 1200 | 1/5 | 1080.7 + 119.3 | 20 mm Mild Steel | 760 | 40000 | 0.75 → 0.15 | 25000 → 5000 | 2e7/2e7/1e7 → 4e6/4e6/2e6 |
| FDG060 | Water (escort) + sonar | 6000 → 1200 | 1/5 | 1080.7 + 119.3 | 20 mm Mild Steel | 760 | 40000 | 0.75 → 0.15 | 25000 → 5000 | 2e7/2e7/1e7 → 4e6/4e6/2e6 |
| DDG030 | Water (escort) + sonar | 10000 → 2000 | 1/5 | 1799.8 + 200.2 | 25 mm Mild Steel | 1020 | 60000 | 0.75 → 0.15 | 35000 → 7000 | 2e7/2e7/1e7 → 4e6/4e6/2e6 |
| 055A | Water (escort) + sonar | 12500 → 2500 | 1/5 | 2250.8 + 249.2 | 25 mm Mild Steel | 1270 | 60000 | 1.2 → 0.24 | 30000 → 6000 | 2e7/2e7/1e7 → 4e6/4e6/2e6 |
| 065 | Water (escort) + sonar | 12500 → 2500 | 1/5 | 2250.8 + 249.2 | 25 mm Mild Steel | 1270 | 60000 | 0.8 → 0.16 | 48000 → 9600 | 2e7/2e7/1e7 → 4e6/4e6/2e6 |
| 055X1 | Water (escort) + sonar | 20000 → 4000 | 1/5 | 3598.9 + 401.1 | 35 mm Mild Steel | 1460 | 80000 | 1.2 → 0.24 | 40000 → 8000 | 6e7/6e7/3e7 → 1.2e7/1.2e7/6e6 |
| CGNX2050 | Water (escort) + sonar | 20000 → 4000 | 1/5 | 3598.9 + 401.1 | 35 mm Mild Steel | 1460 | 80000 | 1.2 → 0.24 | 40000 → 8000 | 6e7/6e7/3e7 → 1.2e7/1.2e7/6e6 |
| CGX065 | Water (escort) + sonar | 32000 → 6400 | 1/5 | 5759.4 + 640.6 | 40 mm Mild Steel | 2040 | 100000 | 0.6 → 0.12 | 85000 → 17000 | 2e7/2e7/1e7 → 4e6/4e6/2e6 |
| 075A | Water (carrier) | 20000 → 4000 | 1/5 | 3598.9 + 401.1 | 35 mm Mild Steel | 1460 | 120000 | 1.2 → 0.24 | 60000 → 12000 | 6e7/6e7/3e7 → 1.2e7/1.2e7/6e6 |
| CVN16 | Water (carrier) | 120000 → 24000 | 1/5 | 21597.9 + 2402.1 | 60 mm Mild Steel | 5100 | 200000 | 1 → 0.2 | 80000 → 16000 | 3e8/3e8/1.2e8 → 6e7/6e7/2.4e7 |
| CVN003 | Water (carrier) | 256000 → 24000 | 1/10.67 | 21597.9 + 2402.1 | 60 mm Mild Steel | 5100 | 200000 | 1.5 → 0.14062 | 480000 → 45000 | 6e8 ×3 → 5.625e7 ×3 |
| CVN004 | Water (carrier) | 560000 → 24000 | 1/23.33 | 21597.9 + 2402.1 | 60 mm Mild Steel | 5100 | 200000 | 1.5 → 0.06429 | 880000 → 37714.3 | 1.2e9 ×3 → 5.14e7 ×3 |
| CVN21 | Water (carrier) | 560000 → 24000 | 1/23.33 | 21597.9 + 2402.1 | 60 mm Mild Steel | 5100 | 200000 | 1.5 → 0.06429 | 400000 → 17142.9 | 1.2e9 ×3 → 5.14e7 ×3 |
| SSBN002 | Submarine | 15600 → 3120 | 1/5 | 2809.1 + 310.9 | 30 mm Mild Steel | 1320 | 50000 | 1 → 0.2 | 40000 → 8000 | 3e7/5e6/1e6 → 6e6/1e6/2e5 |
| MSWS111 | OrbitalAI | 51200 → 10240 | 1/5 | 9215.6 + 1024.4 | 50 mm Mild Steel | 2610 | 100000 | 1.2 → 0.24 | 2000000 → 400000 | 4e7 ×3 → 8e6 ×3 |
| AMB | Water (fast boat) | 400 → 400 | 1/1 | 360.0 + 40.0 | 15 mm Mild Steel | 340 | 20000 | 0.9 (same) | 2500 (same) | same |
| AMBII | Water (fast boat) | 400 → 400 | 1/1 | 360.0 + 40.0 | 15 mm Mild Steel | 340 | 20000 | 0.9 (same) | 2500 (same) | same |

BallastWater: 075A capacity 10000 → 2000 and pump rate 2000 → 400. SSBN002 capacity
20000 → 4000 and pump rate 1000 → 200.

**Ship-mounted equipment mass** is scaled by the same 1/5 (`@mass *= 0.2`). Without this, a
fully fitted hull would weigh five times more relative to its own buoyancy than intended and
would ride far lower or sink. Affected parts:

* AAMS, AAMSL, AAMSLM, AAMSR, FRADAR
* HHQXL, HHQXL2, HHQXX, SAAM, YUXX, MK58L, RML, LLauncher, AS_ML
* AVLS11/22/33, AVLS11V/22V/33V, AVLSX1/2/3, MissleCell2050
* DDG2002 RAIL (120 → 24 t), RGBB (520 → 104 t)
* MSWSWT, MSWSWT1–4, MSWSM4X

Missiles, aircraft, the 3X truck and its turrets are unchanged.

### 7. Miscellaneous

* `Agencies/Agents.cfg` points at `Johnny/Agencies/MNP_scaled`, which is not in the pack. It now
  uses `MNP`.
* `Resources/Jresource.cfg` adds an `FSCoolant` resource scanner to the stock SurfaceScanner.
  That scanner is removed when Firespitter (which defines FSCoolant) is not installed. You have
  Firespitter, so nothing changes for you.

## 8. MNWSFix.dll (plugin)

The plugin is about 19 KB. It targets .NET Framework 4.8 and references only `Assembly-CSharp`,
`UnityEngine.CoreModule`, `mscorlib` and `System.Core`. It has **no BDArmory reference**: BDA
fields are read and written by name through KSP's field list, so the modules do nothing when BDA
is absent. It registers as `KSPAssembly("MNWSFix", 1, 0)`, which is the name the
`:NEEDS[MNWSFix]` patches test for.

### ModuleInjectTransforms: how the launch transforms are made

`MultiMissileLauncher.Start()` calls `part.FindModelTransform(launchTransformName)`. It then
takes the transform's children as launch points, indexed by the number after the first 7
characters of the child's name (`missile1` → index 0). It spawns each missile at the child's
position and rotation and ejects it along the child's +Z. The AVLS models have no such transforms.

The module creates them from cfg data:

* `groupName` (here `mnwsVLS`): a new empty transform under the part's `model` root, or under
  `parentTransform` if that is given. It is rotated by `groupRotation` (here -90, 0, 0, so the
  group's +Z is the part's +Y, out of the cells).
* `childPrefix` + index (`missile1..missileN`, starting at `firstIndex`): one child per entry of
  `positions`, a `"x, y, z; x, y, z; ..."` list in the parent's (part) frame. Each child gets
  `childRotation` relative to the group. `count` is only a sanity check.
* **Timing.** KSP compiles a part as follows: the model is attached, then the modules are added,
  then `OnLoad` runs with the cfg values (`PartLoader.ParsePart`). The module creates the
  transforms in that `OnLoad`, on the part **prefab**. Every editor and flight part is a clone of
  the prefab, so the transforms already exist before any Awake, OnStart or Start runs, whatever
  the module order. `OnAwake`, `OnLoad` and `OnStart` on each instance repeat the creation as a
  safety net. The creation is idempotent: it looks the transforms up by name first.
* **`alignMissileTail`.** Every 0.5 s in the editor and in flight, the module reads the missile
  part name loaded into the launcher (`MultiMissileLauncher.subMunitionName`, by name). When it
  changes, every child is moved along its own axes so that the missile's seat sits on the cell
  floor position given in `positions`. The seat is the missile prefab's `bottom` stack node if
  that faces -Z (true for all MNWS missiles); otherwise it is the bottom-centre of the mesh bounds.
  The displayed dummies are children of the launch points, so they move too. Missiles of any
  length therefore stand on the floor instead of hanging from their origin.

### ModuleMNWSBayLink

* `flagCargoBayMissiles` (default on): in flight, about every 2 s, sets `inCargoBay = true` on
  every part whose `airstreamShields` contains one of this part's `ModuleCargoBay` modules.
  BDA opens a bay only for such missiles (see 4b).
* `zones`, `zoneBayGroup`, `zoneDoorModuleIndex`, `zoneDoorAnimation`: the action-group link from
  section 5. Spheres are given in the part frame, and the test uses the missile's renderer
  centroid.
* `bayAnimations`: the expected door animation for each `ModuleCargoBay` on the part, in module
  order. It is checked in the editor and in flight. A wrong index is re-pointed at the right door
  module, with a warning in the log. BDA reads `DeployModuleIndex` when it fires, so the repair
  takes effect.
* `debug = true` logs every change. Normally each missile is reported once, as
  `[MNWSFix.ModuleMNWSBayLink]: <missile> (...): inCargoBay = true ...`.

### Building

`src/MNWSFix/MNWSFix.csproj` is an SDK-style project. Build it with
`dotnet build -c Release` (needs the .NET Framework 4.8 targeting pack). It references the game's
`KSP_x64_Data/Managed` DLLs through the `KSPRoot` property. The default is
`D:\Epic Games\KerbalSpaceProgram\English`; override it with `-p:KSPRoot=...`. Copy
`bin/Release/MNWSFix.dll` to `GameData/Johnny/Plugins/`. The shipped DLL was built this way, with
0 warnings and 0 errors.

## 9. How the cell positions and bay volumes were derived

All geometry comes from the pack's `.mu` files and part cfgs. A small read-only .mu parser read
the values, not a person. `mnws_vls.py` (AVLS cells and bay data) and `baycheck.py` (bay coverage)
belong to the generator scripts that write the two `.cfg` files.

**AVLS cells (`mnws_vls.py`).** The procedure has five steps:

1. Every AVLS model has a cell floor: the top of collider `Main` at y = 1.0 on AVLS11 and AVLS22,
   and a raised grid floor at y = 3.0 on AVLS33 (396 vertices at exactly that height).
2. All mesh vertices in a ±5 cm band around the floor height are collected, excluding the door
   leaves `Left` and `Right`. Their distinct X and Z coordinates (merged within 6 mm) are the
   outlines of the square cells and walls.
3. For each author attach node `node_stack_missile*`, the cell is the smallest X interval and the
   smallest Z interval of those outlines that contain the node, provided each is between 0.2 and
   1.0 m wide.
4. The cell centre is the midpoint of both intervals. The launch point is that centre at
   floor + 2 cm.
5. Checks: every node falls in exactly one cell, there are no duplicate cells, and there are 8,
   32 and 72 cells respectively. The author's nodes are 2–4 cm off-centre, and the geometry is
   used.

| VLS part | Cells | Cell width m | Floor y | Depth to doors m | Cell X centres | Cell Z centres | Max node offset m |
|---|---|---|---|---|---|---|---|
| AVLS11V | 8 | 0.901 | 1 | 8.05 | -0.8396, 0.8207 | -1.5901, -0.5249, 0.5297, 1.5949 | 0.019 |
| AVLS22V | 32 | 0.430 | 1 | 8.05 | -1.0763, -0.6059, 0.5845, 1.0557 | -1.8206, -1.3517, -0.7561, -0.2871, 0.2974, 0.7664, 1.3638, 1.8328 | 0.026 |
| AVLS33V | 72 | 0.266 | 3.001 | 6.05 | -1.1526, -0.8408, -0.5294, 0.5071, 0.819, 1.1304 | -1.9089, -1.5912, -1.2769, -0.8455, -0.5278, -0.2135, 0.2129, 0.5306, 0.8449, 1.2817, 1.5994, 1.9137 | 0.038 |

The doors (`Left`/`Right`) sit at y = 9.05–9.13 and the clip `AVLS` (1 s) swings them 90°. The
launch-point order follows the author's node order.

**Hull bays (`mnws_vls.py`, checked by `baycheck.py`).**

* Door modules and animations come from the part cfgs. Which leaves each clip moves, and where,
  comes from the animation curves in the `.mu`. Every clip's first key is the closed pose, so
  `closedPosition = 0`.
* Containers are colliders whose name is unique in the model and which lie well away from the
  bay. The check confirms that no ray from a bay candidate to the lookup centre touches the
  container's bounding box.
* A missile on a hull node at position P with direction D has its renderer centroid at
  P + D × (centroid − bottom node) along its axis. For each bay, every one of the 32 MNWS missiles
  with a model is placed on every bay node. Its centroid must be within the radius, inside the
  hull's bounding box, and have an unblocked ray. Rooms without nodes (FDG057, and the two
  action-group hulls) are checked with every torpedo lying along X or Y anywhere in the room.
* SSBN002's missile sphere also covers the sail and the deck around it (radius 44.5 m). This is
  needed because the 56 tubes span 87 m.

## What changed: MNWS_BDA113_Balance.cfg (optional)

This file runs in `:LAST[BDArmory]`, after the compat file. Delete it to keep the original
numbers.

* **Warheads and ranges.** The originals were 2–100 tonnes of TNT, which killed any ship
  instantly, and launch ranges up to 2000 km. The reference values from BDA 1.13 are AIM-120
  25 kg, RBS-15 200 kg, Stingray 108 kg and AGM-86 1300 kg. Each of GBBM's 9 cluster
  submunitions explodes with the full `tntMass`.
  BDA's blast curve is very steep. A 221 kg AGM-84 does roughly 9k damage at 2 m from the hull,
  22k at 1 m and 85k at 0.5 m, divided by 1.18 for 25 mm of steel. A 60 000 HP destroyer
  therefore takes anywhere from one hit to about six, depending on where the missile detonates.
  The originals did about 10⁸, an instant kill whatever the HP. If ships die too fast or too
  slowly, tune the `%maxHitPoints` values in section 6 of `MNWS_BDA113.cfg`.

| Missile | Guidance (orig) | tntMass kg (orig → new) | maxStaticLaunchRange m (orig → new) | activeRadarRange m (orig → new) |
|---|---|---|---|---|
| AGM258 | Cruise | 100000 → 450 | 1200000 → 120000 | 2000000 → 25000 |
| AGM2588 | Cruise | 100000 → 450 | 1200000 → 120000 | 2000000 → 25000 |
| AGM46 | Cruise | 20000 → 100 | 800000 → 60000 | 2000000 → 20000 |
| AGM84 | Cruise | 20000 → 221 | 800000 → 80000 | 880000 → 20000 |
| AGM84BS | Cruise | 20000 → 221 | 800000 → 80000 | 880000 → 20000 |
| LRASM-A | Cruise | 50000 → 450 | 1200000 → 100000 | 20000 (same) |
| LRASM-AS | Cruise | 50000 → 450 | 1200000 → 100000 | 20000 (same) |
| LRASM-B | Cruise | 50000 → 450 | 1200000 → 100000 | 20000 (same) |
| LRASM-BS | Cruise | 50000 → 450 | 1200000 → 100000 | 20000 (same) |
| X51A | Cruise | 80000 → 200 | 1200000 → 100000 | 20000 (same) |
| GBBM | Cruise (cluster ×9) | 100000 → 50 each | 1200000 → 60000 | 2000000 → 20000 |
| BMM | AGM | 100000 → 700 | 1500000 → 200000 | 750000 → 20000 |
| SDB28 | AGM | 2000 → 17 | 120000 → 60000 | 5000 (same) |
| CUDA | AAM | 5000 → 8 | 50000 → 25000 | 500000 → 10000 |
| HHQX | AAM | 5000 → 8 | 15000 (same) | 500000 → 10000 |
| HHQ16C | AAM | 10000 → 20 | 80000 → 40000 | 500000 → 15000 |
| HHQ16CPBR | AAM | 30000 → 20 | 80000 → 40000 | 500000 → 15000 |
| HHQ17 | AAM | 30000 → 40 | 400000 → 60000 | 500000 → 20000 |
| HHQ19 | AAM | 30000 → 60 | 1200000 → 120000 (min 20000 → 5000) | 5000000 → 30000 |
| HHQ19RCS | RCS | 100000 → 5 | 2000000 → 150000 | 20000000 → 30000 |
| RR120 | AAM | 8000 → 8 | 300000 → 40000 | 100000 → 15000 |
| RR121 | AAM | 8000 → 40 | 300000 → 60000 | 100000 → 20000 |
| SAS | AAM | 5000 → 8 | 20000 (same) | 5000 (same) |
| SM_AADRM | AAM | 5000 → 8 | 500000 → 80000 | 250000 → 20000 |
| SM_KKV | AAM | 50000 → 10 | 1000000 → 150000 | 750000 → 30000 |
| SM_KV11 | RCS | 50000 → 5 | 1000000 → 150000 | 10000000 → 30000 |
| SM_LREW | AAM | 20000 → 20 | 1000000 → 150000 | 500000 → 30000 |
| SM_RAADRM | AAM | 2000 → 8 | 200000 → 40000 | 100000 → 15000 |
| SM_SADRM | AAM | 5000 → 20 | 500000 → 80000 | 250000 → 20000 |
| SM_SRADRM | AAM | 3000 → 20 | 350000 → 50000 | 150000 → 15000 |
| ASMM | AAM → SLW (compat) | 6000 → 45 | 5000 (same) | 1000 (same) |
| MK48 Torpedo | aam → SLW (compat) | 80000 → 300 | 50000 → 20000 | 50000 → 8000 |
| MK56 Torpedo | aam → SLW (compat) | 50000 → 200 | 50000 → 15000 | 50000 → 8000 |
| MK58 Torpedo | aam → SLW (compat) | 80000 → 300 | 50000 → 20000 | 50000 → 8000 |

* **Bullets.** The new values are in line with BDA's 35x228 and 203 mm shells.
  * 208mmRailgun: tntMass 800 → 25 kg.
  * 428mmRailgun: tntMass 2000 → 80 kg.
  * 35mmAAMS: bulletMass 0.2 → 0.51 kg, tntMass 0.5 → 0.1 kg, apBulletMod 0 → 0.8. The
    original round contained more TNT than it weighed.
* **ECM.** `rcsReductionFactor = 0` made a vessel completely invisible to radar while its jammer
  was on, so radar missiles could never lock. It is now 0.75 on the 27 MNWS parts that had 0:
  all ships, 3XR, CFA44/A, H20A/B, NH80, UCAV1/2, UH60 and V22. Jets with 0.2–0.7 are untouched.
* **Lasers** (`laserDamage` is damage per second; BDA's own lasers are about 1000):
  * AAMSLM 10000 → 3000
  * 3XLASER 5000 → 2500
  * FRADAR 30000 → 6000
  * MSWSWT4 10000 → 4000
  * AAMSL keeps 2000.
* **Sub-part hitpoints.** Turrets with 80 000 HP outlived the ships they were mounted on.
  * AAMS family: 80000 → 3000
  * 3X turrets, AS_ML, MSWSWT/1/2/3/4, MSWSM4X: 25000 → 4000
  * HHQXL/HHQXL2/HHQXX/SAAM: 8000 → 5000. FRADAR: 20000 → 5000.
  * AVLS11/22/33, AVLS11V/22V/33V, AVLSX1/2/3 and MissleCell2050: 6000. They had no explicit
    value before.
  * DDG2002 RAIL: 20000 → 8000. RGBB: 50000 → 15000.
  * Jets: 8000–15000 → 5000. H20A/B: 50000 → 12000. NH80, UCAV1/2, UH60 and V22: 4000.

## Not fixed and known limitations

* **The VLS variants and the bay links are untested in game.** KSP could not be run here. The MM
  result was checked with a simulator, and the geometry against the models. The transform timing
  and the BDA behaviour come from reading the KSP (decompiled) and BDArmory source.
* **One missile type per VLS block.** This is how BDA's MultiMissileLauncher works. Mixed loads
  need several blocks, or the rack versions.
* **AVLS VLS blocks are fixed launchers.** They have no turret, so `overrideReferenceTransform`
  aims by vessel heading. This is the same as BDA's own VLS.
* **Doors with two bays on one hull (SSBN002).** Both door events read "Open"/"Close" in the part
  menu, because BDA needs exactly that text. The action-group names stay distinct ("Toggle
  Missile Bay", "Toggle Torpedo Door").
* **Lookup spheres catch whatever is inside them.** A part surface-attached to the deck close to
  a bay can also be shielded. The SSBN002 sphere covers the sail. Shielded parts lose aero drag
  (irrelevant for ships). A shielded missile gets In Cargo Bay, so BDA opens that bay's door
  before firing it, which costs 2 s but is otherwise harmless.
* **CGNX2050 missile cells** are 2 m deep point-defence cells. A 13.8 m BMM on the four outer
  cell nodes is not covered by the bay, which is no loss.
* **Action-group doors (CGNX2050, CGX065 torpedoes)** toggle the whole AG8. If the doors are
  already open when BDA starts a launch, the toggle closes them. Leave them closed and let BDA
  handle them.
* **MissleCell2050** (50 cells) could become a VLS the same way, but it was not in scope.
* **Duplicate attach-node IDs** are left as they are. They occur on SSBN002, CGNX2050, AMB,
  AMBII, MSWS111, DDG030 (`node_stack_4`) and MissleCell2050 (50 × `node_stack_missile1`).
  Renaming them would break existing craft. When a craft reloads, parts on duplicate nodes can
  re-attach to the first node with that ID.
* **Submarine buoyancy.** The BDA AI steers depth with pitch only and does not manage ballast.
  Before handing SSBN002 to the AI, trim it close to neutral with the FastDive and FastSurface
  pumps.
* **The nine lifting-surface-only aircraft** (CFA44, CFA44A, H20A, H20B, UCAV1, UCAV2, UH60, V22,
  NH80) have no aerodynamic control surfaces. They never did in practice, because ModuleAileron
  never loaded. It was not converted, because their `ctrlSurfaceRange` and `ctrlSurfaceArea`
  values (for example 1° and 6) would give unpredictable results.
* **ACA module fields** (ReloadableRail, FuelNozzle, FuelCap and Tailhook, apart from the `15f`
  fix) could not be checked, because ACA is not installed.
* **Speeds and handling are untested in game.** KSP could not be run here. The scaling preserves
  the author's draft, acceleration and turning by construction. Top speeds are estimates.
* **Weapon ranges.** The railguns' 100–180 km `maxEffectiveDistance` is left alone. The BDA
  weapon manager caps gun range at 10 km anyway. The ship radars' 1200 km detection curves are
  also left alone, because detection beyond weapon range is harmless.
* Most hulls have `minimumCrew = 1`. BDA's spawner and a crewed launch both handle that.
* `BallastWater` is also defined by KerbalField. KSP logs a duplicate-definition warning, and both
  definitions are identical (1 t/unit).
* `test*.cfg` and `*PBR.cfg` are TexturesUnlimited shader configs. They do nothing without TU.

## How to test in game

1. **Load log.** Search `KSP.log` and `Logs/ModuleManager/ModuleManager.log` for `MNWS_BDA113`.
   There should be no MM errors. In `KSP.log` check the following:
   * There should be no `Cannot find a PartModule of typename 'MNWSCategoryModule'`,
     `'ModuleAileron'`, `'BurnTogether'`, `'RocketLauncher'` or `'BDACategoryModule'`.
   * `[BDArmory.BulletInfo]` should parse 208mmRailgun, 428mmRailgun and 35mmAAMS with no
     "Using default value" lines and no "def ... already exists".
   * Once ACA is installed, `Tailhook` should load without a `wiretension` parse error.
   * `[MNWSFix.ModuleInjectTransforms]: ...: created 'mnwsVLS' under 'model' with 8 children
     (missile1..missile8)`, and the same with 32 and 72 children, during loading. There should be
     no `ModuleInjectTransforms` errors, and no `Cannot find a PartModule of typename
     'ModuleInjectTransforms'` (which would mean the DLL is missing).
   * The ModuleManager log lists `MNWSFix` among the loaded assemblies.
2. **Editor.**
   * Place a DDG030. The part menu should show:
     * the Surface AI (Water, broadside)
     * the weapon manager
     * HP 60000
     * armour 25 mm Mild Steel, about 200 t
     * total mass about 2000 t plus fittings
     * a second radar, "MNWS Hull Sonar"
   * Put an AVLS11 on the hull, fill it with HHQ16C, and check that the door action reads
     "Open". CVN003 and CVN004 should now be controllable.
   * **VLS.** Place "AVLS22 VLS (32 cells, BDArmory launcher)" on a hull. Attach one HHQ16C to
     any cell node. The missile part should disappear, and 32 HHQ16C should appear standing in
     the cells, tails on the floor and noses below the doors. The part menu should show the
     loaded missile name, a salvo size slider (1–32) and the rail ammo. The launcher's "Open"
     button should swing the doors open, and "Close" should shut them. Repeat with AVLS11V and SM_KKV, and with AVLS33V and HHQX.
   * **Hull bays.** On SSBN002, AMB, MSWS111 and FDG057, the door's part-menu event should read
     "Open". On CGNX2050 and CGX065, the action group editor should show "Toggle Torpedo Doors
     (AG8)" in Custom 8 for a newly placed hull.
3. **Water handling.** Spawn a destroyer in the ocean (VesselMover is installed) and compare its
   waterline with the original. It should match. Check acceleration, turning and top speed.
4. **Surface battle.** Two teams, for example 055A and DDG030 against 065 and FDG060, guard mode
   on, AI on. Check the following:
   * The AVLS doors open just before each launch and close afterwards.
   * The Cruise missiles (AGM258 and others) do the pop-up attack.
   * With the balance file, ships take damage per hit instead of dying instantly. Expect one to
     several Harpoon-class hits per destroyer, and tune the hull HP to taste.
   * CIWS (AAMS) rounds air-burst near incoming missiles.
5. **Anti-submarine.**
   1. Trim SSBN002 near neutral, enable its AI (Submarine) and send it in.
   2. It should hold about −50 m and come up to −10 m to fire missiles.
   3. An escort's sonar should show the submarine as a contact.
   4. MK48/MK56/MK58 torpedoes from an escort or AS_ML/ASMM should home on it underwater.
6. **R276 pod.** It should ripple-fire and switch between S-8KOM and S-8DM rockets.
7. **Balance file.** Remove `MNWS_BDA113_Balance.cfg` and confirm that the compat file still
   loads cleanly on its own.
8. **VLS in combat.** Give a 055A two AVLS11V blocks (HHQ16C or SM_KKV) and one AVLS22V, guard
   mode on, against any target. Check the following:
   * Each block appears once in the weapon manager under the missile's name, with the cell count
     as ammo.
   * Before each salvo the doors open in about 1 s. Missiles leave the cells straight up, one
     every 0.5 s with `salvoSize` above 1, and do not hit the doors, the walls or each other. The
     doors close about 1.5 s after the last one.
   * The ammo count drops. After the last cell, the block reloads only if the vessel carries
     spare missiles, because `AccountForAmmo` is on.
   * If a missile spawns visibly off-centre or inside the floor, report the missile name. Turn on
     `debug = true` in the block's `ModuleInjectTransforms` to log the seat it used.
9. **Hull bays in combat.** For each hull, check that BDA opens the right door, waits about 2 s,
   fires, and closes the door again:
   * SSBN002 with BMM in the tubes, at launch depth
   * AMB/AMBII with HHQ16C in the canisters
   * MSWS111 with missiles on missile51–56
   * FDG057 with a torpedo launcher in the torpedo room
   * the CGNX2050 missile cells (HHQX or CUDA)

   `KSP.log` should show one `[MNWSFix.ModuleMNWSBayLink]: ... inCargoBay = true` line per
   missile.
10. **Action-group doors.** On a CGNX2050 or CGX065 with torpedo launchers in the side rooms,
    check that each torpedo's part menu shows Custom Bay = 8 in flight, that the log shows
    `customBayGroup = 8`, and that the side doors open before a torpedo launch. Then load an
    older craft of one of these hulls. The log should show `bound 'Toggle Torpedo Doors (AG8)' to
    Custom08`.
11. **Without the plugin.** Delete `Johnny/Plugins` and check the following:
    * MM still loads cleanly.
    * AVLS11V/22V/33V are gone, and the rack AVLS and hull bays remain.
    * With In Cargo Bay set by hand on a rack or bay missile, the doors still open.

## Notes for an upstream PR

The two `.cfg` files can be dropped into the pack's `MMPatches` folder as they are, with
`Plugins/MNWSFix.dll` and its source. If the author prefers to edit the part configs directly,
each patch block maps one-to-one onto edits in the named part's cfg, and each block's comment
gives the reason. No model or texture changes were made.

If the author re-exports the AVLS models with an empty `mnwsVLS` transform and children
`missile1..N` at the positions in section 9, the VLS parts no longer need the plugin. Drop the
`ModuleInjectTransforms` module from the patch. Similarly, unique hull door event names and
built-in container colliders would let the hull bays work without `ModuleMNWSBayLink`, except for
the In Cargo Bay timing, which is a BDA issue worth reporting upstream.
