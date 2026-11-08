using System;
using UnityEngine;

public class Catapult : PartModule
{
	[KSPField]
	public float maxlength = 90f;

	public float TargetDA = 0f;

	public float BlockA = 0f;

	private Transform CatapultTransform = null;

	private Transform WheelTransform = null;

	private Transform RailTransform = null;

	public bool IsReseting = false;

	public bool IsGrabing = false;

	public bool IsReady = false;

	public bool IsLaunching = false;

	public bool IsBreak = false;

	public Part Wheel = null;

	public Rigidbody WheelBody = null;

	public ModuleWheelBase WheelModule = null;

	public MeshRenderer CatapultColor = null;

	public float time = 0f;

	private PidController xPid = new PidController(50f, 0.005f, 0.005f, 50, 50f);

	private PidController yPid = new PidController(50f, 0.005f, 0.005f, 50, 50f);

	private PidController zPid = new PidController(50f, 0.005f, 0.005f, 50, 50f);

	[KSPField(isPersistant = true)]
	public bool IsActivated = true;

	[KSPField(guiActive = true, guiActiveEditor = true, guiName = "Target Velocity", isPersistant = true)]
	[UI_FloatRange(minValue = 30f, maxValue = 100f, stepIncrement = 10f)]
	public float TargetV = 60f;

	// SkizzyNaval: the action group of the AIRCRAFT that hooks and arms (group on) and fires (group off) the catapult.
	// The original read the aircraft's Brakes, which BDArmory's pilot AI sets and releases by itself, so an AI
	// aircraft that rolled onto the shuttle was armed and launched. Custom07 is a group BDArmory never toggles on
	// its own (it uses AG10 at competition start and in standby wake-up, and AG8/AG9 in tag mode).
	// "Brakes" restores the original behaviour.
	[KSPField(guiActive = true, guiActiveEditor = true, guiName = "Arm/launch with aircraft", isPersistant = true)]
	[UI_ChooseOption(options = new string[] { "Custom01", "Custom02", "Custom03", "Custom04", "Custom05", "Custom06", "Custom07", "Custom08", "Custom09", "Custom10", "Brakes" }, display = new string[] { "AG1", "AG2", "AG3", "AG4", "AG5", "AG6", "AG7", "AG8", "AG9", "AG10", "Brakes" })]
	public string launchGroup = "Custom07";

	private string parsedLaunchGroupName = null;

	private KSPActionGroup parsedLaunchGroup = KSPActionGroup.Custom07;

	private KSPActionGroup LaunchGroup
	{
		get
		{
			if (launchGroup != parsedLaunchGroupName)
			{
				parsedLaunchGroupName = launchGroup;
				parsedLaunchGroup = (!string.IsNullOrEmpty(launchGroup) && Enum.IsDefined(typeof(KSPActionGroup), launchGroup)) ? (KSPActionGroup)Enum.Parse(typeof(KSPActionGroup), launchGroup) : KSPActionGroup.Custom07;
			}
			return parsedLaunchGroup;
		}
	}

	private static string GroupLabel(KSPActionGroup g)
	{
		switch (g)
		{
		case KSPActionGroup.Custom01: return "AG1 (key 1)";
		case KSPActionGroup.Custom02: return "AG2 (key 2)";
		case KSPActionGroup.Custom03: return "AG3 (key 3)";
		case KSPActionGroup.Custom04: return "AG4 (key 4)";
		case KSPActionGroup.Custom05: return "AG5 (key 5)";
		case KSPActionGroup.Custom06: return "AG6 (key 6)";
		case KSPActionGroup.Custom07: return "AG7 (key 7)";
		case KSPActionGroup.Custom08: return "AG8 (key 8)";
		case KSPActionGroup.Custom09: return "AG9 (key 9)";
		case KSPActionGroup.Custom10: return "AG10 (key 0)";
		case KSPActionGroup.Brakes: return "Brakes (hold B)";
		default: return g.ToString();
		}
	}

	public override string GetInfo()
	{
		if (LaunchGroup == KSPActionGroup.Brakes)
		{
			return "Hook: roll an aircraft's wheel onto the shuttle.\nArm: hold the aircraft's brakes until the light turns yellow.\nLaunch: release the brakes. Braking during the stroke aborts.\nThe launch group can be changed in the part menu.";
		}
		return "Hook and arm: stop an aircraft's wheel on the shuttle and switch the aircraft's " + GroupLabel(LaunchGroup) + " on (light turns yellow).\nLaunch: switch it off again. Switching it on during the stroke aborts.\nAircraft with the group off are not hooked.\nThe launch group can be changed in the part menu.";
	}

	[KSPAction("Toggle Catapult", KSPActionGroup.None)]
	private void ActionActivate(KSPActionParam param)
	{
		if (!IsActivated)
		{
			Activate();
		}
		else
		{
			Deactivate();
		}
	}

	[KSPEvent(name = "Activate", guiName = "Activate", active = true, guiActive = false)]
	public void Activate()
	{
		IsActivated = true;
		Events["Deactivate"].guiActive = true;
		Events["Activate"].guiActive = false;
	}

	[KSPEvent(name = "Deactivate", guiName = "Deactivate", active = true, guiActive = true)]
	public void Deactivate()
	{
		IsActivated = false;
		Events["Deactivate"].guiActive = false;
		Events["Activate"].guiActive = true;
		IsReseting = false;
		IsGrabing = false;
		IsReady = false;
		IsLaunching = false;
		IsBreak = false;
	}

	public override void OnStart(StartState state)
	{
		enabled = true;
		part.force_activate();
		CatapultTransform = part.FindModelTransform("Catapult");
		RailTransform = part.FindModelTransform("Rail");
	}

	public override void OnFixedUpdate()
	{
		if (HighLogic.LoadedSceneIsFlight)
		{
			RailTransform.localScale = Vector3.zero;
		}
		Color value = Color.white;
		if (IsActivated)
		{
			// SkizzyNaval: the hooked aircraft can be destroyed or recovered mid-launch; the original then threw a
			// NullReferenceException every physics frame. Drop the hook and reset the shuttle instead.
			if ((IsGrabing || IsReady || IsLaunching) && (Wheel == null || Wheel.vessel == null || WheelBody == null || !WheelTransform))
			{
				IsGrabing = false;
				IsReady = false;
				IsLaunching = false;
				IsBreak = false;
				IsReseting = true;
				Wheel = null;
				WheelBody = null;
				WheelModule = null;
				WheelTransform = null;
			}
			if ((bool)WheelTransform && Wheel != null && Wheel.vessel != null)
			{
				if (Wheel.vessel.ActionGroups[LaunchGroup])
				{
					IsBreak = true;
				}
				else
				{
					IsBreak = false;
				}
			}
			if (!CatapultColor)
			{
				CatapultColor = CatapultTransform.gameObject.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
			}
			if (IsGrabing)
			{
				float totalMass = Wheel.vessel.GetTotalMass();
				Vector3 vector = CatapultTransform.InverseTransformDirection(CatapultTransform.position - WheelTransform.position);
				Vector3 vector2 = CatapultTransform.InverseTransformDirection(part.rb.velocity - WheelBody.velocity);
				if (IsLaunching)
				{
					vector2 = Vector3.zero;
				}
				Vector3 vector3 = vector2 + vector * 5f;
				Vector3 vector4 = CatapultTransform.TransformDirection(vector3.x, -1f, vector3.z);
				value = Color.blue;
				if (!IsLaunching)
				{
					foreach (Part part in Wheel.vessel.parts)
					{
						if (part.physicalSignificance == Part.PhysicalSignificance.FULL && part.rb != null)
						{
							part.AddForce(vector4 * 10f * part.rb.mass);
						}
					}
				}
				else
				{
					Wheel.AddForceAtPosition(vector4 * totalMass * 10f, WheelTransform.position);
				}
				if (IsBreak && !IsReady && !IsLaunching)
				{
					IsReady = true;
				}
			}
			if (IsReady)
			{
				value = Color.yellow;
				if (!IsBreak && !IsLaunching)
				{
					IsLaunching = true;
					IsGrabing = false;
				}
			}
			if (IsLaunching)
			{
				float num = Mathf.Pow(TargetV, 2f) / (2f * maxlength);
				float totalMass2 = Wheel.vessel.GetTotalMass();
				time += Time.fixedDeltaTime;
				value = Color.green;
				CatapultTransform.localPosition += Vector3.right * num * time * Time.fixedDeltaTime;
				Vector3 vector5 = CatapultTransform.InverseTransformDirection(CatapultTransform.position - WheelTransform.position - CatapultTransform.right * num * time * Time.fixedDeltaTime);
				Vector3 vector6 = CatapultTransform.InverseTransformDirection(CatapultTransform.right * num * time + base.part.rb.velocity - WheelBody.velocity);
				Vector3 direction = vector6 + vector5 * 10f;
				Vector3 vector7 = CatapultTransform.TransformDirection(direction) * 10f;
				Vector3 vector8 = CatapultTransform.right * num;
				Wheel.AddForceAtPosition((vector8 + vector7) * totalMass2, WheelTransform.position);
				if (CatapultTransform.localPosition.x > maxlength || IsBreak)
				{
					IsReseting = true;
					IsGrabing = false;
					IsReady = false;
					IsLaunching = false;
					WheelModule = null;
					WheelTransform = null;
					value = Color.white;
				}
			}
			if (IsReseting)
			{
				CatapultTransform.localPosition = Vector3.zero;
				IsReseting = false;
				time = 0f;
			}
		}
		else
		{
			value = Color.grey;
		}
		if ((bool)CatapultColor)
		{
			CatapultColor.material.SetColor("_Color", value);
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (IsReseting || IsReady || IsGrabing || IsLaunching || !IsActivated)
		{
			return;
		}
		Part componentInParent = other.gameObject.GetComponentInParent<Part>();
		if (!componentInParent || !componentInParent.vessel || !(componentInParent.vessel != vessel) || !WantsHook(componentInParent.vessel) || !IsWheel(componentInParent))
		{
			return;
		}
		Wheel = componentInParent;
		WheelBody = other.attachedRigidbody;
		if (!WheelTransform)
		{
			WheelTransform = other.transform;
			Debug.Log("Wheel");
			if (!Wheel.rb)
			{
				Debug.Log("Null");
			}
		}
		IsGrabing = true;
	}

	// SkizzyNaval: with a custom launch group, only an aircraft that has switched that group on is hooked. Otherwise an
	// AI aircraft taxiing over an active catapult would be grabbed and pinned to the shuttle, since the AI never arms
	// it. With "Brakes" the original behaviour is kept (any grounded wheel is hooked; brakes arm).
	private bool WantsHook(Vessel v)
	{
		KSPActionGroup g = LaunchGroup;
		return g == KSPActionGroup.Brakes || v.ActionGroups[g];
	}

	private bool IsWheel(Part IRpart)
	{
		foreach (PartModule module in IRpart.Modules)
		{
			if (module.moduleName == "ModuleWheelBase")
			{
				WheelModule = (ModuleWheelBase)module;
				WheelTransform = IRpart.FindModelTransform(WheelModule.wheelTransformName);
				if (WheelModule.isGrounded)
				{
					return true;
				}
			}
			else if (module.moduleName == "KSPWheelBase")
			{
				return true;
			}
		}
		return false;
	}
}
