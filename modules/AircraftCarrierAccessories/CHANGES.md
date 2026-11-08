# Aircraft Carrier Accessories (KFC): KSP 1.12.5 + BDArmory Plus 1.13 notes

The release zip holds the complete mod: `GameData/KFC` (with the rebuilt `ACA.dll` and one
ModuleManager patch) and `Ships/SPH/CVN65 Enterprise.craft`. It contains no ModuleManager and
no Firespitter. `upstream.diff` is the text diff against the original 1.9.1 release, with a
summary of the `ACA.dll` changes in its header.

## 0.1.2

- **Full build instead of a patch.** The module now ships the whole of ACA 1.9.1 and
  provides/conflicts with CKAN's `AircraftCarrierAccessories` (see `README.md` for why and how
  to upgrade). The file set matches CKAN's original install: `KFC/Ships` goes to `Ships/`,
  `KFC/Resource/ResourcesGeneric.cfg` is left out (`delete.txt`).
- **`ACA.dll` rebuilt** from the 1.9.1 DLL decompiled with ILSpy (`src/ACA/`; the public GitHub
  source is the older 1.5.1). Changes:
  - `CLLS.OnStart` null-checks the `Base` transform (see below). The tail-fin MODEL stopgap is
    removed from the patch.
  - `Catapult` reads a configurable action group of the hooked aircraft instead of
    `ActionGroups.groups[5]` (Brakes): new persistent field `launchGroup` ("Arm/launch with
    aircraft" in the part menu; AG1 to AG10 or Brakes; default `Custom07`).
  - With a custom group, `Catapult.OnTriggerStay` only hooks aircraft that have the group
    switched on. Otherwise an AI aircraft taxiing over an active catapult would be grabbed and
    held on the shuttle, since the AI never arms it. With Brakes, any grounded wheel is hooked,
    as in the original.
  - `Catapult.OnFixedUpdate` resets the shuttle when the hooked aircraft, its wheel or its
    rigidbody disappears (destroyed or recovered mid-launch). The original used `Wheel.vessel`
    with no check and threw every physics frame.
  - `Catapult.GetInfo` describes the selected group (editor part info).
  - `AssemblyInformationalVersion` is `1.9.1-skizzy.1`. Assembly name, version (1.0.0.0),
    namespaces, class names and every existing field are unchanged, so craft and saves load
    as before.
- **Patch file**: the catapult's description explains AG7, and `%launchGroup = Custom07` states
  the default.

### Why AG7 and not AG10

BDArmory toggles AG10 itself: at competition start (`BDACompetitionMode`) and when a pilot AI
wakes from standby (`BDModulePilotAI`). It uses AG8 and AG9 in tag mode, and custom competition
commands can toggle any group. AG7 is never toggled by BDArmory on its own.

## Changes per file

| File | Change | Why |
|---|---|---|
| `KFC/Plugins/ACA.dll` | **Rebuilt** | CLLS start-up fix and catapult launch group (above). |
| `KFC/Patches/zz_KSP1125_BDA113_Compat.cfg` | **New** | See the next two rows. |
| (patch) `@PART[Enterprise]:NEEDS[!Hangar]` | Removes `MODULE[Hangar]` and `MODULE[HangarStorage]` when the Hangar mod is not installed | `Enterprise.cfg:299-324` adds these modules with no guard. Without Hangar, KSP logs "Cannot find a PartModule of typename…" on every load. With Hangar installed nothing changes. The model has the transforms they use (`trigger_hangar`, `hangar_space`, `launch_position`). Liaoning has no Hangar modules. |
| (patch) `@PART[Catapult]` | New description; `%launchGroup = Custom07` | Explains how to arm and launch. |
| `KFC/Resource/ResourcesGeneric.cfg` | **Left out** | CKAN's original install filters `KFC/Resource`. It defines AntiProton and GNparticle, which no ACA part uses. |
| `KFC/Ships/SPH/CVN65 Enterprise.craft` | Moved to `Ships/SPH/` | Same as CKAN's original install. |

## CLLS start-up crash (fixed in 0.1.2)

`CLLS.OnStart` calls `part.FindModelTransform("Base")` and then reads
`BaseTransform.gameObject.GetComponent(MeshRenderer)`. `CLLS.mu` has no transform named `Base`
(its transforms are LaunchSystem, Core, Guide1, Guide2, Instruction, Trigger and Collider), so
OnStart threw a NullReferenceException in the editor and in flight. KSP logged "Module CLLS
threw during OnStart" and skipped the rest of OnStart, so the saved Activate/Launcher state and
the guide visuals were never applied. The rebuilt DLL skips the renderer when the transform is
missing; `Base` was only used to hide that renderer in flight.

Upstream quirk, not changed: once OnStart completes, it *toggles* the saved state
(`if (!IsActivated) Activate(); else Deactivate();`, and the same for Launcher/Arrester). A
CLLS saved "active" therefore loads "inactive", and a new one starts active. Press Activate
after loading if needed.

## Verification

- **Decompile round trip.** The original 1.9.1 DLL (md5 `be1e47460c3a8cab8c8e24211ff0855f`) was
  decompiled, rebuilt and decompiled again. After normalising cosmetic compiler differences
  (default field initialisers, IL offset comments), the only differences between the original
  decompile and the rebuilt DLL's decompile are the changes listed above.
- **IL call check.** No `callvirt` in the original became a `call` in the rebuild (or the
  reverse), compared method by method in the IL.
- **References** are the same set as the original (mscorlib, System.Core, Assembly-CSharp,
  `UnityEngine.CoreModule`, `PhysicsModule`, `AnimationModule`, `InputLegacyModule`), all in
  KSP 1.12.5's `KSP_x64_Data\Managed`.
- **Local paths.** The DLL has no debug directory and no embedded local paths (scanned).
- **Deterministic.** Two builds give the same bytes (`ACA.dll` sha256 `8FA2E460…9EB23`).
- **Package.** The staged release matches `manifest.json`: 87 files, the original files
  byte-identical except `ACA.dll`.

## Other findings (no change)

- **CLLS uses Brakes.** CLLS in launcher mode arms and fires from the aircraft's Brakes, like
  the original catapult did. It was not changed; keep CLLS blocks off taxiways used by AI
  aircraft, or switch them to Arrester.
- **Tail hooks**: `TailHook*.cfg:58` set `wiretension = 15f` / `30`, but `wiretension` is not a
  `[KSPField]`, so those lines are ignored. The `[KSPField] maxwiretension` (default 100) is the
  effective value. Left as shipped because changing it would change arrest behaviour.
- **Carriers**:
  - Enterprise: 94,780 t, buoyancy 0.8. Liaoning: 58,500 t, buoyancy 0.5.
  - Both are single parts that use stock buoyancy plus the `Floatsystem` module, which needs
    the `Draft` and `rudder` transforms. Both models have them.
  - Both engines burn LiquidFuel, so KerbalPowers Naval's IntakeAir flooding patch does not
    touch them.
- **Editor sub-category**: the ACA sub-category lists only parts with ACA modules. The carriers
  and the MagneticBerth (AGP) part appear only in their stock categories. This is cosmetic.

## Notes for BDArmory 1.13 naval play

- **Steering**: `Floatsystem` takes steering from `vessel.ctrlState.yaw`. BDA's Surface AI
  steers through yaw, so the AI can drive the carriers.
- **Damage**: each carrier is **one part**, so BDA gives it a single hit-point pool. There is no
  compartment damage. For multi-part, damageable ships use LBP hulls.
- **ReloadableRail**: it spawns the part named in its persistent "Missile Assign" field, default
  `bahaAim120`. That part still exists in BDA 1.13. Whether a respawned missile registers
  correctly with a weapon manager was not tested.

## Not verified

- **Nothing was run in game.** The findings come from decompiling, rebuilding, reference
  checks, .mu parsing and config parsing.
- **Catapult launch group** in flight, including the part-menu selector and the abort.
- **CKAN upgrade from 0.1.1**: the steps in `README.md` come from reading the CKAN 1.36.4
  source, not from running CKAN.
- **MM `:NEEDS[!Hangar]`** assumes the Hangar mod's assembly or folder is named `Hangar`, which
  is the case for allista's Hangar.

## In-game test steps

1. Install the module, start KSP and open `KSP.log`. Check:
   - no `ACA` load or reflection errors;
   - without Hangar, no "Cannot find a PartModule of typename 'Hangar'";
   - no "Module CLLS threw during OnStart".
2. SPH/VAB:
   - The "Aircraft Carrier Accessories" sub-filter shows the accessory parts.
   - Place a catapult. Its part info and part menu show "Arm/launch with aircraft: AG7".
   - Place a CLLS. In flight, its part menu should show Deactivate and Launcher.
3. Load `CVN65 Enterprise` from the SPH or launch the Enterprise part on water. It should float
   level and steer with yaw.
4. Catapult: stop a small plane's wheel on the shuttle, press 7 (light turns yellow), press 7
   again. The plane should launch. Repeat with AG7 off: the plane is not hooked.
5. With BDA: let an AI aircraft taxi over an active catapult. It should not be hooked.
6. Arrestor: land a TailHook plane on the wire. It should stop smoothly.
