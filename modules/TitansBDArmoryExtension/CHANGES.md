# Titans BDArmory Extension (TBAE) 0.1.4: KSP 1.12.5 / BDArmory Plus 1.13 compatibility

## Install

Copy `GameData\TBAE\` into KSP's `GameData\`. The pack's readme says to delete any old TBAE folder first. It needs ModuleManager and BDArmory Plus 1.13. TBAE ships no DLLs, ModuleManager or Firespitter.

## What changed

- **New file:** `GameData/TBAE/Patches/zz_TBAE_BDA113_Compat.cfg` (ModuleManager).
- **One original line edited:** `Parts/J-627/j627ares_wf.cfg`, line 1. MM can't patch another patch's header, so this one has to be an edit.

`upstream.diff` contains both. It applies with `patch -p1 --binary` from the folder that contains `GameData`, and the result is byte-identical to this bundle.

### 1. BULLET definitions (39, plus TBAE's copy of `def`)

- All 39 TBAE bullets used the pre-1.11 key `subProjectileCount` and had no `projectileCount`. BDA 1.13 logs "Old bullet def detected … Please upgrade your mod's bullet defs" for each one and falls back to `subProjectileCount` (`BulletInfo.ParseField`). The patch copies the value into `projectileCount` (`#$subProjectileCount$`) and removes the old key, so behaviour is identical. That includes the multi-pellet LBX-10 (6), LBX-20 (14) and SB Gauss (4).
- `guidanceRange` (all 39) and `incendiary` (the 4 PLSL bolts) are added with BDA's defaults; BDA warns when they're missing.
- TBAE ships a copy of BDA's default bullet `def`. BDA only uses the one in `BD_Bullets.cfg` and logs "Bullet definition def from TBAE_Bullets already exists, skipping". The patch deletes TBAE's copy only. It matches old-style keys and no `projectileCount`, so BDA's def and NKDR's def don't match.
- `55x432APIBullet` has `explosive = Standard` but `tntMass = 0`, so BDA treats it as a solid slug. It's left as is, because an API round is meant to be a slug.

### 2. Removed `airDetonation` converted to BULLET `fuzeType` (1 weapon)

The 40mm Medium Rifle (`bahaMRLR`) had `airDetonation = false` and `airDetonationTiming = false`. BDA removed those keys; fuzing is now set by the BULLET's `fuzeType`.

- `airDetonation = false` meant a plain impact-fuzed HE round.
- Its round `40x311LRround` is used by no other weapon. It's HE with `fuzeType = None`, which BDA 1.13 already treats as Impact.
- The patch sets `fuzeType = Impact` explicitly and deletes the two dead keys. Behaviour doesn't change.

### 3. Duplicate part names with BDA

TBAE's `Parts/radome166` redefines BDA's own `bdRadome1` and `bdRadome1inline`, with the same part names but different titles and radar stats. KSP registers only the first-loaded part per name (`PartLoader.APFinderByName`), so craft silently load the BDA radome instead of TBAE's, or the other way round.

The patch renames TBAE's copies, matching them by title ("LRR/AESA-113V3" and "LRR/AESA-113V1"):

- → `TBAE_bdRadome1`
- → `TBAE_bdRadome1inline`

BDA's radomes are untouched. `bdRadome2snub` (TBAE's third radome) was already unique.

### 4. Parts that never loaded (missing MODEL)

| part | was | now |
|---|---|---|
| `Aim-117A` | `MalFunc Industries/Parts/MK29/Rim162` (another mod) | `TBAE/Parts/Aim-117/Rim162`, the same file shipped in TBAE |
| `TBAESLDFplsl`, `…heavyrepeater`, `…lightrepeater`, `…repeater` | `Ace_Kombat/Parts/TCA-SPW/PLSL_TCA` (another mod) | `TBAE/Parts/SLDF PLSL (C)/PLSL_TCA`, shipped in TBAE together with its texture atlas |

Without the model, KSP logs "PartCompiler: Cannot clone model … as model does not exist" and drops the part.

### 5. Missing resources

Two resources came from other mods and are defined nowhere in TBAE or the installed GameData. Without them the guns had no ammo.

- `PLSLBolt` (the 4 PLSL guns)
- `LaserCharge` (Thermal Laser Cannon `bahaTLC`)

The patch defines both as massless, pumpable resources, with `:NEEDS[!Ace_Kombat]` so it doesn't clash if Ace Kombat is installed. Nothing in TBAE refills `PLSLBolt`, so the PLSL guns now spawn loaded (`amount = maxAmount`) instead of empty. That's also only without Ace Kombat.

### 6. Missing or wrong-case assets

KSP asset URLs are case-sensitive.

| part(s) | was | now |
|---|---|---|
| `baha23x126mmAmmo` | texture `TBAE/Parts/23x126mmAmmo/tex423mm` | `…/tex23mm` (shipped) |
| `76.2mm`, `Light20mm` | `TBAE/Sounds/Hydraulic/hydraulicloop` | `…/hydraulicLoop` |
| `harp155mmheavy` | `TBAE/Sounds/Hydraulics/hydraulicLoop` (no such folder) | `TBAE/Sounds/Hydraulic/hydraulicLoop` |
| `harp155mmheavy` | `explModelPath = North Kerbin Weaponry/effects/Explosion_Artillery` | `NKDR/effects/Explosion_Artillery` with NKD Reborn (you have it); otherwise `BDArmory/Models/explosion/explosionLarge` |
| `bahaAIM-120ER`, `bahaLAA-M2` | `BDArmory/Sounds/rocketloop2` (not in BDA) | `TBAE/Sounds/MissileEffects/rocketloop2` (shipped) |
| `bahaSLDFgauss`, `bahaSLDFgaussc`, `bahaSLDFSBgaussc`, `bahaZu23A` | `BDArmory/Parts/m230ChainGun/Sounds/…` | `…/sounds/…` |

### 7. Waterfall patch header (original file edited)

`Parts/J-627/j627ares_wf.cfg` started with:

```
@PART[j627ares]:NEEDS[Waterfall]:FOR[StockWaterfallEffects]
```

- `:FOR[X]` declares that X is installed. Without Stock Waterfall Effects, that wrongly switched on every other mod's `NEEDS[StockWaterfallEffects]` patches.
- It also applied templates (`stock-kerozine-turbofan-2`, `stock-kerozine-afterburner`) that only exist when Stock Waterfall Effects is installed.

It's now `:NEEDS[Waterfall,StockWaterfallEffects]:AFTER[StockWaterfallEffects]`. With your install (Stock Waterfall Effects is present) the result is the same.

### Left alone

- `ModuleTurret` keys `shortName` and `texutre` (sic) on 4 SLDF turrets. BDA ignores them, so they're harmless.
- `MissileLauncher` keys `terminalManeuvering` and `explSoundPath` (not a KSPField on MissileLauncher). Both are ignored, so they're harmless.
- `SRCM-09B AL` has `mesh = model.mu`, but its only model is `RBS-15-17.mu`. KSP ignores `mesh` when a part has no MODEL node and loads the folder's first `.mu` (`PartLoader.CompileModel` → `GetModelPrefabIn`). There is exactly one, so it works.
- `weaponType = cannon` is deprecated but still accepted by BDA 1.13 as ballistic.

## Verification done (static only)

- **Bullets:** every BULLET was checked against BDA 1.13 `BulletInfo`; after patching, no keys are missing or unknown.
- **Parts:** every PART was scanned for model, texture and sound paths (case-sensitive), bullet, ammo and resource references, and module keys against BDA 1.13 KSPFields. After patching, only the harmless keys above remain.
- **MM simulation:** the patch was run through a ModuleManager-subset simulator. Every block matched exactly its intended targets. With NKDR present, the `!NKDR` variant is correctly skipped.
- **Name clashes:** part, bullet and resource name collisions were checked against the installed GameData.

## Unverified

- Not run in KSP.
- The PLSL and TLC resource definitions (massless, pumpable) are my choice. The originals from Ace Kombat and others weren't available.
- Craft that used TBAE's radome under the old shared name will load BDA's `bdRadome1` or `bdRadome1inline`. Re-place the TBAE radome from the parts list; its title is unchanged.

## In-game test

1. Start KSP and open `KSP.log`. Check that:
   - there are no "Old bullet def detected" warnings for TBAE bullets;
   - there is no "Bullet definition def from TBAE_Bullets already exists";
   - there are no `Cannot clone model` lines for Aim-117A or the PLSL guns.
2. **Editor:** you should see two LRR/AESA-113V3 and V1 radomes next to BDA's AN/APG radomes, all as separate parts. Aim-117A and the four SLDF PLSL guns are present. The PLSL guns and the Thermal Laser Cannon show ammo in their PAW.
3. **Firing:**
   - Fire the 40mm Medium Rifle at a target; HE rounds detonate on impact.
   - Fire an LBX-10 and check it spreads 6 pellets.
   - Fire the 155mm heavy; it shows NKDR's artillery explosion.
4. **Turret sounds:** traverse the 76.2mm and Light 20mm turrets; you should hear the hydraulic loop.
