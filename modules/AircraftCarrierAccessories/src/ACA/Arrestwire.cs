using UnityEngine;

public class Arrestwire : PartModule
{
	[KSPField]
	public float wiretension = 1f;

	private Transform ARP1Transform = null;

	private Transform ARP2Transform = null;

	private Transform WireTransform = null;

	public bool IsHooked = false;

	[KSPField(guiActive = true, guiActiveEditor = true, guiName = "Force Ratio", isPersistant = true)]
	[UI_FloatRange(minValue = 1f, maxValue = 10f, stepIncrement = 1f)]
	public float ForceRatio = 1f;

	public override void OnStart(StartState state)
	{
		enabled = true;
		part.force_activate();
		ARP1Transform = part.FindModelTransform("ARP1");
		ARP2Transform = part.FindModelTransform("ARP2");
		WireTransform = part.FindModelTransform("Wire");
	}

	public override void OnFixedUpdate()
	{
		if (!IsHooked)
		{
			WireTransform.localScale = Vector3.one;
		}
		else
		{
			WireTransform.localScale = Vector3.zero;
		}
	}
}
