# Maritime Pack: KSP 1.12.5 NullReferenceException fixes

The pack was patched from the GitHub zip snapshot `Fengist-Maritime-Pack-2822417`.
The goal is to stop the NullReferenceExceptions (NREs) users report in KSP 1.12 while changing behaviour as little as possible.

| Output | What it is |
|---|---|
| `GameData\` | Ready-to-install copy of the pack with the fixes (`FengistAnim`, `Maritime Pack`, `MPUtils`). |
| `src\` | Patched C# sources for the four rebuilt plugins, plus SDK-style project files. |
| `upstream.diff` | Unified diffs against each upstream code base, split into 4 sections and ready for PRs. Binary DLL swaps are listed as text lines. |

**Install:** copy the three folders in `GameData\` into KSP's `GameData\`, overwriting any existing copies.
ModuleManager is required for the cfg fixes. It is not bundled, and neither is Firespitter. The pack never shipped either one, and no MM or Firespitter DLL exists anywhere in this tree.

## 1. What changed, file by file

The original sources the line numbers refer to:

| Plugin | Original source |
|---|---|
| MPUtils | `Fengist/MPUtils` @ 40615d6 |
| MPHover | `Fengist/MPHover` @ 6736813 |
| MPEngineSounds, FengistAnim | The DLLs shipped in the pack, decompiled with ILSpy. The public repos are older than the shipped binaries. |

### MPUtils.dll (`GameData\MPUtils\Plugins\`): rebuilt

The shipped DLL was built from MPUtils @ 40615d6. Evidence:
- The build stamp in the shipped `MPLog.dat` is `v0.1.8134.*`, i.e. 2022-04-09, the commit date.
- The KSP-visible API surface matches the shipped DLL exactly.

| File (upstream line) | NRE / bug | Fix |
|---|---|---|
| `MPSubComputer.cs:106` | The guard `!LoadedSceneIsFlight && rescount != 2` never returns in flight. With AutoKeel/AutoBuoy on and fewer than two CompressedWater tanks, or after a tank is destroyed or decoupled, `Part1`/`Part2` are null. The result is an NRE every frame (lines 159-286). | The guard becomes `!flight \|\| vessel == null`. Tanks are re-validated (non-null, distinct, still on this vessel) and re-scanned once; if that fails, auto modes switch off with an on-screen message. |
| `MPSubComputer.cs:82-84` | Blow Ballast dereferences `Part1`/`Part2` unchecked. | Same tank validation, plus a screen message. |
| `MPSubComputer.cs:126-127` | The EC drain reads `FlightGlobals.ActiveVessel`. It can be null, and it is the wrong vessel when the sub is not active. | Uses `this.vessel`; checks the resource id. |
| `MPSubComputer.cs:~173, ~195` | Copy/paste bug: the Part2 branch tested `p1amount`. | Now tests `p2amount`. |
| `MPSubComputer.cs` AutoKeel | `findAltitude` used the active vessel's body. | Uses this vessel's body. |
| `MPBuoyancy.cs:80-96` | This FixedUpdate runs on 23 hull parts every physics frame. It throws when `vessel` is null (part spawning, splitting or dying), when it runs before KSP has called `OnStart` (`forcePoint` null), and through `vessel.GetComponent<Rigidbody>()` (the root part's rigidbody, null while the root is destroyed or replaced, which is common in BDArmory fights). `forcePointName != string.Empty` is also wrong for an unset field (null). | Returns early on null vessel/body/part, on packed parts, or when there is no rigidbody. Caches `part.rb`. Falls back to `part.transform` for `forcePoint`. `up` is computed from the force-point position. `partInfo` is null-safe. Uses `IsNullOrEmpty`. |
| `MPFunctions.cs:383, 368, 389, 395-396` | `GetResourceID`/`GetResourceMax`/`GetResourceAmount`/`GetResourceSpace` dereference a null `PartResourceDefinition` or `PartResource` when a resource is undefined or missing on the part. | `GetResourceID` returns -1. A new null-safe `GetPartResource()` is used everywhere. Vessel/parts null checks in `GetResourcePart`/`Total`/`Max`. |
| `MPFunctions.cs:421` | `GetBody()` chains through `ActiveVessel.orbitDriver.referenceBody`. | Null-safe. |
| `MPFunctions.cs:106, 115, 136` | Log/config file IO could throw into game code (missing PluginData folder, read-only install). | `try/catch`; the directory is created; file logging turns itself off after the first failure; config falls back to defaults. `MPLoadIcons` defaults to true. |
| `MPFunctions.cs` | `findAltitude(Transform)` always used the active vessel's body. | New `findAltitude(Transform, CelestialBody)` overload, used by MPEngine and MPSubComputer. `GetPitch` and `DepthBelowKeel` handle a null vessel. |
| `MPSubGui.cs:23-24` | Uses `ActiveVessel` without a null check (vessel switch). It also uses window id 0, which collides with other mods. | Null-safe. Uses a unique window id. The static `guiActive` is reset on destroy. |
| `MPSub.cs:141-142` | `ConsumeResource` used `ActiveVessel` and always summed `pressureResource1`, whatever resource was requested. | Uses `this.vessel` and the requested resource; checks the id. Adds vessel/body null guards in `OnStart`/`FixedUpdate`/`manageIntakes`. |
| `MPEngine.cs:29` and `FCWSolarSteam.cs:16` | `vessel.name` in `OnStart` (vessel is null in the editor). | Guarded. MPEngine uses its own vessel's body for the submerged check. |
| `FCWSolarSteam.cs:52-55`, `MPStirling.cs:39-44` | `ActiveVessel` instead of the own vessel; resource id unchecked. | Uses `this.vessel`; checks the id. |
| `MPResDecay.cs:27-35` | Its FixedUpdate also ran in the editor, where `ActiveVessel` is null. | Flight-only; uses `this.vessel`; checks the id. (No Maritime Pack part uses this module.) |
| `MPIconify.cs:124-125` | `filters.Find(...)` can return null, giving an NRE in `AddCustomSubcategoryFilter`. The handler stays subscribed after the once-only addon is destroyed. Icons loaded through `new WWW(<plain path>)` without `file://` never loaded, and the "on" icon was read from the `_off` file. | Null checks and `try/catch`. Removes the handler before adding it. Icons are read with `File.ReadAllBytes` + `ImageConversion.LoadImage`. The editor subcategory icons now actually show. |
| `MPAnim.cs:27` (`ModuleLayeredAnimator`) | Re-declares `[KSPField] int layer`, hiding the identical field of KSP 1.12's `ModuleAnimateGeneric` (default 1 in both). It never calls `base.OnStart`, so the module is uninitialised and `anim` can be null. | The duplicate field is removed. Calls `base.OnStart`, then sets the layer only if the animation exists. |
| `MPAnim.cs:77-79, 144-145, 215, 252-344` (`MPanimEngine`) | `new ModuleEngines()`/`new ModuleResourceIntake()` (Unity warns for every instance). `partType = Engine` without an engine module, or with a missing thrust transform, throws. `aniEngine` is not checked. `rTotal / rMax` is NaN with no EC storage. | Fields start as null. Engine/transform guards. FixedUpdate returns when the required module or the animation is missing. NaN guard. |
| `MPFuelSwitch.cs:212, 243` | `UIPartActionController.Instance` can be null while a craft loads; `tankList` can be null. | Guarded. |
| `Properties\AssemblyInfo.cs` | | `AssemblyVersion` stays `0.1.*`. Added `AssemblyInformationalVersion("0.1-ksp1.12.5-nrefix.1")`. |

### MPHover.dll (`GameData\Maritime Pack\Plugins\`): rebuilt

The shipped DLL matches the MPHover repo source (checked against an ILSpy decompile; same API surface).

| File (upstream line) | NRE / bug | Fix |
|---|---|---|
| `MPHover.cs:220` | `applyHoverThrust` reads `FlightGlobals.ActiveVessel.rootPart`/`.mainBody`. That throws on every physics frame while there is no active vessel (vessel switch, active vessel destroyed) and any hovercraft has its lift engine on. It is also the wrong "up" for any hovercraft that is not the active vessel. | Uses the affected vessel's own position and body; guards null vessel/body/parts. |
| `MPHover.cs:153-213` | `checkHoverOn` walked every module of every vessel in the game every physics frame, calling `GetComponent` on each match. The prepared `allVesselsWithHE` list was never used, and was filled once per engine rather than once per vessel. | Iterates the cached list of loaded vessels with a hover engine; skips packed or destroyed vessels. The list is rebuilt when the vessel count changes and on `onVesselLoaded`/`Create`/`WasModified`/`Destroy` (events are removed in `OnDestroy`). Uses `as` casts instead of `GetComponent`. |
| `MPHover.cs:251, 310` | Division by the skirt-transform count (0 gives NaN, and `aveAlt` is persisted). | Returns early or skips when the count is 0; ignores destroyed transforms and rigidbodies; prefers the part's live `rb`. |
| `MPHover.cs:113-120` | `FlightGlobals.currentMainBody` is not checked. | Guarded. |
| `MPHover.cs:55-77` | `OnInitialize` uses `vessel.parts` unchecked. | Guarded. |
| `Properties\AssemblyInfo.cs` | | FileVersion 1.0.1.0, plus an InformationalVersion. AssemblyVersion is unchanged. |

### MPEngineSounds.dll (`GameData\MPUtils\Plugins\`): rebuilt from the decompiled shipped DLL

| File (decompile line) | NRE / bug | Fix |
|---|---|---|
| `MPengineWrapper.cs:248` | On a part with no engine module, `type` was still set to `ModuleEngineFX`, so every getter dereferenced a null `engineFX`. This throws in `OnStart` (`maxThrust`) and in every FixedUpdate. None of the 8 Maritime Pack users trigger it (all have engines), but any other part using the module would. | `type = NONE` when no engine is found. |
| `MPengineSounds.cs:126` | Unset sound fields are null, not `string.Empty`; the FXGroup was not checked. | `IsNullOrEmpty` and a null-group check. FXGroups are created if missing. |
| `MPengineSounds.cs:298, 317, 329`; `MPengineWrapper.cs:212` | `finalThrust / maxThrust` gives a NaN audio pitch when `maxThrust` is 0. | New `ThrustFraction()` returns 0 in that case. |
| `MPengineSounds.cs` FixedUpdate | | Null-part guard. |
| `Properties\AssemblyInfo.cs` | | FileVersion 1.0.1.0, plus an InformationalVersion. |

### FengistAnim.dll (`GameData\FengistAnim\`): rebuilt from the decompiled shipped DLL

| File (decompile line) | NRE / bug | Fix |
|---|---|---|
| `FAnimEngine.cs:77, 81` | `new ModuleEngines()` / `new ModuleResourceIntake()`. | Fields start as null. |
| `FAnimEngine.cs:147-148` | `myEngine.thrustVectorTransformName` / `mytransform.localRotation` throw in `OnStartFinished` when `partType = Engine` but the engine or transform is missing. | Guarded, with a log line. |
| `FAnimEngine.cs:217` | `CheckDirection` with a null `mytransform`. | Returns the last direction. |
| `FAnimEngine.cs:281, 287, 294, 343, 439` | FixedUpdate dereferences `myEngine`/`myIntake`/`aniEngine` without checks (missing module, or an animation name not in the model), every physics frame. Battery `EC/EC-max` is NaN. The vessel is not checked. | Early return when the required module or the animation is missing. Null-safe throttle read. NaN guard. Vessel guard. |
| `ModuleLayeredAnimator.cs:6, 10` | Same hidden `layer` field and missing `base.OnStart` as in MPUtils. | Same fix. |
| `Properties\AssemblyInfo.cs` | | FileVersion 1.0.1.0, plus an InformationalVersion. |

### FSResizer.dll: kept as shipped

Reasons it was not rebuilt:
- Every type and member reference it uses resolves against the KSP 1.12.5 `Managed\` assemblies (Mono.Cecil check, 0 unresolved).
- Its ILSpy decompile compiles cleanly against 1.12.5.
- The 16 hull parts use only `FSPartResizer`, and no NRE path was found in the code they reach.
- Rebuilding from a decompile would add risk for no gain.

### New: `GameData\Maritime Pack\Patches\zz_MP_KSP1125_Compat.cfg` (ModuleManager)

Every patch is guarded with `:NEEDS[MPUtils]` and runs in the `:AFTER[MPUtils]` pass.
1. **Sound typo.** `FS_SubmarineElecEngine1` and `FS_SubmarineElecEngine125` have `flameout = Maritime Pack/Sounds/EleclStop`, a file that does not exist. The patch changes it to `.../ElecStop`. This was the only missing asset found in the pack.
2. **MPSub `maxDepth` → `MaxDepth`.** The Bathysphere and Sub Conning cfgs set `maxDepth = 700`, but the field is `MaxDepth` (default 400 m, adjustable 400-2000 m in the editor, persistent). The value was therefore ignored. The patch copies it to `MaxDepth` and deletes the misspelled key, but only if `MaxDepth` is not already set. This is a deliberate behaviour change: these two parts now default to 700 m, as the author intended. Craft saved earlier keep their stored value.
3. **Duplicate `Water` definition.** `MPUtils\Resources.cfg` redefines `Water` (STAGE_PRIORITY_FLOW), which CommunityResourcePack already defines (ALL_VESSEL). When CRP is installed, the duplicate is removed (`:NEEDS[MPUtils&CommunityResourcePack]`). CRP's definition already won, so the only effect is that the duplicate-definition log error goes away.

### Removed

- `GameData\MPUtils\Plugins\PluginData\MPLog.dat`: the author's leftover runtime log (one line). MPUtils recreates it.

## 2. cfg audit (KSP 1.12.5)

Modules the pack's cfgs actually use:

| Module | Parts |
|---|---|
| MPbuoyancy | 23 |
| FSPartResizer | 16 |
| FAnimEngine | 12 |
| MPengineSounds | 8 |
| MPEngine | 7 |
| MPHoverSkirt | 3 |
| MPSub | 2 |
| MPfuelSwitch | 2 |
| MPHoverEngine | 2 |
| MPSubComputer | 1 |

- Every other module used is stock. No Firespitter module is used.
- Every module, field type, animation name and transform name was checked against the code and the `.mu` files. Everything resolves except the items below.

Harmless and left alone:
- `MPSub` has `manageThrottle` (Sub Conning), which is not a field. It is ignored.
- `MPEngine` has `shutdownStatus` (3 parts), which is not a field. It is ignored; the default `shutdownUnder = false` already gives the intended "out of water" behaviour.
- `FS_SubmarineElecEngine1` declares `mesh = model.mu`, but its folder only holds `model125.mu`. KSP falls back to the folder's model, so it loads.
- `MPUtils\Plugins\PluginData\CVFlightDeck\config.xml` is a leftover from another mod. It is unused.

## 3. Build

| | |
|---|---|
| Tool | `dotnet build -c Release` (.NET SDK 10.0.403, MSBuild 18.9) |
| Target | `net472` (SDK-style projects, `LangVersion latest`) |
| References | `D:\Epic Games\KerbalSpaceProgram\English\KSP_x64_Data\Managed\` (Assembly-CSharp, -firstpass, UnityEngine + Core/Physics/Animation/Audio/IMGUI/ImageConversion modules, UnityEngine.UI), all `Private=false` |
| Result | All four projects build with **0 errors and 0 warnings**. Suppressed in `Directory.Build.props`: CS0618, CS0649, CS0414, CS0169 (obsolete-API and unused-field noise from the original code). |
| Output | Only the four plugin DLLs were copied into `GameData` (no PDBs, no KSP or Unity DLLs). |

To rebuild elsewhere, run this inside `src\<Project>\`:

```
dotnet build -c Release -p:KSPRoot="C:\path\to\KSP"
```

Static checks on the rebuilt DLLs:
- **Mono.Cecil reference check:** 0 unresolved type or member references against the 1.12.5 `Managed\` folder.
- **API comparison with the shipped DLLs:** every class name, KSPField (including UI attributes), KSPEvent and KSPAction is unchanged. The only removals are the duplicate `layer` fields (now inherited from the base class with the same name and default). The only additions are `findAltitude(Transform, CelestialBody)`, `GetPartResource`, and MPHover's `Start`/`OnDestroy`. Existing saves and craft files load unchanged.
- **`upstream.diff`:** each section applies to its original source with `patch -p1 --binary`, and the result is byte-identical to `src\`.

## 4. Not verified

- **No in-game run.** All fixes come from static analysis plus compilation and metadata checks against 1.12.5.
- **Forum reports.** The KSP forum thread with the users' NRE stack traces could not be read (HTTP 403 / bot check), so the root causes are inferred from the code, not matched to their logs. The most likely user-facing sources are:
  - MPbuoyancy: 23 parts, every physics frame, fails during part spawn, split or destruction.
  - MPHover: active vessel null.
  - MPSubComputer: auto modes running without two ballast tanks.
  - MPIconify: editor category.
- **MPHover.** The hover-thrust refactor (per-vessel "up", cached vessel list, skipping packed vessels) has not been flown. Hover feel could differ slightly for a hovercraft that is not the active vessel, which previously used the active vessel's "up".
- **FSResizer.** Runtime behaviour is unchanged and untested.
- **MaxDepth patch.** Behaviour at 700 m is untested.
- **Two cosmetic changes, also untested:** the editor subcategory icons now load, and the dive-computer window uses a new window id.
- **Pre-existing quirks, not changed:** `MPHoverEngine.OnInitialize` marks the vessel LANDED and puts it to the ground on every load. `FAnimEngine` with `partType = Throttle` and no `syncThrottle` keys its animation to 0.

## 5. In-game test steps (KSP 1.12.5 + ModuleManager [+ CRP, BDArmory])

1. **Load.** Install, start KSP, and check `KSP.log`:
   - No `NullReferenceException` or `[ERR]` from MPUtils, MPHover, MPEngineSounds or FengistAnim.
   - The ModuleManager log shows the `zz_MP_KSP1125_Compat` patches applied.
   - With CRP installed, there is no duplicate `RESOURCE_DEFINITION` "Water" message.
2. **Editor.**
   - Open the "Filter by Function" category: the Maritime Pack, Kerbal Electric, Lighter than Air, 1869 and Fonts subcategories appear with icons.
   - Place the Sub Conning tower: its "Max Depth" slider shows 700.
   - Place a hull and resize it (FSResizer). Place hover, paddle and sub engines. Expect no log errors.
3. **Boat.** Launch a hull + Clipper engine + paddlewheel boat into the ocean:
   - It floats.
   - Engine sounds play; the throttle changes the pitch.
   - Animations run.
   - Switch to another vessel and back; no NRE spam.
4. **Submarine** (hull + Sub Conning + two CompressedWater ballast tanks + electric stern engine):
   - Dive with AutoKeel/AutoBuoy.
   - Blow Ballast.
   - Open the dive-computer GUI.
   - Then decouple or destroy one ballast tank while AutoBuoy is on. Expect the message "AutoKeel/AutoBuoy need two CompressedWater ballast tanks..." and no NRE.
   - Flame out the electric engine (drain EC). The ElecStop sound plays.
5. **Hovercraft** (lift engine + skirts):
   - Activate the lift engine; it hovers over land and water.
   - Spawn a second hovercraft, switch between them, and check that both still hover correctly.
   - Destroy the active vessel while a hovercraft is loaded: no NRE in FixedUpdate.
6. **BDArmory.** Fight with Maritime Pack boats (root part destroyed, parts blown off, vessels splitting):
   - `KSP.log` shows no NREs from MPbuoyancy, MPHover or MPEngineSounds.
   - Debris hull pieces keep floating or sinking normally.
