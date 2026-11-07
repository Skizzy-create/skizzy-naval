// SkizzyRig -- ModuleRigTurret
// Copyright (c) 2026 Skizzy-create. MIT licence (see LICENSE in this folder).
//
// Builds BDArmory turret transforms (yaw, pitch, fire) inside a part model that was made without them, from
// numbers given in the part cfg. Optionally moves existing mesh nodes under the new yaw/pitch transforms so the
// visible mount and barrel turn with them.
//
// Hierarchy created under the part's "model" transform (or under parentTransform):
//
//   baseName   at `pivot`, +Y = upAxis, +Z = boreDirection projected on the plane normal to upAxis.
//              BDArmory's ModuleTurret: baseTransformName. Yaw 0 points along +Z, so yawRange is centred on it.
//     yawName    same place and orientation (identity local rotation): ModuleTurret yawTransformName.
//       pitchName  at `trunnion`, identity local rotation: ModuleTurret pitchTransformName.
//         fireName   one per muzzle, identity local rotation (+Z along the bore): ModuleWeapon fireTransformName.
//
// BDArmory drives yaw as localRotation = Euler(0, yaw, 0) and pitch as localRotation = Euler(-pitch, 0, 0), and
// returns both to identity at rest. A barrel modelled at an elevation (boreDirection not on the yaw plane) would
// therefore point higher than the fire transforms at rest. So the rig's rest pose is "barrel horizontal":
//   * each fire transform is placed where the modelled muzzle ends up after rotating it about the trunnion axis
//     down to the yaw plane; at BDArmory pitch = modelled elevation it sits exactly on the visible muzzle;
//   * every node listed in pitchMeshes is reparented under the pitch transform keeping its world pose, then rotated
//     about the trunnion axis by the same angle, so its bore lies along the pitch transform's +Z.
//   * nodes in yawMeshes are reparented under the yaw transform keeping their world pose.
//
// Models whose base, mount and barrel are one merged mesh have nothing to reparent: leave yawMeshes and
// pitchMeshes empty. The transforms are then invisible -- the gun aims and fires from the right point, but the
// mesh does not move.
//
// Timing: the rig is built in OnLoad on the part PREFAB, while KSP compiles the part (PartLoader.ParsePart:
// CompileModel -> CreatePart -> AddModule/OnLoad), so it is part of every clone before any module's OnStart runs,
// whatever the module order. OnAwake / OnLoad / OnStart on instances repeat the (idempotent) check.
//
// All positions and directions are in the frame of the parent transform (the part frame when the parent is
// "model" and rescaleFactor = 1), in model units.

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SkizzyRig
{
    public class ModuleRigTurret : PartModule
    {
        /// <summary>Existing model transform to build under. Empty = the part's "model" transform.</summary>
        [KSPField] public string parentTransform = "";

        [KSPField] public string baseName = "rigBase";
        [KSPField] public string yawName = "rigYaw";
        [KSPField] public string pitchName = "rigPitch";
        [KSPField] public string fireName = "rigFire";

        /// <summary>Mount's up direction (yaw axis), parent frame.</summary>
        [KSPField] public Vector3 upAxis = Vector3.up;

        /// <summary>A point on the yaw axis (the mount pivot), parent frame.</summary>
        [KSPField] public Vector3 pivot = Vector3.zero;

        /// <summary>A point on the pitch (trunnion) axis, parent frame. The axis is horizontal and normal to the bore.</summary>
        [KSPField] public Vector3 trunnion = Vector3.zero;

        /// <summary>Bore direction as modelled (any length), parent frame.</summary>
        [KSPField] public Vector3 boreDirection = Vector3.forward;

        /// <summary>Muzzle points as modelled, parent frame: "x,y,z; x,y,z; ...". One fire transform each.</summary>
        [KSPField] public string muzzles = "";

        /// <summary>Mesh node names (comma separated) that turn with the yaw transform.</summary>
        [KSPField] public string yawMeshes = "";

        /// <summary>Mesh node names (comma separated) that elevate with the pitch transform.</summary>
        [KSPField] public string pitchMeshes = "";

        [KSPField] public bool debug = false;

        const string Tag = "[SkizzyRig.ModuleRigTurret]";

        public override void OnAwake()
        {
            base.OnAwake();
            EnsureRig(false);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            EnsureRig(true);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            EnsureRig(true);
        }

        /// <summary>Builds the rig if it does not exist yet. Safe to call any number of times.
        /// Returns true when the rig (or the named transforms) exist afterwards.</summary>
        public bool EnsureRig(bool reportConfigErrors)
        {
            if (part == null) return false;
            if (string.IsNullOrEmpty(muzzles))
            {
                // Prefab OnAwake runs before the cfg is loaded; only complain once the fields are final.
                if (reportConfigErrors) Debug.LogError($"{Tag}: {PartName}: no muzzles given, nothing built");
                return false;
            }
            Transform model = part.transform.Find("model");
            if (model == null) return false; // model not attached yet

            if (Find(model, baseName) != null) return true; // already built (prefab, or cloned from it)
            foreach (string n in new[] { yawName, pitchName, fireName })
            {
                if (Find(model, n) != null)
                {
                    Debug.LogWarning($"{Tag}: {PartName}: the model already has a transform named '{n}'; nothing built (remove this module if the model has real turret transforms)");
                    return true;
                }
            }

            Transform parent = string.IsNullOrEmpty(parentTransform) ? model : Find(model, parentTransform);
            if (parent == null)
            {
                Debug.LogError($"{Tag}: {PartName}: parent transform '{parentTransform}' not found");
                return false;
            }

            List<Vector3> muzzleList = ParseVectors(muzzles);
            if (muzzleList.Count == 0)
            {
                Debug.LogError($"{Tag}: {PartName}: could not parse muzzles '{muzzles}'");
                return false;
            }
            Vector3 up = upAxis.normalized;
            Vector3 bore = boreDirection.normalized;
            Vector3 fwd = Vector3.ProjectOnPlane(bore, up);
            if (up.sqrMagnitude < 0.5f || fwd.sqrMagnitude < 1e-6f)
            {
                Debug.LogError($"{Tag}: {PartName}: upAxis {upAxis} / boreDirection {boreDirection} do not define a horizontal forward");
                return false;
            }
            fwd.Normalize();
            float elevation = Mathf.Atan2(Vector3.Dot(bore, up), Vector3.Dot(bore, fwd)) * Mathf.Rad2Deg;

            Transform rigBase = NewChild(baseName, parent);
            rigBase.localPosition = pivot;
            rigBase.localRotation = Quaternion.LookRotation(fwd, up);

            Transform yaw = NewChild(yawName, rigBase);

            Transform pitch = NewChild(pitchName, yaw);
            pitch.position = parent.TransformPoint(trunnion);
            pitch.localRotation = Quaternion.identity;

            // Rotation about the trunnion axis that takes the modelled pose down to BDArmory's rest pose.
            Vector3 axis = pitch.right;
            Vector3 trunnionWorld = pitch.position;
            Quaternion toRest = Quaternion.AngleAxis(elevation, axis);

            for (int i = 0; i < muzzleList.Count; i++)
            {
                Transform fire = NewChild(fireName, pitch);
                Vector3 modelled = parent.TransformPoint(muzzleList[i]);
                fire.position = trunnionWorld + toRest * (modelled - trunnionWorld);
                fire.localRotation = Quaternion.identity;
            }

            int movedYaw = Reparent(model, rigBase, yaw, yawMeshes, null, 0f);
            int movedPitch = Reparent(model, rigBase, pitch, pitchMeshes, axis, elevation);

            string meshes = movedYaw + movedPitch == 0
                ? "no mesh nodes reparented: invisible rig, the model's mesh stays static"
                : $"{movedYaw} mesh node(s) under yaw, {movedPitch} under pitch";
            if (debug || HighLogic.LoadedScene == GameScenes.LOADING)
                Debug.Log($"{Tag}: {PartName}: built {baseName}/{yawName}/{pitchName} + {muzzleList.Count} x {fireName}; " +
                          $"pivot {Fmt(pivot)}, trunnion {Fmt(trunnion)}, modelled bore elevation {elevation.ToString("F2", CultureInfo.InvariantCulture)} deg, " +
                          $"rest fire point(s) {RestPoints(parent, pitch)}; {meshes}");
            return true;
        }

        int Reparent(Transform model, Transform rigBase, Transform target, string names, Vector3? axis, float angle)
        {
            int moved = 0;
            foreach (string raw in names.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string n = raw.Trim();
                if (n.Length == 0) continue;
                Transform t = Find(model, n);
                if (t == null)
                {
                    Debug.LogError($"{Tag}: {PartName}: mesh node '{n}' not found");
                    continue;
                }
                if (t == model || rigBase.IsChildOf(t))
                {
                    Debug.LogError($"{Tag}: {PartName}: '{n}' contains the rig itself and cannot be moved");
                    continue;
                }
                t.SetParent(target, true);
                if (axis.HasValue && angle != 0f) t.RotateAround(target.position, axis.Value, angle);
                moved++;
            }
            return moved;
        }

        static Transform NewChild(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.layer = parent.gameObject.layer;
            Transform t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
            return t;
        }

        string RestPoints(Transform parent, Transform pitch)
        {
            var parts = new List<string>();
            for (int i = 0; i < pitch.childCount; i++)
            {
                Transform c = pitch.GetChild(i);
                if (c.name == fireName) parts.Add(Fmt(parent.InverseTransformPoint(c.position)));
            }
            return string.Join(" ", parts.ToArray());
        }

        string PartName => part.partInfo != null ? part.partInfo.name : part.name;

        static string Fmt(Vector3 v) =>
            "(" + v.x.ToString("F4", CultureInfo.InvariantCulture) + ", " + v.y.ToString("F4", CultureInfo.InvariantCulture) +
            ", " + v.z.ToString("F4", CultureInfo.InvariantCulture) + ")";

        static Transform Find(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                Transform r = Find(t.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        internal static List<Vector3> ParseVectors(string s)
        {
            var list = new List<Vector3>();
            if (string.IsNullOrEmpty(s)) return list;
            foreach (string entry in s.Split(';'))
            {
                string[] c = entry.Trim().Split(',');
                if (c.Length < 3) continue;
                if (float.TryParse(c[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                    float.TryParse(c[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
                    float.TryParse(c[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
                    list.Add(new Vector3(x, y, z));
            }
            return list;
        }

        public override string GetInfo() => string.Empty; // nothing for the player
    }
}
