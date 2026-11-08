// MNWSFix -- ModuleMNWSBayLink
// Makes BDArmory open the built-in doors of MNWS hulls (and the AVLS rack cells) before it fires a missile
// that sits behind them. Two mechanisms, both configured from cfg:
//
// 1. Cargo bays (flagCargoBayMissiles): BDArmory's MissileFire only opens a ModuleCargoBay for a missile whose
//    MissileBase.inCargoBay is true and whose part.airstreamShields contains that bay. inCargoBay is only set
//    automatically in MissileFire.OnStart, when the stock cargo bay has not computed its shielded parts yet
//    (it does that after a delayed collider reset). This module re-checks every few seconds in flight and sets
//    inCargoBay on every missile shielded by a ModuleCargoBay of THIS part.
//
// 2. Action-group zones (zones / zoneBayGroup): for doors that cover several separate compartments with one
//    animation, a stock ModuleCargoBay (one lookup sphere) cannot be used, and two bays on one animation make
//    BDArmory close the door it just opened. Instead, missiles whose centre lies in one of the spheres get
//    MissileBase.customBayGroup = zoneBayGroup (only if the player left it at 0), and the door's toggle action
//    is bound to that action group, so BDArmory's custom-bay logic toggles the door.
//
// 3. Index check (bayAnimations): ModuleCargoBay.DeployModuleIndex is a plain module index. If another mod's
//    patch removes a module in front of the door, the index points at the wrong module and BDArmory silently
//    stops opening the door. With bayAnimations set, the index of every ModuleCargoBay on the part is checked
//    against the expected door animation and, if wrong, re-pointed at the door module with that animation
//    (BDArmory reads DeployModuleIndex when it fires, so the repair is effective for BDArmory; the stock bay's
//    own door callbacks keep the module it found in its OnStart).
//
// 4. Door buttons (doorLabels): BDArmory only opens a cargo-bay door whose button reads exactly "Open"/"Close",
//    so a hull with two bay doors shows two identical "Open" buttons. For each listed door, the door's own
//    button is hidden and this module shows "Open <label>" / "Close <label>" instead, which toggles the same
//    door. The door module, its event (which BDArmory reads and calls directly) and its action are unchanged.
//
// No BDArmory reference: fields are accessed by name through KSP's BaseFieldList, so the module is inert
// without BDArmory.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace MNWSFix
{
    public class ModuleMNWSBayLink : PartModule
    {
        /// <summary>Seconds between scans in flight.</summary>
        [KSPField] public float interval = 2f;

        /// <summary>Set inCargoBay on missiles shielded by this part's ModuleCargoBay modules.</summary>
        [KSPField] public bool flagCargoBayMissiles = true;

        /// <summary>Spheres in this part's frame, "x,y,z,r; x,y,z,r; ...". Missiles whose centre is inside get customBayGroup.</summary>
        [KSPField] public string zones = "";

        /// <summary>BDArmory customBayGroup index: 1-10 = AG1-AG10, 11 Lights, 12 RCS, 13 SAS, 14 Brakes, 15 Abort, 16 Gear.</summary>
        [KSPField] public int zoneBayGroup = 0;

        /// <summary>Index of the door module (normally a ModuleAnimateGeneric) whose action is bound to zoneBayGroup in flight; -1 = don't bind.</summary>
        [KSPField] public int zoneDoorModuleIndex = -1;

        /// <summary>Animation name the door module must have (safety check against shifted module indices). Empty = no check.</summary>
        [KSPField] public string zoneDoorAnimation = "";

        [KSPField] public string zoneDoorAction = "ToggleAction";

        /// <summary>Expected door animation of each ModuleCargoBay on this part, in module order: "anim1, anim2". Empty = no check.</summary>
        [KSPField] public string bayAnimations = "";

        /// <summary>Labelled buttons for door modules: "animation: label; animation: label". See (4) above. Empty = none.</summary>
        [KSPField] public string doorLabels = "";

        [KSPField] public bool debug = false;

        readonly List<Vector4> zoneList = new List<Vector4>();
        readonly HashSet<PartModule> reported = new HashSet<PartModule>();

        sealed class DoorButton
        {
            public BaseEvent own;     // the door module's "Toggle" event, read by BDArmory
            public BaseEvent button;  // the labelled event shown in its place
            public string label;
            public string shownState; // own.guiName the label was last built from
        }
        readonly List<DoorButton> doorButtons = new List<DoorButton>();

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
            {
                CheckBayIndices();
                SetupDoorButtons();
            }
            if (!HighLogic.LoadedSceneIsFlight) return;
            ParseZones();
            if (zoneList.Count > 0 && zoneBayGroup > 0) BindDoorAction();
            if (flagCargoBayMissiles || (zoneList.Count > 0 && zoneBayGroup > 0))
                StartCoroutine(ScanLoop());
        }

        IEnumerator ScanLoop()
        {
            yield return new WaitForSeconds(1f);
            var wait = new WaitForSeconds(Mathf.Max(0.25f, interval));
            while (true)
            {
                if (vessel != null && vessel.loaded && !vessel.packed) Scan();
                yield return wait;
            }
        }

        void Scan()
        {
            // Forget missiles that were fired or destroyed, so the set does not keep their dead modules (and parts) alive.
            if (reported.Count > 0) reported.RemoveWhere(pm => pm == null);
            List<ModuleCargoBay> bays = flagCargoBayMissiles ? part.FindModulesImplementing<ModuleCargoBay>() : null;
            bool useZones = zoneList.Count > 0 && zoneBayGroup > 0;
            if ((bays == null || bays.Count == 0) && !useZones) return;
            string group = zoneBayGroup.ToString(CultureInfo.InvariantCulture);
            List<Part> parts = vessel.parts;
            for (int i = 0; i < parts.Count; i++)
            {
                Part p = parts[i];
                if (p == null || p == part || !HasMissileModule(p)) continue;

                bool inBay = false;
                if (bays != null && p.airstreamShields != null)
                    for (int b = 0; b < bays.Count && !inBay; b++)
                        inBay = bays[b] != null && p.airstreamShields.Contains(bays[b]);

                bool inZone = useZones && InZone(p);
                if (!inBay && !inZone) continue;

                for (int m = 0; m < p.Modules.Count; m++)
                {
                    PartModule pm = p.Modules[m];
                    if (pm == null) continue;
                    if (inBay)
                    {
                        BaseField f = pm.Fields["inCargoBay"];
                        if (f != null && f.FieldInfo != null && f.FieldInfo.FieldType == typeof(bool) && !(bool)f.GetValue(pm))
                        {
                            f.SetValue(true, pm);
                            Report(pm, $"inCargoBay = true (shielded by a cargo bay of {part.partInfo?.name})");
                        }
                    }
                    if (inZone)
                    {
                        BaseField g = pm.Fields["customBayGroup"];
                        if (g != null && g.FieldInfo != null && g.FieldInfo.FieldType == typeof(string))
                        {
                            string v = g.GetValue(pm) as string;
                            if (string.IsNullOrEmpty(v) || v == "0")
                            {
                                g.SetValue(group, pm);
                                Report(pm, $"customBayGroup = {group} (inside a door zone of {part.partInfo?.name})");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>True if the part has a module with BDArmory's MissileBase fields (checked before any geometry work).</summary>
        static bool HasMissileModule(Part p)
        {
            for (int m = 0; m < p.Modules.Count; m++)
            {
                PartModule pm = p.Modules[m];
                if (pm != null && (pm.Fields["inCargoBay"] != null || pm.Fields["customBayGroup"] != null)) return true;
            }
            return false;
        }

        bool InZone(Part p)
        {
            Bounds[] rb = p.GetRendererBounds();
            Vector3 centre = rb != null && rb.Length > 0
                ? p.partTransform.TransformPoint(PartGeometryUtil.FindBoundsCentroid(rb, p.partTransform))
                : p.partTransform.position;
            Vector3 local = part.partTransform.InverseTransformPoint(centre);
            for (int z = 0; z < zoneList.Count; z++)
            {
                Vector4 s = zoneList[z];
                if ((local - (Vector3)s).sqrMagnitude <= s.w * s.w) return true;
            }
            return false;
        }

        void CheckBayIndices()
        {
            if (string.IsNullOrEmpty(bayAnimations)) return;
            string[] want = bayAnimations.Split(',');
            List<ModuleCargoBay> bays = part.FindModulesImplementing<ModuleCargoBay>();
            for (int i = 0; i < bays.Count && i < want.Length; i++)
            {
                string anim = want[i].Trim();
                if (anim.Length == 0 || bays[i] == null) continue;
                int idx = bays[i].DeployModuleIndex;
                if (AnimationAt(idx) == anim) continue;
                int found = FindAnimationModule(anim);
                if (found >= 0)
                {
                    bays[i].DeployModuleIndex = found;
                    Debug.LogWarning($"[MNWSFix.ModuleMNWSBayLink]: {part.partInfo?.name}: cargo bay {i} pointed at module {idx}, not at the '{anim}' door; re-pointed to module {found} (another patch changed the module order)");
                }
                else
                    Debug.LogError($"[MNWSFix.ModuleMNWSBayLink]: {part.partInfo?.name}: cargo bay {i}: no ModuleAnimateGeneric with animation '{anim}' on this part; BDArmory cannot open this bay");
            }
        }

        void SetupDoorButtons()
        {
            doorButtons.Clear();
            if (string.IsNullOrEmpty(doorLabels)) return;
            foreach (string entry in doorLabels.Split(';'))
            {
                int colon = entry.IndexOf(':');
                if (colon <= 0) continue;
                string anim = entry.Substring(0, colon).Trim();
                string label = entry.Substring(colon + 1).Trim();
                if (anim.Length == 0 || label.Length == 0) continue;
                int index = FindAnimationModule(anim);
                BaseEvent own = index >= 0 ? part.Modules[index].Events["Toggle"] : null;
                if (own == null)
                {
                    Debug.LogWarning($"[MNWSFix.ModuleMNWSBayLink]: {part.partInfo?.name}: no door module with animation '{anim}'; no '{label}' button");
                    continue;
                }
                string id = "MNWSDoor_" + anim;
                BaseEvent button = Events[id];
                if (button == null)
                {
                    // Hidden until SyncDoorButtons moves the door button's visibility over to it.
                    var attr = new KSPEvent
                    {
                        guiName = label,
                        active = own.active,
                        unfocusedRange = own.unfocusedRange,
                        externalToEVAOnly = own.externalToEVAOnly,
                        requireFullControl = own.requireFullControl,
                    };
                    button = new BaseEvent(Events, id, new BaseEventDelegate(own.Invoke), attr);
                    Events.Add(button);
                }
                doorButtons.Add(new DoorButton { own = own, button = button, label = label });
            }
            SyncDoorButtons();
        }

        void Update()
        {
            if (doorButtons.Count > 0) SyncDoorButtons();
        }

        void SyncDoorButtons()
        {
            for (int i = 0; i < doorButtons.Count; i++)
            {
                DoorButton b = doorButtons[i];
                BaseEvent own = b.own, button = b.button;
                // The door module sets its button's visibility in OnStart (and in SetUIWrite); move it to ours.
                if (own.guiActive || own.guiActiveEditor || own.guiActiveUnfocused)
                {
                    button.guiActive = own.guiActive;
                    button.guiActiveEditor = own.guiActiveEditor;
                    button.guiActiveUnfocused = own.guiActiveUnfocused;
                    button.unfocusedRange = own.unfocusedRange;
                    own.guiActive = false;
                    own.guiActiveEditor = false;
                    own.guiActiveUnfocused = false;
                }
                button.active = own.active; // the door hides its button while it moves
                string state = own.guiName;  // "Open" or "Close"
                if (state != b.shownState)
                {
                    b.shownState = state;
                    button.guiName = string.IsNullOrEmpty(state) ? b.label : state + " " + b.label;
                }
            }
        }

        string AnimationAt(int index)
        {
            if (index < 0 || index >= part.Modules.Count) return null;
            return (part.Modules[index] as ModuleAnimateGeneric)?.animationName;
        }

        int FindAnimationModule(string anim)
        {
            for (int m = 0; m < part.Modules.Count; m++)
                if (AnimationAt(m) == anim) return m;
            return -1;
        }

        void BindDoorAction()
        {
            KSPActionGroup g = ToActionGroup(zoneBayGroup);
            if (g == KSPActionGroup.None) return;
            int index = zoneDoorModuleIndex;
            if (!string.IsNullOrEmpty(zoneDoorAnimation) && AnimationAt(index) != zoneDoorAnimation)
            {
                int found = FindAnimationModule(zoneDoorAnimation);
                if (found < 0)
                {
                    Debug.LogWarning($"[MNWSFix.ModuleMNWSBayLink]: {part.partInfo?.name}: no door module with animation '{zoneDoorAnimation}'; action not bound");
                    return;
                }
                index = found;
            }
            if (index < 0 || index >= part.Modules.Count) return;
            PartModule door = part.Modules[index];
            BaseAction a = door.Actions[zoneDoorAction];
            if (a == null) return;
            if ((a.actionGroup & g) == 0)
            {
                a.actionGroup |= g;
                Debug.Log($"[MNWSFix.ModuleMNWSBayLink]: {part.partInfo?.name}: bound '{a.guiName}' to {g} for BDArmory custom bay {zoneBayGroup}");
            }
        }

        static KSPActionGroup ToActionGroup(int bdaIndex)
        {
            switch (bdaIndex)
            {
                case 1: return KSPActionGroup.Custom01;
                case 2: return KSPActionGroup.Custom02;
                case 3: return KSPActionGroup.Custom03;
                case 4: return KSPActionGroup.Custom04;
                case 5: return KSPActionGroup.Custom05;
                case 6: return KSPActionGroup.Custom06;
                case 7: return KSPActionGroup.Custom07;
                case 8: return KSPActionGroup.Custom08;
                case 9: return KSPActionGroup.Custom09;
                case 10: return KSPActionGroup.Custom10;
                case 11: return KSPActionGroup.Light;
                case 12: return KSPActionGroup.RCS;
                case 13: return KSPActionGroup.SAS;
                case 14: return KSPActionGroup.Brakes;
                case 15: return KSPActionGroup.Abort;
                case 16: return KSPActionGroup.Gear;
                default: return KSPActionGroup.None;
            }
        }

        void ParseZones()
        {
            zoneList.Clear();
            if (string.IsNullOrEmpty(zones)) return;
            foreach (string entry in zones.Split(';'))
            {
                string[] c = entry.Trim().Split(',');
                if (c.Length < 4) continue;
                if (float.TryParse(c[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                    float.TryParse(c[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
                    float.TryParse(c[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float z) &&
                    float.TryParse(c[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float r))
                    zoneList.Add(new Vector4(x, y, z, r));
            }
        }

        void Report(PartModule pm, string what)
        {
            if (!debug && reported.Contains(pm)) return;
            reported.Add(pm);
            Debug.Log($"[MNWSFix.ModuleMNWSBayLink]: {pm.part.partInfo?.name} ({pm.moduleName}): {what}");
        }

        public override string GetInfo() => string.Empty;
    }
}
