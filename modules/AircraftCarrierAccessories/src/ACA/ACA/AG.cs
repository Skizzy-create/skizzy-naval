using UnityEngine;

namespace ACA;

public class AG : PartModule
{
	[KSPField(isPersistant = true)]
	private bool IsActivated = false;

	[KSPField(isPersistant = true)]
	private Vector3 CusSize;

	private Transform GravityDirection;

	private Transform GravityCube;

	[KSPField(guiActive = true, guiActiveEditor = true, guiName = "Gravity ", isPersistant = true)]
	[UI_FloatRange(minValue = -20f, maxValue = 20f, stepIncrement = 5f)]
	public float Gravity = 0f;

	[KSPField(guiActive = false, guiActiveEditor = true, guiName = "Height ", isPersistant = true)]
	[UI_FloatRange(minValue = 1f, maxValue = 50f, stepIncrement = 5f)]
	public float Height = 0f;

	[KSPField(guiActive = false, guiActiveEditor = true, guiName = "Length ", isPersistant = true)]
	[UI_FloatRange(minValue = 1f, maxValue = 50f, stepIncrement = 5f)]
	public float Length = 0f;

	[KSPField(guiActive = false, guiActiveEditor = true, guiName = "Width ", isPersistant = true)]
	[UI_FloatRange(minValue = 1f, maxValue = 10f, stepIncrement = 1f)]
	public float Width = 0f;

	[KSPAction("Toggle Gravity", KSPActionGroup.None)]
	private void ActionActivate(KSPActionParam param)
	{
		if (!IsActivated)
		{
			PadActivated();
		}
		else
		{
			PadDeavtivated();
		}
	}

	[KSPEvent(name = "PadActivated", guiName = "Activate Pad", active = true, guiActive = true)]
	public void PadActivated()
	{
		IsActivated = true;
		Events["PadDeavtivated"].guiActive = true;
		Events["PadActivated"].guiActive = false;
		Debug.Log("PadActivated");
	}

	[KSPEvent(name = "PadDeavtivated", guiName = "Deactivate Pad", active = true, guiActive = false)]
	public void PadDeavtivated()
	{
		IsActivated = false;
		Events["PadDeavtivated"].guiActive = false;
		Events["PadActivated"].guiActive = true;
	}

	public override void OnStart(StartState state)
	{
		enabled = true;
		part.force_activate();
		if ((bool)part.FindModelTransform("GravityDirection"))
		{
			GravityDirection = part.FindModelTransform("GravityDirection");
		}
		if ((bool)part.FindModelTransform("GravityCube"))
		{
			GravityCube = part.FindModelTransform("GravityCube");
		}
	}

	public override void OnUpdate()
	{
		if (HighLogic.LoadedSceneIsEditor)
		{
			if ((bool)part.FindModelTransform("GravityCube"))
			{
				GravityCube = part.FindModelTransform("GravityCube");
			}
			GravityCube.localScale = new Vector3(Height, Width, Length);
		}
	}

	public override void OnFixedUpdate()
	{
		if (IsActivated)
		{
			GravityCube.localScale = new Vector3(Height, Width, Length);
		}
		else
		{
			GravityCube.localScale = Vector3.zero;
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (IsActivated && (bool)other.gameObject.GetComponentInParent<Part>() && !other.gameObject.GetComponentInParent<Part>().vessel.isEVA)
		{
			Part componentInParent = other.gameObject.GetComponentInParent<Part>();
			if ((bool)componentInParent && (bool)componentInParent.vessel && componentInParent.vessel != vessel)
			{
				componentInParent.AddForceAtPosition(componentInParent.mass * Gravity * GravityDirection.forward, componentInParent.transform.position);
				part.AddForceAtPosition((0f - componentInParent.mass) * Gravity * GravityDirection.forward, componentInParent.transform.position);
			}
		}
	}
}
