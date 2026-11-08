using UnityEngine;

public class FuelNozzle : PartModule
{
	[KSPField]
	public float wiretension = 1f;

	private Transform PipeTransform = null;

	private Transform ConnectorTransform = null;

	private Transform triggerTransform = null;

	private Transform NozzleCapTransform = null;

	public bool IsHooked = false;

	public Part FP = null;

	[KSPField(isPersistant = true)]
	public bool Ispassive = true;

	[KSPField(isPersistant = true)]
	public bool IsActivated = true;

	[KSPField(guiActive = true, guiActiveEditor = true, guiName = "WireLength", isPersistant = true)]
	[UI_FloatRange(minValue = 1f, maxValue = 10f, stepIncrement = 1f)]
	public float MaxWireLength = 5f;

	[KSPAction("Toggle Nozzle", KSPActionGroup.None)]
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
		PipeTransform = null;
		IsHooked = false;
	}

	public override void OnStart(StartState state)
	{
		enabled = true;
		part.force_activate();
		PipeTransform = part.FindModelTransform("PipeTransform");
		triggerTransform = part.FindModelTransform("triggerTransform");
		ConnectorTransform = part.FindModelTransform("Connector");
		NozzleCapTransform = part.FindModelTransform("NozzleCap");
	}

	public override void OnFixedUpdate()
	{
		if (IsActivated)
		{
			triggerTransform.localScale = Vector3.one * MaxWireLength;
		}
		else
		{
			triggerTransform.localScale = Vector3.zero;
		}
		if (IsHooked)
		{
			ConnectorTransform.localScale = Vector3.one * 0.2f;
			NozzleCapTransform.localScale = Vector3.zero;
		}
		else
		{
			ConnectorTransform.localScale = Vector3.zero;
			NozzleCapTransform.localScale = Vector3.one;
		}
		IsHooked = false;
	}

	private void OnTriggerStay(Collider other)
	{
	}
}
