// MNWSFix -- ModuleInjectTransforms
// Creates empty transforms inside a part's model from cfg data, so that modules which look transforms up
// by name (BDArmory's MultiMissileLauncher: "launchTransformName" with children "missile1".."missileN")
// work on models that were built without them (the MNWS AVLS11/22/33 cells).
//
// Timing: the transforms are created in OnLoad on the part PREFAB while KSP compiles the part (the model is
// already attached at that point, see PartLoader.ParsePart: CompileModel -> CreatePart -> AddModule/Load).
// Every editor/flight instance is a clone of the prefab, so the transforms already exist before any module's
// Awake/OnStart/Start runs, whatever the module order. OnAwake/OnLoad/OnStart on instances repeat the
// (idempotent) creation as a safety net.
//
// Optional "alignMissileTail": reads the missile part name loaded into the launcher (by default
// MultiMissileLauncher.subMunitionName, through KSP's field list, so no BDArmory reference is needed) and moves
// every launch transform along its own +Z so that the missile's tail sits on the cell floor and the missile is
// centred in the cell. The seat is the missile's "bottom" stack node if it faces -Z, otherwise its mesh bounds.

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace MNWSFix
{
    public class ModuleInjectTransforms : PartModule
    {
        /// <summary>Name of an existing model transform to parent the group under. Empty = the part's "model" root.</summary>
        [KSPField] public string parentTransform = "";

        /// <summary>Name of the transform that is created under the parent and holds the children.</summary>
        [KSPField] public string groupName = "mnwsLaunchTransforms";

        /// <summary>Local position of the group in the parent's frame.</summary>
        [KSPField] public Vector3 groupPosition = Vector3.zero;

        /// <summary>Local rotation (Euler, degrees) of the group. -90,0,0 makes the group's +Z point along the parent's +Y.</summary>
        [KSPField] public Vector3 groupRotation = new Vector3(-90f, 0f, 0f);

        /// <summary>Children are named childPrefix + (firstIndex + i), e.g. missile1..missile8.</summary>
        [KSPField] public string childPrefix = "missile";
        [KSPField] public int firstIndex = 1;

        /// <summary>Local rotation (Euler, degrees) of every child relative to the group.</summary>
        [KSPField] public Vector3 childRotation = Vector3.zero;

        /// <summary>Child positions in the PARENT's frame: "x,y,z; x,y,z; ...".</summary>
        [KSPField] public string positions = "";

        /// <summary>Expected number of positions (sanity check only; -1 = don't check).</summary>
        [KSPField] public int count = -1;

        /// <summary>Move the children so the loaded missile's tail rests on the cell floor (the given position).</summary>
        [KSPField] public bool alignMissileTail = false;
        [KSPField] public string missileModule = "MultiMissileLauncher";
        [KSPField] public string missileField = "subMunitionName";

        [KSPField] public bool debug = false;

        Transform group;
        readonly List<Transform> children = new List<Transform>();
        readonly List<Vector3> basePositions = new List<Vector3>(); // group-local
        string alignedFor;
        float nextPoll;

        static readonly Dictionary<string, Vector3?> seatCache = new Dictionary<string, Vector3?>();

        public override void OnAwake()
        {
            base.OnAwake();
            EnsureTransforms();
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            EnsureTransforms();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            EnsureTransforms();
            if (alignMissileTail && (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight))
                UpdateAlignment(true);
        }

        public void Update()
        {
            if (!alignMissileTail || group == null) return;
            if (!(HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)) return;
            if (Time.unscaledTime < nextPoll) return;
            nextPoll = Time.unscaledTime + 0.5f;
            UpdateAlignment(false);
        }

        /// <summary>Creates the group and its children if they don't exist yet. Safe to call any number of times.</summary>
        public void EnsureTransforms()
        {
            if (part == null || string.IsNullOrEmpty(positions) || string.IsNullOrEmpty(groupName)) return;
            Transform modelRoot = part.transform.Find("model");
            if (modelRoot == null) return; // model not attached yet
            Transform parent = string.IsNullOrEmpty(parentTransform) ? modelRoot : FindChildRecursive(modelRoot, parentTransform);
            if (parent == null)
            {
                Debug.LogError($"[MNWSFix.ModuleInjectTransforms]: {part.name}: parent transform '{parentTransform}' not found");
                return;
            }

            List<Vector3> pts = ParsePositions(positions);
            if (count >= 0 && pts.Count != count)
                Debug.LogWarning($"[MNWSFix.ModuleInjectTransforms]: {part.name}: {pts.Count} positions given, count = {count}");

            Quaternion gRot = Quaternion.Euler(groupRotation);
            Quaternion cRot = Quaternion.Euler(childRotation);
            Quaternion gInv = Quaternion.Inverse(gRot);

            group = parent.Find(groupName);
            bool created = false;
            if (group == null)
            {
                var go = new GameObject(groupName);
                group = go.transform;
                group.SetParent(parent, false);
                group.localPosition = groupPosition;
                group.localRotation = gRot;
                group.localScale = Vector3.one;
                go.layer = parent.gameObject.layer;
                created = true;
            }

            children.Clear();
            basePositions.Clear();
            for (int i = 0; i < pts.Count; i++)
            {
                string childName = childPrefix + (firstIndex + i).ToString(CultureInfo.InvariantCulture);
                Vector3 local = gInv * (pts[i] - groupPosition);
                basePositions.Add(local);
                Transform c = group.Find(childName);
                if (c == null)
                {
                    var go = new GameObject(childName);
                    c = go.transform;
                    c.SetParent(group, false);
                    c.localPosition = local;
                    c.localRotation = cRot;
                    c.localScale = Vector3.one;
                    go.layer = group.gameObject.layer;
                }
                children.Add(c);
            }
            if (created && (debug || HighLogic.LoadedScene == GameScenes.LOADING))
                Debug.Log($"[MNWSFix.ModuleInjectTransforms]: {part.name}: created '{groupName}' under '{parent.name}' with {pts.Count} children ({childPrefix}{firstIndex}..{childPrefix}{firstIndex + pts.Count - 1})");
        }

        void UpdateAlignment(bool force)
        {
            string missile = GetLoadedMissileName();
            if (!force && missile == alignedFor) return;
            alignedFor = missile;
            Vector3 seat = Vector3.zero;
            bool haveSeat = !string.IsNullOrEmpty(missile) && TryGetSeat(missile, out seat);
            // Offset expressed in the child's own frame (= the missile's frame): centre laterally, raise by the tail distance.
            Vector3 off = haveSeat ? new Vector3(-seat.x, -seat.y, -seat.z) : Vector3.zero;
            Quaternion cRot = Quaternion.Euler(childRotation);
            // Convert a metre-sized vector in the child's frame to group-local units (handles a scaled parent).
            Vector3 localOff = group.InverseTransformVector(group.TransformDirection(cRot * off));
            for (int i = 0; i < children.Count && i < basePositions.Count; i++)
            {
                if (children[i] == null) continue;
                children[i].localPosition = basePositions[i] + localOff;
            }
            if (debug)
                Debug.Log($"[MNWSFix.ModuleInjectTransforms]: {part.name}: aligned for '{missile}' seat={(haveSeat ? seat.ToString("F3") : "none")} offset={localOff.ToString("F3")}");
        }

        string GetLoadedMissileName()
        {
            if (string.IsNullOrEmpty(missileModule) || string.IsNullOrEmpty(missileField)) return null;
            for (int i = 0; i < part.Modules.Count; i++)
            {
                PartModule m = part.Modules[i];
                if (m == null || m.moduleName != missileModule) continue;
                BaseField f = m.Fields[missileField];
                return f?.GetValue(m) as string;
            }
            return null;
        }

        /// <summary>Seat of a missile in its own part frame: x,y = lateral centre, z = tail position along the nose axis (+Z).</summary>
        static bool TryGetSeat(string partName, out Vector3 seat)
        {
            seat = Vector3.zero;
            if (seatCache.TryGetValue(partName, out Vector3? cached))
            {
                if (cached.HasValue) seat = cached.Value;
                return cached.HasValue;
            }
            AvailablePart ap = PartLoader.getPartInfoByName(partName) ?? PartLoader.getPartInfoByName(partName.Replace('_', '.'));
            Part prefab = ap?.partPrefab;
            Vector3? result = null;
            if (prefab != null)
            {
                // 1. The author's stack seat: a "bottom" node facing -Z (all MNWS missiles have one at the tail).
                if (prefab.attachNodes != null)
                    for (int i = 0; i < prefab.attachNodes.Count; i++)
                    {
                        AttachNode n = prefab.attachNodes[i];
                        if (n != null && n.id == "bottom" && Vector3.Dot(n.orientation.normalized, Vector3.back) > 0.9f)
                        {
                            result = n.position;
                            break;
                        }
                    }
                // 2. Otherwise the mesh bounds in the prefab's part frame.
                if (!result.HasValue)
                {
                    Transform root = prefab.transform;
                    bool any = false;
                    Bounds b = default;
                    void Add(Mesh mesh, Transform t)
                    {
                        if (mesh == null) return;
                        Bounds mb = mesh.bounds;
                        for (int k = 0; k < 8; k++)
                        {
                            Vector3 c = mb.center + Vector3.Scale(mb.extents, new Vector3((k & 1) == 0 ? -1 : 1, (k & 2) == 0 ? -1 : 1, (k & 4) == 0 ? -1 : 1));
                            Vector3 p = root.InverseTransformPoint(t.TransformPoint(c));
                            if (!any) { b = new Bounds(p, Vector3.zero); any = true; }
                            else b.Encapsulate(p);
                        }
                    }
                    foreach (MeshFilter mf in prefab.GetComponentsInChildren<MeshFilter>(true)) Add(mf.sharedMesh, mf.transform);
                    foreach (SkinnedMeshRenderer smr in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true)) Add(smr.sharedMesh, smr.transform);
                    if (any) result = new Vector3(b.center.x, b.center.y, b.min.z);
                }
            }
            seatCache[partName] = result;
            if (result.HasValue) seat = result.Value;
            return result.HasValue;
        }

        static Transform FindChildRecursive(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                Transform r = FindChildRecursive(t.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        internal static List<Vector3> ParsePositions(string s)
        {
            var list = new List<Vector3>();
            if (string.IsNullOrEmpty(s)) return list;
            foreach (string entry in s.Split(';'))
            {
                string e = entry.Trim();
                if (e.Length == 0) continue;
                string[] c = e.Split(',');
                if (c.Length < 3) continue;
                if (float.TryParse(c[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                    float.TryParse(c[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
                    float.TryParse(c[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
                    list.Add(new Vector3(x, y, z));
            }
            return list;
        }

        public override string GetInfo() => string.Empty; // nothing a player needs to see in the part info
    }
}
