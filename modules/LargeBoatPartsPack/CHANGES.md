# LargeBoatPartsPack: KSP 1.12.5 + BDArmory Plus 1.13 compatibility build

The ready-to-install copy is in `GameData\`, which holds four folders: `LShipPartsModern`, `LShipPartsRequired`, `LShipPartsWW2` and `SM_Industries`.
It contains no ModuleManager and no Firespitter, so install those yourself. ModuleManager is required. Firespitter is optional and only adds engine sounds and a deck texture switch.
`upstream.diff` is the unified diff of the original extracted folders against this copy.

## Changes per file

| File | Change | Why |
|---|---|---|
| `SM_Industries/Plugin/SMIndustries.dll` | **Removed** | Built for KSP 1.3.1 (AssemblyInfo says "SMIndustries for ksp 1.3.1 detune", runtime v2.0.50727). It calls `UnityEngine.Texture2D::LoadImage(byte[])`, which no longer exists in KSP 1.12's Unity (it moved to `ImageConversion`). It is called from `BaseFilter.GenIcon` (decompiled `SMIndustries/BaseFilter.cs:57,60`), which every editor filter uses, so it throws `MissingMethodException` when the editor builds its categories. The two archived builds have the same defect. |
| `SM_Industries/Plugin/SMIndustries 131 real.zip`, `SMIndustries pre removal spinners.zip` | **Removed** | Alternate builds of the same DLL with the same defect. KSP never loads zips anyway. |
| `LShipPartsRequired/LBP_tweakableComplete.cfg` | Lines 1-5 commented out with an explanation | `@PART[*]:HAS[@MODULE[BuoyancyControl]]:NEEDS[BuoyancyControl]:FINAL { !MODULE[BuoyancyControl]{} }` removed the BuoyancyControl module from **every** part that had one, in every mod, whenever BuoyancyControl was installed. No LBP part uses BuoyancyControl. The hulls use the stock `buoyancy` key. |
| same file | `%MODULE` changed to `%MODULE[FSengineSounds]` (LBPMdrive1) and `%MODULE[FStextureSwitch2]` (TriLCSHull) | A bare `%MODULE` edits the part's **first** MODULE instead of adding a new one, so with Firespitter installed these blocks overwrote an unrelated module. |
| `LShipPartsRequired/LBP_Hangar_MM.cfg` | Four bare `%MODULE` changed to `%MODULE[Hangar]` / `%MODULE[HangarStorage]` (mediumsternHangarShort/Long) | Same `%MODULE` bug. The blocks stay behind `:NEEDS[Hangar]`, as all Hangar blocks in this file already were. |
| same file | `@PART[vetteHangar]` changed to `@PART[vetteHangar]:NEEDS[TweakScale]` | It was the only TweakScale block without a guard, so it added a `TweakScale` module even when TweakScale is not installed. |
| `LShipPartsRequired/Patches/zz_KSP1125_BDA113_Compat.cfg` | **New** | See the next section. |

### What the new patch file does

1. **VLS cells (BDA 1.13)**: the patch applies to ModernSuperVLS, SubHullVLSshort and VetteHullVLSshort, the three parts with `ModuleTurret` + `MissileTurret`, and is guarded with `:NEEDS[BDArmory]`. It sets `MissileTurret.fireFOV = 180`.
   - These cells are fixed turrets: 0 deg/s, yawRange 0, pitch 0. Before each launch, BDA 1.13 calls `AimMissileTurretAngle(..., angleThreshold = fireFOV)` (`BDArmory/Control/MissileFire.cs:2824`, threshold from `GetMissileTurretFireAngle`, line 2915). With the default fireFOV of 5 this waits the full turret timeout (at least 2 s) on every shot unless the target is straight overhead.
   - Point-defence missile launches use `TargetInTurretRange(mT.turret, mT.fireFOV, ...)` (lines 10414 and 10744). With fireFOV 5 that refuses any target more than 5° from vertical.
   - With 180 both checks pass, and the missile's own boresight check in `GetLaunchAuthorization` (line 10533) still applies.
   - All VLS module fields were checked against BDA 1.13: every `ModuleTurret` key and `MissileTurret` turretID, finalTransformName, deployAnimationName, deployAnimationSpeed, railLength, firePauseTime and activeMissileOnly still exist as `[KSPField]`.
2. **Submarine ballast** (replaces the old BuoyancyControl idea): adds KerbalPowers Naval `ModuleShipBallast` to the six submarine hull sections. It is guarded with `:NEEDS[Cavitation]`, which is KPN Naval's plugin assembly name.
   - Settings: maxBuoyancy = 0.6 (the stock hull value), minBuoyancy = 0.1, maxDepth = 300, pumpRate = 5, EC = 2.
   - With the pump off, the hulls behave as before.
   - Without KPN Naval, the hulls keep stock buoyancy only.
3. **SubmarineWaterJet + KPN Naval**: KPN's `JetEngine_Flooding.cfg` adds `ModuleEngineFlooding` (which sets `disableUnderwater`) to every `ModuleEnginesFX` that burns IntakeAir. The water jet uses IntakeAir as a workaround, so the patch removes that module from this one part with `:NEEDS[Cavitation]:AFTER[Cavitation]`. Without this, the sub jet shuts down when submerged.
4. **Static-scan fixes** (no gameplay change except the gimbal):
   - SubmarineWaterJet: `gimbalTransformName = nozzle` does not exist in the model, which only has `thrustTransform`, so the 28° gimbal never worked. It now uses `thrustTransform`.
   - SubmarineWaterJet: its EFFECTS sounds pointed at `SM_Marine/Sounds/*`, a mod not in this pack. They now point to the identical files in `LShipPartsRequired/Sounds/`.
   - DG10Kstern: `animationName = DGSternLights` changed to `DGsternLights`, the name in the model. Unity names are case-sensitive.
   - CruiserHeliHangar: typo in `lightName` fixed (`ight3` → `light3`).
   - Stock `SurfaceScanner`: removes the `FSCoolant` scanner that `SM_Industries/Resources/SMIresource.cfg` adds. FSCoolant is not defined by anything installed, so the stock part showed a dead readout. The `SMIdropro` scanner is kept.

## SMIndustries.dll decision: dropped

- **What it did for LBP**: it added the editor sub-filters "Festive Naval Engineering Co.", "SM Marine" and "SM Armory", plus an "SM Paintshop" toolbar button. No LBP part uses its `SMtextureswitch2` module.
- **Why dropping it is safe**: on 1.12.5 the filter icons already failed with `MissingMethodException` in `GenIcon`. KSP catches exceptions per event handler (`EventVoid.Fire`), so the only effect was log spam and broken or missing sub-categories.
- **Fallback**: stock *Filter by Manufacturer → Festive Naval Engineering Co.*, using the agent in `LShipPartsRequired/Agencies/Agents.cfg`. All parts are still in their normal stock categories.
- **Kept**: the `SM_Industries` folder still supplies localization strings (`#autoLOC_SMIndustries_*`, used by the titles of 133 of the 142 LBP parts), resources (BallastWater, HydraulicOil, SMIdropro) and sounds.
- **Caveat**: other SM mods (SM Marine, SM Armory) may expect this DLL. If you have them, use their own current builds.

## Checked and fine (no change)

- **Hulls**: they use stock `buoyancy`, and no hull carries a plugin buoyancy module.
- **Hangar**: every Hangar/HangarStorage block was already `:NEEDS[Hangar]`. The helper transforms (`trigger_hangar`, `hangar_space`) are trigger colliders or have no renderer.
- **Legacy `mesh = model.mu` mismatches** (11 parts): KSP loads the folder's single .mu, so nothing needed changing.
- **Stock modules used**: ModuleRCS with legacy `resourceName`, ModuleResourceIntake, ModuleLight and ModuleGimbal all still parse. ModuleResourceIntake tolerates the missing `intake` transform on the sub jet.
- **`MissileNodePatch.cfg`**: adds `node_stack_bottom` to BDA missiles so they can sit in the VLS cells.
  - The patch values match the BDA 1.13 models. For example, AGM-86 uses -2.4, and BDA's own `node_stack_base` is at -2.407.
  - Five targets (PEW*) do not exist and are no-ops.
  - RBS-15 and AGM-86 end up with two overlapping bottom nodes (`base` and `bottom`), which is harmless.
- **Duplicate `RESOURCE_DEFINITION`s**:
  - BallastWater is defined in `SMM_GlobalResources.cfg`, `SMIresource.cfg` and KerbalField. HydraulicOil is defined in the first two.
  - KSP logs "already contains definition" and keeps the first one (`PartResourceDefinitionList.Add`).
  - The definitions are identical apart from KerbalField's hsp, so this is harmless.
- **Missing FX models**: `SM_Marine/FX/TorpWake` and `ThrusterWake` (DG10KBow, DG10Kstern) and `StackSmoke17` (WW2FunnelStraight) are missing. The only effect is no wake/smoke visuals and a log line. They are not fixed because they need assets from SM Marine.
- **Missile nodes for stacking**: the VLS cells use `NODE{}` transforms (`missileNode1..6b`), which works on 1.12.

## Not verified

- **Nothing was run in game.** All findings come from decompiling (`ilspycmd`), a reference resolver against the 1.12.5 managed DLLs, `.mu` parsing and config parsing.
- **ModuleManager semantics** (bare `%MODULE`, `:NEEDS[Cavitation]`, the `!MODULE[...]:HAS[#ResourceName[FSCoolant]]` syntax, pass ordering) are based on MM's documented behaviour. They were not executed.
- **VLS launch quality with fireFOV 180**: this depends on each missile's `maxOffBoresight`. A missile with a small boresight launched vertically may waste energy turning. `fireFOV` is persistent, so **craft saved before installing this patch keep their old value (5)**. Raise it in the editor PAW (range 1-180).
- **Submarine ballast tuning**: the ModuleShipBallast values (0.1-0.6, 300 m) are a reasonable starting point, not tested.
- **BDA DLZ for VLS**: BDA computes the launch zone with `turret.yawRange` (0 here), which assumes the missile is already pointing at the target, so the predicted max range is optimistic for vertical launches. This would need a code change, so it was left alone.

## In-game test steps

1. Install ModuleManager, then this `GameData\` (and BDArmory 1.13, optionally KPN Naval / Hangar / TweakScale / Firespitter). Start KSP and check `KSP.log` / `ModuleManager.ConfigCache` for:
   - no `SMIndustries` / `MissingMethodException` errors;
   - the patch count includes `zz_KSP1125_BDA113_Compat.cfg`;
   - no MM errors in `LBP_*`.
2. In the VAB, open *Filter by Manufacturer → Festive Naval Engineering Co.* and confirm all LBP parts are listed.
3. Build a corvette with a VetteHullVLSshort.
   - Attach missiles (e.g. AIM-120 or RIM-116) to its cell nodes. Check the PAW shows **Fire FOV = 180** on a newly placed part.
   - Add a weapon manager and a radar. Put it on the water against a drone and confirm missiles launch without a ~2 s pause each and without turret-angle errors.
   - Repeat with point-defence missiles against incoming missiles.
4. Submarine, with KPN Naval:
   - Hull sections show *Enable Pump / Target Depth*. Submerge, then run the SubmarineWaterJet under water and confirm it keeps thrust (no flooding shutdown) and gimbals.
   - Without KPN Naval: the hulls show no ballast module and float as before.
5. DG10Kstern lights animate. CruiserHeliHangar shows all 6 lights. The stock SurfaceScanner has no FSCoolant readout.
6. With Firespitter: LBPMdrive1 plays FS engine sounds and TriLCSHull has the deck switch, and neither part's other modules are altered. With Hangar: mediumsternHangarShort/Long show hangar controls.
