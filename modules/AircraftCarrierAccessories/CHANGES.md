# Aircraft Carrier Accessories (KFC): KSP 1.12.5 + BDArmory Plus 1.13 notes

The ready-to-install copy is `GameData\KFC\`. It contains no ModuleManager and no Firespitter. ModuleManager is needed for the patch file.
`upstream.diff` is the unified diff of the original `extracted\GameData` against this copy. It also contains the suggested ACA.dll code fix as comment text.

## Changes per file

| File | Change | Why |
|---|---|---|
| `KFC/Plugins/ACA.dll` | **Unchanged** | It loads on 1.12.5 (see below). |
| `KFC/Patches/zz_KSP1125_BDA113_Compat.cfg` | **New** | See the next two rows. |
| (patch) `@PART[Enterprise]:NEEDS[!Hangar]` | Removes `MODULE[Hangar]` and `MODULE[HangarStorage]` when the Hangar mod is not installed | `Enterprise.cfg:299-324` adds these modules with no guard. Without Hangar, KSP logs "Cannot find a PartModule of typename…" on every load. With Hangar installed nothing changes. The model has the transforms they use (`trigger_hangar`, `hangar_space`, `launch_position`). Liaoning has no Hangar modules. |
| (patch) `@PART[CLLS]` | Adds a second MODEL: the stock `Squad/Parts/Aero/airplaneFins/TailFin` at 0.001 scale | **Stopgap.** See the CLLS section below. Delete this block if you install an ACA.dll that has the null check. |

## ACA.dll load check on 1.12.5

- **Assembly**: ACA 1.0.0.0, TargetFramework .NET 4.5, runtime v4.0.30319.
- **References**: mscorlib 4.0, System.Core 4.0, Assembly-CSharp, and the split Unity modules `UnityEngine.CoreModule`, `PhysicsModule`, `AnimationModule` and `InputLegacyModule`. All of these exist in `KSP_x64_Data\Managed`.
- **Reference resolver**: every external type and member reference was walked against the 1.12.5 managed DLLs, including generic-base inflation. **579 resolved, 0 unresolved.**
- **Editor category**: `ACAEditorCategory` is a `KSPAddon(MainMenu)`. It adds an "Aircraft Carrier Accessories" sub-filter using `KFC/Textures/icon`, which exists. It does not call any removed Unity API.
- **Part modules**: MagneticBerth, Arrestwire, Catapult, CLLS, FixPoint, FuelCap, FuelNozzle, OLS, ReloadableRail, Tailhook, MagneticHook, Tiedown and Floatsystem all exist as classes in ACA.dll.
- **Transforms and textures**: every transform these modules look up exists in their .mu files, except CLLS `Base`. All `KFC/Textures/*` files referenced in code exist.

## CLLS stopgap

`CLLS.OnStart` calls `part.FindModelTransform("Base")` and then reads `BaseTransform.gameObject.GetComponent(MeshRenderer)` (decompiled `CLLS.cs:162,177`).

`CLLS.mu` has no transform named `Base`. Its transforms are LaunchSystem, Core, Guide1, Guide2, Instruction, Trigger and Collider. So OnStart throws a NullReferenceException in both the editor and flight.

KSP catches the exception per module and logs "Module CLLS threw during OnStart" (`Part.ModulesOnStart`). The rest of OnStart is skipped, so the saved Activate/Launcher state and the guide visuals are never applied. The PAW buttons still work.

The stock tail fin model contains `Base` with a MeshRenderer (checked by parsing TailFin.mu), so OnStart can complete. CLLS hides that renderer in flight itself. In the editor it is a 1 mm fin inside the block.

The proper fix is a code change in ACA.dll: null-check `BaseTransform` and `BaseRenderer`. The suggested code is in `upstream.diff`.

Upstream quirk, not changed: once OnStart completes, it *toggles* the saved state. `if (!IsActivated) Activate(); else Deactivate();` and the same pattern for Launcher/Arrester. A CLLS saved "active" therefore loads "inactive". Press Activate after loading.

## Other findings (no change)

- **Tail hooks**: `TailHook*.cfg:58` set `wiretension = 15f` / `30`, but `wiretension` is not a `[KSPField]`, so those lines are ignored. The `[KSPField] maxwiretension` (default 100) is the effective value. The `15f` suffix would not parse anyway. Left as shipped because changing it would change arrest behaviour.
- **Carriers**:
  - Enterprise: 94,780 t, buoyancy 0.8. Liaoning: 58,500 t, buoyancy 0.5.
  - Both are single parts that use stock buoyancy plus the `Floatsystem` module, which needs the `Draft` and `rudder` transforms. Both models have them.
  - Both engines burn LiquidFuel, so KerbalPowers Naval's IntakeAir flooding patch does not touch them.
- **Editor sub-category**: the ACA sub-category lists only parts with ACA modules. The carriers and the MagneticBerth (AGP) part appear only in their stock categories. This is cosmetic.

## Notes for BDArmory 1.13 naval play

- **Steering**: `Floatsystem` takes steering from `vessel.ctrlState.yaw` (`Floatsystem.cs:59,77`). BDA's Surface AI steers through yaw, so the AI can drive the carriers.
- **Damage**: each carrier is **one part**, so BDA gives it a single hit-point pool. There is no compartment damage: the ship dies all at once, and per-part flooding mods treat it as one hull. Check its HP in the editor PAW. For multi-part, damageable ships use LBP hulls.
- **ReloadableRail**: it spawns the part named in its persistent "Missile Assign" field, default `bahaAim120`. That part still exists in BDA 1.13 (`BDArmory/Parts/aim-120/amraam.cfg`). Whether a respawned missile registers correctly with a weapon manager was not tested.
- **Catapult**:
  - Arming and launching key off the aircraft's **Brakes** action group (`ActionGroups.groups[5]`). A BDA Pilot AI that releases brakes when it takes off will fire the catapult straight away.
  - `Catapult.OnFixedUpdate` uses `Wheel.vessel` with no null check (decompiled `Catapult.cs`, around line 117). If the hooked aircraft is destroyed during launch, expect NullReferenceException spam until the system resets.

## Not verified

- **Nothing was run in game.** The findings come from decompiling, reference resolution, .mu parsing and config parsing.
- **CLLS stopgap**: the extra-model workaround has not been seen working in game. The extra fin collider is 1 mm, which should be harmless, but this was not tested.
- **MM `:NEEDS[!Hangar]`**: this assumes the Hangar mod's assembly or folder is named `Hangar`, which is the case for allista's Hangar.
- **BDA interaction**: everything in the BDA notes above comes from reading code, not from play-testing.

## In-game test steps

1. Install ModuleManager and `GameData\KFC`, start KSP and open `KSP.log`. Check:
   - no `ACA` load or reflection errors;
   - without Hangar, no "Cannot find a PartModule of typename 'Hangar'";
   - no "Module CLLS threw during OnStart".
2. SPH/VAB:
   - The "Aircraft Carrier Accessories" sub-filter shows the accessory parts.
   - Place a CLLS. Its PAW should show Acceleration, and you should not see a fin. In flight, its PAW should show Deactivate and Launcher, because OnStart now completes and auto-activates it.
3. Load `KFC/Ships/SPH/CVN65 Enterprise.craft` (copy it to your save's `Ships/SPH`) or launch the Enterprise part on water. It should float level and steer with yaw.
4. Catapult and CLLS: hook a small plane, hold brakes until the light turns yellow, then release. The plane should launch.
5. Arrestor: land a TailHook plane on the wire. It should stop smoothly.
6. With BDA: put a carrier and a BDA-armed escort in a Surface-AI team and confirm the carrier steers under AI control. Optionally assign a ReloadableRail missile and fire it from a weapon manager.
