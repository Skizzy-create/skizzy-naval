using UnityEngine;
using UnityEngine.Rendering;

public class FixPoint : PartModule
{
	[KSPField]
	private Transform TieTransform = null;

	private Transform FPTransform = null;

	public bool IsHooked = false;

	public bool IsBreak = false;

	public Part LinkedPart = null;

	public float CableLength = 0f;

	public Tiedown Tiedown = null;

	[KSPField(isPersistant = true)]
	public bool IsActivated = true;

	[KSPField(guiActive = true, guiActiveEditor = true, guiName = "Cable Preload", isPersistant = true)]
	[UI_FloatRange(minValue = 0f, maxValue = 5f, stepIncrement = 0.25f)]
	public float Preload = 0f;

	[KSPAction("Toggle FixPoint", KSPActionGroup.None)]
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
		LinkedPart = null;
		Tiedown = null;
	}

	public override void OnStart(StartState state)
	{
		enabled = true;
		part.force_activate();
		FPTransform = part.FindModelTransform("FixPointTransform");
	}

	public override void OnFixedUpdate()
	{
		if (IsActivated && LinkedPart != null && Tiedown.IsActivated)
		{
			if (!IsConnected(LinkedPart))
			{
				TieTransform = null;
				LinkedPart = null;
				Tiedown = null;
				return;
			}
			Color value = Color.green;
			float num = Vector3.Magnitude(part.rb.velocity - LinkedPart.rb.velocity);
			if (vessel.ActionGroups.groups[5])
			{
				IsBreak = true;
				if (!IsHooked)
				{
					if (num < 0.2f)
					{
						IsHooked = true;
						CableLength = Vector3.Distance(LinkedPart.rb.position, part.rb.position);
					}
				}
				else
				{
					value = new Color(1f, 0.6f, 1f, 1f);
					float num2 = Vector3.Distance(LinkedPart.rb.position, part.rb.position) - CableLength;
					if (num2 > 0f)
					{
						part.AddForce((LinkedPart.rb.position - part.rb.position).normalized * (num2 * 1000f + Preload * 10f) * (float)vessel.totalMass);
						LinkedPart.AddForce(-(LinkedPart.rb.position - part.rb.position).normalized * (num2 * 1000f + Preload * 10f) * (float)vessel.totalMass);
					}
					else
					{
						CableLength = Mathf.Max(1f, Vector3.Distance(LinkedPart.rb.position, part.rb.position));
					}
				}
			}
			else
			{
				IsBreak = false;
				value = Color.green;
				if (IsHooked || Vector3.Distance(LinkedPart.rb.position, part.rb.position) > Tiedown.MaxWireLength)
				{
					IsHooked = false;
					TieTransform = null;
					LinkedPart = null;
					Tiedown = null;
				}
			}
			LineRenderer component = part.gameObject.GetComponent<LineRenderer>();
			if (TieTransform != null)
			{
				if (component == null)
				{
					component = part.gameObject.AddComponent<LineRenderer>();
					value.a /= 2f;
					component.material = new Material(Shader.Find("KSP/Particles/Alpha Blended"));
					component.material.SetColor("_TintColor", value);
					component.material.mainTexture = GameDatabase.Instance.GetTexture("KFC/Textures/chain", asNormalMap: false);
					component.material.SetTextureScale("_MainTex", new Vector2(Vector3.Distance(LinkedPart.rb.position, part.rb.position) * 4f, 1f));
					component.shadowCastingMode = ShadowCastingMode.Off;
					component.receiveShadows = false;
					component.startWidth = 0.25f;
					component.endWidth = 0.25f;
					component.SetPosition(0, TieTransform.position + LinkedPart.rb.velocity * Time.fixedDeltaTime);
					component.SetPosition(1, FPTransform.position + part.rb.velocity * Time.fixedDeltaTime);
					component.useWorldSpace = true;
					component.enabled = true;
				}
				else
				{
					component.SetPosition(0, TieTransform.position + LinkedPart.rb.velocity * Time.fixedDeltaTime);
					component.SetPosition(1, FPTransform.position + part.rb.velocity * Time.fixedDeltaTime);
					value.a /= 2f;
					component.material.SetColor("_TintColor", value);
					component.material.SetTextureScale("_MainTex", new Vector2(Vector3.Distance(LinkedPart.rb.position, part.rb.position) * 4f, 1f));
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
		else
		{
			LineRenderer component3 = part.gameObject.GetComponent<LineRenderer>();
			if (component3 != null)
			{
				Object.Destroy(component3);
			}
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (IsHooked || !IsActivated)
		{
			return;
		}
		Part componentInParent = other.gameObject.GetComponentInParent<Part>();
		if (!componentInParent)
		{
			return;
		}
		float num = Vector3.Magnitude(part.rb.velocity - componentInParent.rb.velocity);
		if ((bool)componentInParent.vessel && componentInParent.vessel != vessel && num < 5f && IsTiedown(componentInParent))
		{
			if (LinkedPart == null)
			{
				LinkedPart = componentInParent;
				TieTransform = LinkedPart.FindModelTransform("TieTransform");
			}
			else if (Vector3.Distance(componentInParent.transform.position, part.transform.position) < Vector3.Distance(LinkedPart.transform.position, part.transform.position))
			{
				LinkedPart = componentInParent;
				TieTransform = LinkedPart.FindModelTransform("TieTransform");
			}
		}
	}

	private bool IsTiedown(Part IRpart)
	{
		foreach (PartModule module in IRpart.Modules)
		{
			if (module.moduleName == "Tiedown")
			{
				Tiedown = (Tiedown)module;
				if (IsConnected(IRpart) && Tiedown.IsActivated)
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool IsConnected(Part IRpart)
	{
		Ray ray = new Ray(FPTransform.position, IRpart.transform.position - FPTransform.position);
		if (Physics.Raycast(ray, out var hitInfo, Vector3.Magnitude(FPTransform.position - IRpart.transform.position), 2097153))
		{
			Part componentInParent = hitInfo.collider.GetComponentInParent<Part>();
			if (componentInParent == IRpart)
			{
				return true;
			}
		}
		return false;
	}
}
