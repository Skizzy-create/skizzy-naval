using UnityEngine;
using UnityEngine.Rendering;

public class Tiedown : PartModule
{
	[KSPField]
	public float wiretension = 1f;

	private Transform TieTransform = null;

	private Transform FPTransform = null;

	private Transform triggerTransform = null;

	public bool IsHooked = false;

	public Part FP = null;

	[KSPField(isPersistant = true)]
	public bool Ispassive = true;

	[KSPField(isPersistant = true)]
	public bool IsActivated = true;

	[KSPField(guiActive = true, guiActiveEditor = true, guiName = "Force Ratio", isPersistant = true)]
	[UI_FloatRange(minValue = 1f, maxValue = 10f, stepIncrement = 1f)]
	public float ForceRatio = 1f;

	[KSPField(guiActive = true, guiActiveEditor = true, guiName = "WireLength", isPersistant = true)]
	[UI_FloatRange(minValue = 1f, maxValue = 10f, stepIncrement = 1f)]
	public float MaxWireLength = 1f;

	[KSPAction("Toggle TieDown", KSPActionGroup.None)]
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
		TieTransform = null;
		IsHooked = false;
	}

	public override void OnStart(StartState state)
	{
		enabled = true;
		part.force_activate();
		TieTransform = part.FindModelTransform("TieTransform");
		triggerTransform = part.FindModelTransform("triggerTransform");
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
			LineRenderer component = part.gameObject.GetComponent<LineRenderer>();
			if (component == null)
			{
				component = part.gameObject.AddComponent<LineRenderer>();
				Color grey = Color.grey;
				grey.a /= 2f;
				component.material = new Material(Shader.Find("KSP/Alpha/Cutoff"));
				component.material.mainTexture = GameDatabase.Instance.GetTexture("KFC/Textures/chain", asNormalMap: false);
				component.shadowCastingMode = ShadowCastingMode.Off;
				component.receiveShadows = false;
				component.widthCurve = new AnimationCurve(new Keyframe(0f, 0.1f), new Keyframe(1f, 0.1f));
				component.positionCount = 2;
				component.SetPosition(0, TieTransform.position + part.rb.velocity * Time.fixedDeltaTime);
				component.SetPosition(1, FPTransform.position + part.rb.velocity * Time.fixedDeltaTime);
				component.useWorldSpace = true;
				component.enabled = true;
			}
			else
			{
				component.SetPosition(0, TieTransform.position + part.rb.velocity * Time.fixedDeltaTime);
				component.SetPosition(1, FPTransform.position + part.rb.velocity * Time.fixedDeltaTime);
				component.enabled = true;
			}
		}
		else
		{
			LineRenderer component2 = part.gameObject.GetComponent<LineRenderer>();
			if (component2 != null)
			{
				Object.Destroy(component2);
			}
		}
	}

	private void OnTriggerStay(Collider other)
	{
	}
}
