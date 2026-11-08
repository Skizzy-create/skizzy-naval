using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KSP.UI.Screens;
using UnityEngine;

public class ReloadableRail : PartModule
{
	public delegate void OnPartReady(Part affectedPart);

	private Transform MissileTransform = null;

	[KSPField(guiName = "Missile Assign", guiActive = true, isPersistant = true)]
	private string MissileName = "bahaAim120";

	[KSPAction("Reassign", KSPActionGroup.None)]
	private void ActionResupply(KSPActionParam param)
	{
		Reassign();
	}

	[KSPEvent(name = "Resupply", guiName = "Resupply", active = true, guiActive = false)]
	public void Resupply()
	{
		if (part.children.Count != 0)
		{
			Debug.Log("Not Empty" + part.children.Count);
			return;
		}
		List<AvailablePart> loadedPartsList = PartLoader.LoadedPartsList;
		foreach (AvailablePart item in loadedPartsList)
		{
			if (!(item.partPrefab.name == MissileName))
			{
				continue;
			}
			Debug.Log("Got Missile" + item.partPrefab.name);
			foreach (PartModule module in item.partPrefab.Modules)
			{
				if (module.moduleName == "MissileLauncher")
				{
					ConfigNode configNode = new ConfigNode();
					PartSnapshot(item.partPrefab).CopyTo(configNode);
					Debug.Log("Node" + item.partPrefab.srfAttachNode.originalPosition);
					CreatePart(configNode, MissileTransform.transform.position - MissileTransform.TransformDirection(item.partPrefab.srfAttachNode.originalPosition), part.transform.rotation, part, part, "srfAttach");
				}
			}
		}
	}

	[KSPEvent(name = "Reassign", guiName = "Reassign", active = true, guiActive = true)]
	public void Reassign()
	{
		if (part.children.Count != 1)
		{
			return;
		}
		foreach (Part child in part.children)
		{
			foreach (PartModule module in child.Modules)
			{
				if (module.moduleName == "MissileLauncher")
				{
					MissileName = child.name;
					Debug.Log(MissileName);
				}
			}
		}
	}

	public override void OnStart(StartState state)
	{
		enabled = true;
		part.force_activate();
		MissileTransform = part.FindModelTransform("MissileTransform");
		Reassign();
	}

	public override void OnFixedUpdate()
	{
	}

	[KSPEvent(name = "Resupply", guiName = "Resupply", active = true, guiActive = false)]
	public static AttachNode GetAttachNodeById(Part p, string id)
	{
		AttachNode attachNode = ((id == "srfAttach") ? p.srfAttachNode : p.FindAttachNode(id));
		if (attachNode == null)
		{
			Debug.Log("Cannot find attach node {0} on part {1}. Using srfAttach" + id + p);
			attachNode = p.srfAttachNode;
		}
		return attachNode;
	}

	public static ModuleDockingNode GetDockingNode(Part part, string attachNodeId = null, AttachNode attachNode = null)
	{
		string nodeId = attachNodeId ?? attachNode?.id;
		return part.FindModulesImplementing<ModuleDockingNode>().FirstOrDefault((ModuleDockingNode x) => x.referenceAttachNode == nodeId);
	}

	public static bool CoupleDockingPortWithPart(ModuleDockingNode dockingNode)
	{
		Part attachedPart = dockingNode.referenceNode.attachedPart;
		if (attachedPart == null)
		{
			Debug.Log("Node's part {0} is not attached to anything thru the reference node" + dockingNode.part);
			return false;
		}
		if (dockingNode.state != dockingNode.st_ready.name)
		{
			Debug.Log(string.Concat("Hard reset docking node {0} from state '{1}' to '{2}'", dockingNode.part, dockingNode.state, dockingNode.st_ready.name));
			dockingNode.dockedPartUId = 0u;
			dockingNode.dockingNodeModuleIndex = 0;
			dockingNode.fsm.StartFSM(dockingNode.st_ready.name);
		}
		IEnumerator enumerator = dockingNode.lateFSMStart(StartState.None);
		while (enumerator.MoveNext())
		{
		}
		if (dockingNode.fsm.currentStateName != dockingNode.st_preattached.name)
		{
			Debug.Log(string.Concat("Node on {0} is unexpected state '{1}'", dockingNode.part, dockingNode.fsm.currentStateName));
			return false;
		}
		Debug.Log(string.Concat("Successfully set docking node {0} to state {1} with part {2}", dockingNode.part, dockingNode.fsm.currentStateName, attachedPart));
		return true;
	}

	private static IEnumerator WaitAndMakeLonePart(Part newPart, OnPartReady onPartReady)
	{
		Debug.Log("Create lone part vessel for {0}" + newPart);
		string originatingVesselName = newPart.vessel.vesselName;
		newPart.physicalSignificance = Part.PhysicalSignificance.NONE;
		newPart.PromoteToPhysicalPart();
		newPart.Unpack();
		newPart.disconnect(controlledSeparation: true);
		Vessel newVessel = newPart.gameObject.AddComponent<Vessel>();
		newVessel.id = Guid.NewGuid();
		if (newVessel.Initialize())
		{
			newVessel.vesselName = Vessel.AutoRename(newVessel, originatingVesselName);
			newVessel.IgnoreGForces(10);
			newVessel.currentStage = StageManager.RecalculateVesselStaging(newVessel);
			newPart.setParent();
		}
		yield return new WaitWhile(() => !newPart.started && newPart.State != PartStates.DEAD);
		Debug.Log(string.Concat("Part {0} is in state {1}", newPart, newPart.State));
		if (newPart.State == PartStates.DEAD)
		{
			Debug.Log("Part {0} has died before fully instantiating" + newPart);
		}
		else
		{
			onPartReady?.Invoke(newPart);
		}
	}

	public static void AwakePartModule(PartModule module)
	{
		MethodInfo method = typeof(PartModule).GetMethod("Awake", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (method != null)
		{
			method.Invoke(module, new object[0]);
		}
		else
		{
			Debug.Log("Cannot find Awake() method on {0}. Skip awakening", module);
		}
	}

	public static void CleanupFieldsInModule(PartModule module)
	{
		ModuleScienceLab moduleScienceLab = module as ModuleScienceLab;
		if (moduleScienceLab != null)
		{
			moduleScienceLab.ExperimentData = new List<string>();
			Debug.Log("WORKAROUND. Fix null field in ModuleScienceLab module on the part prefab: {0}", module);
		}
		try
		{
			module.Fields.GetEnumerator();
		}
		catch
		{
			Debug.Log("WORKAROUND. Module {0} on part prefab is not awaken. Call Awake on it", module);
			AwakePartModule(module);
		}
		foreach (BaseField field in module.Fields)
		{
			BaseField baseField = field;
			if (baseField.isPersistant && baseField.GetValue(module) != null)
			{
			}
		}
	}

	public static void CleanupModuleFieldsInPart(Part part)
	{
		List<PartModule> list = new List<PartModule>();
		foreach (PartModule module in part.Modules)
		{
			PartModule partModule = module;
			try
			{
				CleanupFieldsInModule(partModule);
			}
			catch
			{
				list.Add(partModule);
			}
		}
		foreach (PartModule item in list)
		{
			Debug.Log("Module on part prefab {0} is setup improperly: name={1}. Drop it!" + part, item);
			part.RemoveModule(item);
		}
	}

	public static ConfigNode PartSnapshot(Part part)
	{
		if ((object)part == part.partInfo.partPrefab)
		{
			CleanupModuleFieldsInPart(part);
		}
		Vessel vessel = part.vessel;
		ProtoPartSnapshot protoPartSnapshot = part.protoPartSnapshot;
		List<ProtoCrewMember> protoModuleCrew = part.protoModuleCrew;
		if (vessel == null)
		{
			part.vessel = part.gameObject.AddComponent<Vessel>();
			Debug.Log(string.Concat("Making a fake vessel for the part to make a snapshot: part=", part, ", vessel=", part.vessel));
		}
		ProtoPartSnapshot protoPartSnapshot2 = new ProtoPartSnapshot(part, null);
		protoPartSnapshot2.attachNodes = new List<AttachNodeSnapshot>();
		protoPartSnapshot2.srfAttachNode = new AttachNodeSnapshot("attach,-1");
		protoPartSnapshot2.symLinks = new List<ProtoPartSnapshot>();
		protoPartSnapshot2.symLinkIdxs = new List<int>();
		ConfigNode configNode = new ConfigNode("PART");
		protoPartSnapshot2.Save(configNode);
		if (vessel != part.vessel)
		{
			Debug.Log("Destroying the fake vessel");
			UnityEngine.Object.DestroyImmediate(part.vessel);
		}
		part.vessel = vessel;
		part.protoPartSnapshot = protoPartSnapshot;
		part.protoModuleCrew = protoModuleCrew;
		configNode.RemoveValues("parent");
		configNode.RemoveValues("position");
		configNode.RemoveValues("rotation");
		configNode.RemoveValues("istg");
		configNode.RemoveValues("dstg");
		configNode.RemoveValues("sqor");
		configNode.RemoveValues("sidx");
		configNode.RemoveValues("attm");
		configNode.RemoveValues("srfN");
		configNode.RemoveValues("attN");
		configNode.RemoveValues("connected");
		configNode.RemoveValues("attached");
		configNode.RemoveValues("flag");
		configNode.RemoveNodes("ACTIONS");
		configNode.RemoveNodes("EVENTS");
		ConfigNode[] nodes = configNode.GetNodes("MODULE");
		foreach (ConfigNode configNode2 in nodes)
		{
			configNode2.RemoveNodes("ACTIONS");
			configNode2.RemoveNodes("EVENTS");
		}
		return configNode;
	}

	public static Part CreatePart(AvailablePart avPart, Vector3 position, Quaternion rotation, Part fromPart)
	{
		ConfigNode configNode = new ConfigNode();
		PartSnapshot(avPart.partPrefab).CopyTo(configNode);
		return CreatePart(configNode, position, rotation, fromPart);
	}

	public static Part CreatePart(ConfigNode partConfig, Vector3 position, Quaternion rotation, Part fromPart, Part coupleToPart = null, string srcAttachNodeId = null, AttachNode tgtAttachNode = null, OnPartReady onPartReady = null, bool createPhysicsless = false)
	{
		if (coupleToPart != null && (srcAttachNodeId == null || (srcAttachNodeId == "srfAttach" && tgtAttachNode != null) || (srcAttachNodeId != "srfAttach" && (tgtAttachNode == null || tgtAttachNode.id == "srfAttach"))))
		{
			srcAttachNodeId = "srfAttach";
			tgtAttachNode = null;
		}
		Vessel vessel = ((coupleToPart != null) ? coupleToPart.vessel : fromPart.vessel);
		ConfigNode node = new ConfigNode();
		partConfig.CopyTo(node);
		ProtoPartSnapshot protoPartSnapshot = new ProtoPartSnapshot(node, vessel.protoVessel, HighLogic.CurrentGame);
		if (HighLogic.CurrentGame.flightState.ContainsFlightID(protoPartSnapshot.flightID) || protoPartSnapshot.flightID == 0)
		{
			protoPartSnapshot.flightID = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);
		}
		protoPartSnapshot.parentIdx = ((coupleToPart != null) ? vessel.parts.IndexOf(coupleToPart) : 0);
		protoPartSnapshot.position = position;
		protoPartSnapshot.rotation = rotation;
		protoPartSnapshot.stageIndex = 0;
		protoPartSnapshot.defaultInverseStage = 0;
		protoPartSnapshot.seqOverride = -1;
		protoPartSnapshot.inStageIndex = -1;
		protoPartSnapshot.attachMode = ((srcAttachNodeId == "srfAttach") ? 1 : 0);
		protoPartSnapshot.attached = true;
		protoPartSnapshot.flagURL = fromPart.flagURL;
		Part part = protoPartSnapshot.Load(vessel, loadAsRootPart: false);
		vessel.Parts.Add(part);
		part.transform.position = position;
		part.transform.rotation = rotation;
		part.missionID = fromPart.missionID;
		part.UpdateOrgPosAndRot(part.vessel.rootPart);
		if (coupleToPart != null)
		{
			Debug.Log(string.Concat("Ready to error", part, srcAttachNodeId, tgtAttachNode));
			part.StartCoroutine(WaitAndCouple(part, srcAttachNodeId, tgtAttachNode, onPartReady, createPhysicsless));
		}
		else
		{
			part.StartCoroutine(WaitAndMakeLonePart(part, onPartReady));
		}
		return part;
	}

	private static IEnumerator WaitAndCouple(Part newPart, string srcAttachNodeId, AttachNode tgtAttachNode, OnPartReady onPartReady, bool createPhysicsless = false)
	{
		Part tgtPart = newPart.parent;
		if (createPhysicsless)
		{
			newPart.PhysicsSignificance = 1;
		}
		Debug.Log(string.Concat("Attach new part ", newPart, " to ", newPart.vessel, ": srcNodeId =", srcAttachNodeId, ", tgtNode=", tgtAttachNode));
		AttachNode srcAttachNode = GetAttachNodeById(newPart, srcAttachNodeId);
		srcAttachNode.attachedPart = tgtPart;
		srcAttachNode.attachedPartId = tgtPart.flightID;
		if (tgtAttachNode != null)
		{
			tgtAttachNode.attachedPart = newPart;
			tgtAttachNode.attachedPartId = newPart.flightID;
		}
		ModuleDockingNode srcDockingNode = GetDockingNode(newPart, srcAttachNodeId);
		if (srcDockingNode != null)
		{
			srcDockingNode.state = "PreAttached";
			srcDockingNode.dockedPartUId = 0u;
			srcDockingNode.dockingNodeModuleIndex = 0;
			Debug.Log(string.Concat("Force new node ", newPart, " to state ", srcDockingNode.state));
		}
		ModuleDockingNode tgtDockingNode = GetDockingNode(tgtPart, null, tgtAttachNode);
		if (tgtDockingNode != null)
		{
			CoupleDockingPortWithPart(tgtDockingNode);
		}
		Debug.Log(string.Concat("Wait for part", newPart, " to get alive..."));
		newPart.transform.parent = tgtPart.transform;
		Vector3 relPos = newPart.transform.localPosition;
		Quaternion relRot = newPart.transform.localRotation;
		if (newPart.PhysicsSignificance != 1)
		{
			CollisionManager.IgnoreCollidersOnVessel(ignoreColliders: newPart.GetComponentsInChildren<Collider>(includeInactive: false), vessel: tgtPart.vessel);
		}
		while (!newPart.started && newPart.State != PartStates.DEAD)
		{
			yield return new WaitForFixedUpdate();
			if (newPart.rb != null)
			{
				newPart.rb.position = newPart.parent.transform.TransformPoint(relPos);
				newPart.rb.rotation = newPart.parent.transform.rotation * relRot;
				newPart.rb.velocity = newPart.parent.Rigidbody.velocity;
				newPart.rb.angularVelocity = newPart.parent.Rigidbody.angularVelocity;
			}
		}
		newPart.transform.parent = newPart.transform;
		Debug.Log(string.Concat("Part ", newPart, " is in state ", newPart.State));
		if (newPart.State == PartStates.DEAD)
		{
			Debug.Log(string.Concat("Part", newPart, "has died before fully instantiating"));
			yield break;
		}
		newPart.Unpack();
		newPart.InitializeModules();
		GameEvents.onPartCouple.Fire(new GameEvents.FromToAction<Part, Part>(newPart, tgtPart));
		GameEvents.onVesselPartCountChanged.Fire(tgtPart.vessel);
		newPart.vessel.checkLanded();
		newPart.vessel.currentStage = StageManager.RecalculateVesselStaging(tgtPart.vessel) + 1;
		GameEvents.onVesselWasModified.Fire(tgtPart.vessel);
		newPart.CheckBodyLiftAttachment();
		onPartReady?.Invoke(newPart);
	}
}
