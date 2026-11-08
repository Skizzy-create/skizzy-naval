using UnityEngine;

namespace ACA;

public class MagneticBerth : PartModule
{
	[KSPField(isPersistant = true)]
	private bool IsActivated = false;

	[KSPField(isPersistant = true)]
	private Vector3 CusSize;

	private Transform BerthTransform;

	[KSPField(guiActive = true, guiActiveEditor = true, guiName = "Gravity ", isPersistant = true)]
	[UI_FloatRange(minValue = -20f, maxValue = 20f, stepIncrement = 5f)]
	public float Gravity = 0f;
}
