"""Generate HullBreach 1.8 MM patches and validate part names against the read-only part surveys.
Usage: python -I gen_hb_patches.py <survey_dir> <out_patches_dir>

<survey_dir> holds tab-separated part surveys (columns: pack, file, name, ...) named survey.tsv,
survey2.tsv, survey_stock.tsv, plus bda_core_names.txt (one installed BDArmory part name per line).
They were produced read-only from the pack downloads and GameData. Any listed name that matches no
surveyed part is reported as WARN; the 1.8.0 patches generated with zero warnings.
"""
import csv, fnmatch, os, sys

SCRATCH, OUT = sys.argv[1], sys.argv[2]

def load(path, packs=None):
    names = {}
    with open(path, encoding='utf-8', errors='replace') as f:
        r = csv.reader(f, delimiter='\t')
        next(r)
        for row in r:
            if len(row) < 3:
                continue
            if packs and row[0] not in packs:
                continue
            names.setdefault(row[0], set()).add(row[2])
    return names

s1 = load(os.path.join(SCRATCH, 'survey.tsv'))
s2 = load(os.path.join(SCRATCH, 'survey2.tsv'))
s3 = load(os.path.join(SCRATCH, 'survey_stock.tsv'))
ALL = {}
for d in (s1, s2, s3):
    for k, v in d.items():
        ALL.setdefault(k, set()).update(v)

# installed BDArmory core names (read from GameData by the caller, one per line)
core_file = os.path.join(SCRATCH, 'bda_core_names.txt')
if os.path.exists(core_file):
    ALL['BDA'] = set(l.strip() for l in open(core_file, encoding='utf-8') if l.strip())

HEADER = """// HullBreach 1.8 - ModuleManager patches ({title})
// Generated for KSP 1.12.5 + BDArmory 1.13. Every patch is guarded with :NEEDS so it is inert when
// the pack is not installed, skips parts that already carry ModuleHullBreach (pack-supplied configs
// win), and runs in :LAST[HullBreach] so parts added or renamed by other patches are covered.
//
// Roles:
//   hull = true         watertight compartment, floods from BDArmory damage below the waterline
//   submarine = true    pressure hull: marks the vessel as a submarine (also detected from the
//                       BDA Surface AI in Submarine mode)
//   crushable = true    superstructure/weapon: loses buoyancy once its ship founders
//   hydroExplosive      magazine/turret that cooks off if it goes under with a foundered ship
//   subdivision = 2     single-part ships: light damage floods a small share of the hull
//   crushDepth          metres; submarines are never crushed above submarineMinCrushDepth (350 m)
"""

def module(fields):
    lines = ["\tMODULE", "\t{", "\t\tname = ModuleHullBreach"]
    for k, v in fields:
        lines.append("\t\t%s = %s" % (k, v))
    lines.append("\t}")
    return "\n".join(lines)

HULL = [('hull', 'true'), ('crushDepth', 200)]
HULL_SINGLE = [('hull', 'true'), ('subdivision', 2), ('crushDepth', 200)]
SUB_HULL = [('hull', 'true'), ('submarine', 'true'), ('crushDepth', 400)]
SUB_SINGLE = [('hull', 'true'), ('submarine', 'true'), ('subdivision', 2), ('crushDepth', 400)]
SUB_TOP = [('crushable', 'true'), ('submarine', 'true'), ('crushDepth', 400)]
CRUSH = [('crushable', 'true'), ('crushDepth', 100)]
CRUSH_200 = [('crushable', 'true'), ('crushDepth', 200)]
GUN_HX = [('crushable', 'true'), ('hydroExplosive', 'true'), ('crushDepth', 100)]

problems = []
total = set()

def check(pack_keys, names):
    pool = set()
    for k in pack_keys:
        pool |= ALL.get(k, set())
    matched = set()
    for n in names:
        pat = n.replace('?', '?')
        hits = [p for p in pool if fnmatch.fnmatchcase(p, pat)]
        if not hits:
            problems.append("%s: no surveyed part matches '%s'" % ('/'.join(pack_keys), n))
        matched.update(hits)
    return matched

def patch(comment, names, needs, fields, pack_keys, has_extra=None):
    matched = check(pack_keys, names) if pack_keys else set(names)
    for m in matched:
        total.add((tuple(pack_keys) if pack_keys else ('?',), m))
    has = "!MODULE[ModuleHullBreach]"
    if has_extra:
        has += "," + has_extra
    out = ["// " + comment]
    out.append("@PART[%s]:HAS[%s]:NEEDS[%s]:LAST[HullBreach]" % ("|".join(names), has, needs))
    out.append("{")
    out.append(module(fields))
    out.append("}")
    return "\n".join(out) + "\n", len(matched)

files = {}

def emit(fname, title, blocks):
    text = HEADER.format(title=title) + "\n" + "\n".join(b for b, _ in blocks)
    files[fname] = (text, sum(c for _, c in blocks))

# ---------------- Large Boat Parts Pack ----------------
LBP_HULL = ["CVHalfHull", "CarrierHangarHull", "CarrierShortStern", "CarrierStorageHangarHull", "DG10KBow", "DG10Kstern",
            "DGfwdHangar", "DGhangarLftHull", "MedBowRevB", "MedHullLCSModule2", "MedLCSmodule1", "MedLCSmodule1B",
            "carrierShortBow", "carriermbow", "carriermhull", "carriermstern", "mediumbow", "mediumhelostern", "mediumhull",
            "mediumhullshort", "mediumhullshortvls", "mediumstern", "mediumsternHangarLong", "mediumsternHangarShort",
            "mediumtumbow", "smallbow", "smallhull", "smallhullshort", "smallstern", "tribow", "trihull", "trihullshort",
            "trimaranShortBow", "tumblebow", "TriLCSHull"]
LBP_SUB = ["subbow", "subhull", "subhullshort", "subhullstern", "SubHullVLSshort", "SubHullMFWvls"]
LBP_SUBTOP = ["subtower", "subplatform"]
LBP_CRUSH = ["DGBridgeBase", "DGHangar", "DGHangar2", "DGSuperBack", "DGSuperFront", "DGSuperMid", "DGdeckHangar",
             "ModernSuperMFVLS", "ModernSuperVLS", "VetteCargoCell", "VetteHullVLSshort", "VetteSuperOpen", "VetteUpperCargo",
             "modernsuperback", "modernsuperflat", "modernsuperfront", "modernsuperfrontcut", "modernsupermiddle",
             "modernsupertall", "shipsupers", "smallsupcut", "smallsupfront", "smallsupmid", "smalltower", "vetteHangar",
             "vetteTower2", "vetteTower3Lit", "vetteTower3lit", "CarrierLiftLong", "CarrierLiftShort"]
WW2_HULL = ["Cruiser5mtrHull", "CruiserHeliHangar", "LizzyBow1", "LizzyBow1A", "LizzyHull1", "LizzyHull1T", "LizzyHull2",
            "LizzyHull4", "LizzyStern2", "Lizzystern1", "lizzyBow2", "battlebow", "battlehull", "battlehulls", "battlestern",
            "cruiserMFWvls", "cruiserbow", "cruiserhull", "cruiserhullshort", "cruiserstern", "destbow", "desthull",
            "desthulls", "deststern"]
WW2_CRUSH = ["AAmount1", "AAmount2", "BBSuperLower", "SuperAftBB", "SuperFrontBB", "aftdeck", "aftlower", "aftlower001",
             "fwddeck", "interdeck", "mainmidlower", "mainmidlower001", "middeck", "midsub1lower", "midsub1lower001",
             "superfrontlower", "superfrontlower001", "LizzyMainDeckAft", "destsupend", "destsupmid", "battleplatend",
             "battleplatmid", "cruiserplatend", "cruiserplatmid", "WW2FunnelCanted", "WW2FunnelStraight"]
REQ_CRUSH = ["BridgeLC2Lower", "BridgeLC2Upper", "DGBridge", "LBPsmBridge2", "LBPsmBridge3", "bridgesmall",
             "SmokeFunne11", "SmokeFunne12", "AirVentShort", "AirVentTall"]
emit("HullBreach_LargeBoatParts.cfg", "Large Boat Parts Pack", [
    patch("Modern hulls", LBP_HULL, "LShipPartsModern", HULL, ["LBP"]),
    patch("Modern submarine pressure hulls", LBP_SUB, "LShipPartsModern", SUB_HULL, ["LBP"]),
    patch("Submarine sail / deck platform", LBP_SUBTOP, "LShipPartsModern", SUB_TOP, ["LBP"]),
    patch("Modern superstructure, hangars, VLS blocks, lifts", LBP_CRUSH, "LShipPartsModern", CRUSH, ["LBP"]),
    patch("WW2 hulls", WW2_HULL, "LShipPartsWW2", HULL, ["LBP"]),
    patch("WW2 superstructure, decks, AA platforms, funnels", WW2_CRUSH, "LShipPartsWW2", CRUSH, ["LBP"]),
    patch("Bridges, funnels, vents (LShipPartsRequired)", REQ_CRUSH, "LShipPartsRequired", CRUSH, ["LBP"]),
])

# ---------------- MNWS (GameData/Johnny) ----------------
MNWS_SHIPS = ["055A", "055X1", "075A", "065", "CGX065", "AMB", "AMBII", "CGNX2050", "CVN003", "CVN004", "CVN16", "CVN21",
              "DDG030", "FDG057", "FDG060", "MSWS111"]
MNWS_SUB = ["SSBN002"]
MNWS_WEAP = ["AAMS", "AAMSL", "AAMSLM", "AAMSR", "AS_ML", "AVLS11", "AVLS22", "AVLS33", "AVLSX1", "AVLSX2", "AVLSX3",
             "DDG2002?RAIL", "FRADAR", "HHQXL", "HHQXL2", "HHQXX", "LLauncher", "YUXX", "MK58L", "MLX", "MLX2", "MMB",
             "MSWSM4X", "MSWSWT", "MSWSWT1", "MSWSWT2", "MSWSWT3", "MSWSWT4", "MissleCell2050", "RGBB", "RML", "SAAM"]
emit("HullBreach_MNWS.cfg", "MNWS - Modern Naval Weapon Systems", [
    patch("Single-part warships and carriers", MNWS_SHIPS, "Johnny", HULL_SINGLE, ["MNWS"]),
    patch("Single-part ballistic missile submarine", MNWS_SUB, "Johnny", SUB_SINGLE, ["MNWS"]),
    patch("Naval turrets, launchers, VLS cells, radars ('DDG2002 RAIL' has a space: matched with ?)", MNWS_WEAP, "Johnny", CRUSH, ["MNWS"]),
])

# ---------------- SACD ----------------
emit("HullBreach_SACD.cfg", "SACD carrier decks", [
    patch("Generic hull", ["CJGenericHull"], "SACDCJ", HULL_SINGLE, ["SACD"]),
    patch("Flight decks", ["CJSDeckA", "CJSDeckB", "CJSDeckC", "CJSDeckD", "CJSDeckE", "CJSDeckF", "CJSDeckH"], "SACDCJ", CRUSH, ["SACD"]),
])

# ---------------- KerbalPowers Naval ----------------
emit("HullBreach_KerbalPowersNaval.cfg", "KerbalPowers Naval", [
    patch("Ballast/buoyancy hulls (ModuleShipBallast changes part.buoyancy; HullBreach follows it)",
          ["smallBuoyancy", "mediumBuoyancy", "largeBuoyancy"], "KerbalPowers/Naval", HULL, ["KPN"]),
])

# ---------------- BDArmory family ----------------
BDAX = ["baha100mmTurret", "baha130mmTurret", "baha37mmCIWS", "baha37mmTurret", "baha57mmTurret", "baha76mmTurret",
        "bahaAPS", "bahaCIMS", "bahaGMLR", "bahaSingleGMLR", "bahaSmallMissileTurret", "baha_Torplauncher", "BDAsonarPod1B"]
BDA_CORE = ["bahaGoalKeeper", "GoalKeeperBDAcMk1", "BDAcGKmk2", "bahaOMillennium", "scanLockRadar1", "scanLargeRadar", "BDAsonarPod1A"]
BDA_ARMOR = ["BD_PanelArmor*", "BD*panelArmor", "BD*slopeArmor"]
BDAR = ["APARradar", "APARradarLarge", "smartsradar", "STIRradar", "Talosradar", "thephalanx", "RAMLauncherTurret",
        "SEARAMLauncherTurret", "MK29Launcher", "CB90MissileLuncher"]
BDAR_LEGACY = ["theotomelara76compact", "MK41VLSQUAD", "MK41VLSINGLE"]
emit("HullBreach_BDArmory.cfg", "BDArmory Plus, BDArmoryExtended, Refurbished Armory", [
    patch("BDArmory: naval CIWS, radars, sonar", BDA_CORE, "BDArmory", CRUSH, ["BDA"]),
    patch("BDArmory: armour plates (as in HullBreach 1.7)", BDA_ARMOR, "BDArmory", CRUSH_200, ["BDA"]),
    patch("BDArmoryExtended: naval guns, CIWS, missile turrets, torpedo launcher, sonar", BDAX, "BDArmoryExtended", CRUSH, ["BDAX"]),
    patch("Refurbished Armory: naval radars, Phalanx, RAM/SeaRAM, Mk29, CB90", BDAR, "BDArmoryRefurbished", CRUSH, ["BDAR", "BDARdev"]),
])
# legacy names are not in the current Refurbished release; appended unchecked
legacy_block = ("// Refurbished Armory: legacy part names from older releases (no-op when absent)\n"
                "@PART[%s]:HAS[!MODULE[ModuleHullBreach]]:NEEDS[BDArmoryRefurbished]:LAST[HullBreach]\n{\n%s\n}\n"
                % ("|".join(BDAR_LEGACY), module(CRUSH)))
t, c = files["HullBreach_BDArmory.cfg"]
files["HullBreach_BDArmory.cfg"] = (t + "\n" + legacy_block, c)

# ---------------- Stock ----------------
STOCK = ["MK1Fuselage", "Mk1FuselageStructural", "mk2Fuselage", "mk2FuselageLongLFO", "mk2FuselageShortLFO",
         "mk2FuselageShortLiquid", "mk2FuselageShortMono", "mk2CargoBayL", "mk2CargoBayS", "mk2CrewCabin",
         "mk2SpacePlaneAdapter", "mk2_1m_AdapterLong", "mk2_1m_Bicoupler", "adapterSize2-Mk2",
         "mk3FuselageLF_25", "mk3FuselageLF_50", "mk3FuselageLF_100", "mk3FuselageLFO_25", "mk3FuselageLFO_50",
         "mk3FuselageLFO_100", "mk3FuselageMONO", "mk3CargoBayS", "mk3CargoBayM", "mk3CargoBayL", "mk3CargoRamp",
         "mk3CrewCabin", "adapterMk3-Mk2", "adapterMk3-Size2", "adapterMk3-Size2Slant", "adapterSize3-Mk3"]
emit("HullBreach_Stock.cfg", "stock boat-like fuselages", [
    patch("Mk1/Mk2/Mk3 fuselages, cargo bays, cabins and adapters (commonly used as boat hulls)", STOCK, "Squad", HULL, ["SQ"]),
])

# ---------------- Legacy packs carried over from 1.7 ----------------
emit("HullBreach_NAS.cfg", "Naval Artillery System (legacy 1.7 coverage)", [
    patch("Main and secondary gun turrets", ["NAS_DD_*", "NAS_CL_*", "NAS_CA_*", "NAS_BB_*", "NAS_BC_*", "NAS_CB_*", "NAS_AA_DD_*", "NAS_AA_SC_*"],
          "NAS", GUN_HX, ["NAS"]),
    patch("Light AA mounts, torpedo tubes, radars, rangefinder", ["NAS_AA_MG_*", "NAS_TP_*", "NAS_RA_*", "NAS_RangeFinder"], "NAS", CRUSH, ["NAS"]),
    patch("Ship magazine", ["NAS_ShipMagazine"], "NAS", GUN_HX, ["NAS"]),
])
emit("HullBreach_CVX.cfg", "Carrier Vessel eXpansion (legacy 1.7 coverage)", [
    patch("Nimitz single-part hull", ["EHInimitz"], "Eskandare_Heavy_Industries/Carrier_Vessel_Expansion", HULL_SINGLE, ["CVX"]),
    patch("Nimitz island and radar", ["EHInimitzRadar", "ehiNimitzBridge"], "Eskandare_Heavy_Industries/Carrier_Vessel_Expansion", CRUSH, ["CVX"]),
])

os.makedirs(OUT, exist_ok=True)
for fname, (text, count) in sorted(files.items()):
    with open(os.path.join(OUT, fname), 'w', encoding='utf-8', newline='\n') as f:
        f.write(text)
    print("%-36s %4d surveyed parts" % (fname, count))
print("total distinct surveyed parts patched: %d" % len(set(n for _, n in total)))
for p in problems:
    print("WARN " + p)
