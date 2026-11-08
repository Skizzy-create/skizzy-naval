# Aircraft Carrier Accessories (SkizzyNaval build)

CKAN: `SkizzyNaval-AircraftCarrierAccessories` | strategy: **rebuild** | licence: **MIT**

The complete Aircraft Carrier Accessories 1.9.1 by flywlyx with a rebuilt `ACA.dll`:

- **CLLS** no longer throws a NullReferenceException in `OnStart` (its model has no `Base`
  transform). The tail-fin MODEL stopgap from 0.1.1 is gone.
- **Catapult** hooks, arms and fires from an action group of the aircraft, **AG7 by default**,
  instead of its Brakes. BDArmory's pilot AI sets and releases the brakes by itself, so in the
  original an AI aircraft that rolled onto a catapult was hooked, armed and launched. The group
  is a per-catapult part-menu setting; "Brakes" gives the original behaviour back.
- **Catapult** drops a hooked aircraft that is destroyed or recovered mid-launch and resets,
  instead of throwing a NullReferenceException every physics frame.
- The Enterprise's Hangar modules are removed when the Hangar mod isn't installed (patch).

Details, verification and limits: `CHANGES.md`.

## Using the catapult

1. Stop the aircraft with a wheel on the catapult shuttle.
2. Switch the aircraft's **AG7** on (key **7**). The shuttle hooks the wheel, and the light
   turns yellow when it is armed.
3. Switch AG7 **off** to launch. Switching it on again during the stroke aborts.

Aircraft with AG7 off are not hooked, so AI aircraft can taxi across an active catapult. To
use another group, open the catapult's part menu (editor or flight) and change
**"Arm/launch with aircraft"** (AG1 to AG10, or Brakes). AG10 is a poor choice with BDArmory:
BDArmory toggles AG10 at competition start and when an AI wakes from standby. CLLS (the
"Carrier Landing And Launching System" block) still uses Brakes, as in the original.

## Why a full build, and how it fits with CKAN

The fix is in `ACA.dll`, which CKAN's `AircraftCarrierAccessories` module owns. CKAN does not
let two modules install the same file, so an overlay that replaces the DLL would fail with a
file conflict. This module is therefore the whole mod (MIT allows that):

- it **provides** `AircraftCarrierAccessories`, so mods that depend on or suggest ACA are
  satisfied by it;
- it **conflicts** with `AircraftCarrierAccessories`, so CKAN never installs both.

It installs the same files as CKAN's original: `GameData/KFC` without `KFC/Ships` and
`KFC/Resource` (`delete.txt`; the resource file defines AntiProton and GNparticle, which no ACA
part uses), and the Enterprise craft in `Ships/SPH`.

**Upgrading from 0.1.1.** Up to 0.1.1 this module was a patch that depended on the original.
`ckan upgrade --all` does not move it to 0.1.2, because CKAN skips upgrades that conflict with
an installed module. Upgrade it by naming the version:

    ckan upgrade SkizzyNaval-AircraftCarrierAccessories=0.1.2

CKAN lists "Auto-removing Aircraft Carrier Accessories 1:1.9.1" (the original was installed as
a dependency of 0.1.1) and asks to continue. If you installed the original yourself, CKAN
reports a conflict instead; run `ckan mark auto AircraftCarrierAccessories` first. Avoid
`ckan remove AircraftCarrierAccessories`: it would also remove 0.1.1 and the Skizzy Naval Pack,
which depend on it.

| Path | Contents |
|---|---|
| `GameData/KFC/Plugins/ACA.dll` | Rebuilt plugin (replaces the original) |
| `GameData/KFC/Patches/zz_KSP1125_BDA113_Compat.cfg` | Enterprise Hangar guard; catapult default group and description |
| `delete.txt` | Original file left out (`KFC/Resource/ResourcesGeneric.cfg`) |
| `src/ACA/` | Source of `ACA.dll`: the 1.9.1 DLL decompiled with ILSpy, plus the fixes |
| `upstream.diff` | Text diff against the original, and a summary of the `ACA.dll` changes |

**Building `ACA.dll`.** `dotnet build src/ACA/ACA.csproj -c Release -p:KSPRoot="<KSP 1.12 folder>"`
(or set `KSPDIR`). Release builds are deterministic and carry no debug information or local
paths.

**CKAN note.** The main repository marks ACA 1.9.1 as KSP 1.10.0 only; this build is marked
for KSP 1.12, like the rest of the pack.
