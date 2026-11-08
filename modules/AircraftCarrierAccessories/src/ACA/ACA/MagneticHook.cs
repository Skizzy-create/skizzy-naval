using UnityEngine;

namespace ACA;

public class MagneticHook : PartModule
{
	[KSPField(isPersistant = true)]
	private bool IsActivated = false;

	private Transform BerthTransform;

	[KSPField(guiActive = true, guiActiveEditor = true, guiName = "Gravity ", isPersistant = true)]
	[UI_FloatRange(minValue = 0f, maxValue = 20f, stepIncrement = 1f)]
	public float Gravity = 5f;

	[KSPAction("Toggle MGHook", KSPActionGroup.None)]
	private void ActionActivate(KSPActionParam param)
	{
		if (!IsActivated)
		{
			MGHookActivated();
		}
		else
		{
			MGHookDeavtivated();
		}
	}

	[KSPEvent(name = "MGHookActivated", guiName = "Activate MGHook", active = true, guiActive = true)]
	public void MGHookActivated()
	{
		IsActivated = true;
		Events["MGHookDeavtivated"].guiActive = true;
		Events["MGHookActivated"].guiActive = false;
		Debug.Log("MGHookActivated");
	}

	[KSPEvent(name = "MGHookDeavtivated", guiName = "Deactivate MGHook", active = true, guiActive = false)]
	public void MGHookDeavtivated()
	{
		IsActivated = false;
		Events["MGHookDeavtivated"].guiActive = false;
		Events["MGHookActivated"].guiActive = true;
	}

	public override void OnStart(StartState state)
	{
		enabled = true;
		part.force_activate();
	}

	public override void OnFixedUpdate()
	{
	}

	private void OnTriggerStay(Collider other)
	{
		if (!IsActivated || !other.gameObject.GetComponentInParent<Part>() || other.gameObject.GetComponentInParent<Part>().vessel.isEVA)
		{
			return;
		}
		Part componentInParent = other.gameObject.GetComponentInParent<Part>();
		if (!componentInParent || !componentInParent.vessel || !(componentInParent.vessel != vessel) || !IsBerth(componentInParent))
		{
			return;
		}
		BerthTransform = other.transform;
		if (!IsActivated || !vessel.ActionGroups.groups[5])
		{
			return;
		}
		Vector3 vector = transform.InverseTransformDirection(vessel.CoM - other.transform.position);
		Vector3 vector2 = transform.InverseTransformDirection(part.rb.velocity - componentInParent.vessel.rootPart.rb.velocity);
		Vector3 vector3 = transform.TransformDirection(vector + vector2);
		Vector3 vector4 = FlightGlobals.getGeeForceAtPosition(vessel.transform.position);
		foreach (Part part in vessel.parts)
		{
			if (part.physicalSignificance == Part.PhysicalSignificance.FULL && part.rb != null)
			{
				part.AddForce(-(vector4 + Gravity * vector3) * part.rb.mass);
			}
		}
	}

	private bool IsBerth(Part IRpart)
	{
		foreach (PartModule module in IRpart.Modules)
		{
			if (module.moduleName == "MagneticBerth")
			{
				return true;
			}
		}
		return false;
	}
}
