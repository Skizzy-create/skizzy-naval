using System.Collections.Generic;
using UnityEngine;

public class FuelCap : PartModule
{
	[KSPField]
	private Transform PipeTransform = null;

	private Transform ConnectorTransform = null;

	private Transform CapTransform = null;

	private Transform RopeTransform = null;

	public bool IsHooked = false;

	public bool IsBreak = false;

	public Part LinkedPart = null;

	public float CableLength = 0f;

	public FuelNozzle Nozzle = null;

	public MeshRenderer RopeRender = null;

	private List<PartResource> ResourceList = null;

	[KSPField(isPersistant = true)]
	public bool IsActivated = true;

	[KSPField(isPersistant = true)]
	public float Speed = 20f;

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
		PipeTransform = null;
		IsHooked = false;
		LinkedPart = null;
		Nozzle = null;
	}

	public override void OnStart(StartState state)
	{
		enabled = true;
		part.force_activate();
		CapTransform = part.FindModelTransform("CapTransform");
		ConnectorTransform = part.FindModelTransform("Connector");
		RopeTransform = part.FindModelTransform("Rope");
		RopeRender = RopeTransform.gameObject.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
	}

	public override void OnFixedUpdate()
	{
		if (IsActivated && LinkedPart != null && Nozzle.IsActivated)
		{
			if (!IsConnected(LinkedPart))
			{
				PipeTransform = null;
				LinkedPart = null;
				Nozzle = null;
				ConnectorTransform.localScale = Vector3.zero;
				return;
			}
			float num = Vector3.Magnitude(part.rb.velocity - LinkedPart.rb.velocity);
			CableLength = Vector3.Distance(PipeTransform.position + LinkedPart.vel * Time.fixedDeltaTime, CapTransform.position);
			if (vessel.ActionGroups.groups[5])
			{
				IsBreak = true;
				if (!IsHooked)
				{
					if (num < 0.2f)
					{
						IsHooked = true;
					}
					return;
				}
				if (PipeTransform != null)
				{
					ConnectorTransform.localScale = Vector3.one * 1.6f;
					Vector3 vector = PipeTransform.position + LinkedPart.vel * Time.fixedDeltaTime - CapTransform.position;
					Quaternion quaternion = Quaternion.FromToRotation(CapTransform.up, vector.normalized);
					CapTransform.rotation = Quaternion.Slerp(CapTransform.rotation, quaternion * CapTransform.rotation, 1f);
					CapTransform.localScale = new Vector3(1f, CableLength * 2.5f, 1f);
					RopeRender.material.SetTextureScale("_MainTex", new Vector2(1f, CableLength * 2.5f));
					Nozzle.IsHooked = true;
					if (ResourceList == null)
					{
						ResourceList = new List<PartResource>();
					}
					foreach (Part part in vessel.Parts)
					{
						if (part.Resources.Count != 0)
						{
							foreach (PartResource resource in part.Resources)
							{
								if (!ResourceList.Contains(resource))
								{
									ResourceList.Add(resource);
								}
							}
						}
						else
						{
							if (part.children.Count != 0)
							{
								continue;
							}
							foreach (PartModule module in part.Modules)
							{
								if (module.moduleName == "ReloadableRail")
								{
									ReloadableRail reloadableRail = (ReloadableRail)module;
									reloadableRail.Resupply();
								}
							}
						}
					}
					{
						foreach (PartResource resource2 in ResourceList)
						{
							double amount = 0.0;
							double maxAmount = 0.0;
							double amount2 = 0.0;
							double maxAmount2 = 0.0;
							LinkedPart.GetConnectedResourceTotals(resource2.info.id, out amount, out maxAmount);
							if (amount != 0.0)
							{
								base.part.GetConnectedResourceTotals(resource2.info.id, out amount2, out maxAmount2);
								if (amount2 < maxAmount2)
								{
									double num2 = LinkedPart.RequestResource(resource2.info.id, (double)Mathf.Min((float)maxAmount2 - (float)amount2, (float)amount, Speed * Time.fixedDeltaTime));
									base.part.RequestResource(resource2.info.id, 0.0 - num2);
								}
							}
						}
						return;
					}
				}
				ConnectorTransform.localScale = Vector3.zero;
			}
			else
			{
				IsBreak = false;
				if (IsHooked || Vector3.Distance(LinkedPart.rb.position, base.part.rb.position) > Nozzle.MaxWireLength)
				{
					IsHooked = false;
					PipeTransform = null;
					LinkedPart = null;
					Nozzle = null;
					ConnectorTransform.localScale = Vector3.zero;
				}
			}
		}
		else
		{
			ConnectorTransform.localScale = Vector3.zero;
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
		if ((bool)componentInParent.vessel && componentInParent.vessel != vessel && num < 5f && IsNozzle(componentInParent))
		{
			if (LinkedPart == null)
			{
				LinkedPart = componentInParent;
				PipeTransform = LinkedPart.FindModelTransform("PipeTransform");
			}
			else if (Vector3.Distance(componentInParent.transform.position, part.transform.position) < Vector3.Distance(LinkedPart.transform.position, part.transform.position))
			{
				LinkedPart = componentInParent;
				PipeTransform = LinkedPart.FindModelTransform("PipeTransform");
			}
		}
	}

	private bool IsNozzle(Part IRpart)
	{
		foreach (PartModule module in IRpart.Modules)
		{
			if (module.moduleName == "FuelNozzle")
			{
				Nozzle = (FuelNozzle)module;
				if (IsConnected(IRpart) && Nozzle.IsActivated)
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool IsConnected(Part IRpart)
	{
		Ray ray = new Ray(CapTransform.position, IRpart.transform.position - CapTransform.position);
		if (Physics.Raycast(ray, out var hitInfo, Vector3.Magnitude(CapTransform.position - IRpart.transform.position), 2097153))
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
