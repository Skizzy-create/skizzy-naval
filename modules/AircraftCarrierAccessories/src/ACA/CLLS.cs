using System;
using UnityEngine;

public class CLLS : PartModule
{
	[KSPField]
	public float maxlength = 200f;

	public float width = 10f;

	public float length = 20f;

	public float TargetDA = 0f;

	public float PrepareP = 0f;

	public Vector3 ReP = Vector3.zero;

	public Vector3 ReV = Vector3.zero;

	private Vessel Target = null;

	private Transform CoreTransform = null;

	private Transform Guide1Transform = null;

	private Transform Guide2Transform = null;

	private Transform InstructionTransform = null;

	private Transform BaseTransform = null;

	private Transform TriggerTransform = null;

	private Transform ColliderTransform = null;

	public MeshRenderer Guide1Color = null;

	public MeshRenderer Guide2Color = null;

	public MeshRenderer InstructionColor = null;

	public MeshRenderer BaseRenderer = null;

	public float time = 0f;

	private PidController xPid = new PidController(10f, 0.005f, 0.002f, 50, 10f);

	private PidController yPid = new PidController(10f, 0.005f, 0.002f, 50, 10f);

	private PidController zPid = new PidController(10f, 0.005f, 0.002f, 50, 10f);

	[KSPField(isPersistant = true)]
	public bool IsActivated = false;

	public bool IsReseting = false;

	public bool IsGrabing = false;

	public bool IsReady = false;

	public bool IsLaunching = false;

	public bool IsBreak = false;

	[KSPField(isPersistant = true)]
	public bool IsLanding = false;

	[KSPField(guiActive = true, guiActiveEditor = true, guiName = "Acceleration", isPersistant = true)]
	[UI_FloatRange(minValue = 10f, maxValue = 60f, stepIncrement = 10f)]
	public float Acceleration = 30f;

	[KSPAction("Toggle System", KSPActionGroup.None)]
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

	[KSPEvent(name = "Activate", guiName = "Activate", active = true, guiActive = true)]
	public void Activate()
	{
		IsActivated = true;
		Events["Deactivate"].guiActive = true;
		Events["Activate"].guiActive = false;
		InstructionTransform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
		Guide1Color.material.SetTextureScale("_MainTex", new Vector2(length, 1f));
		Guide2Color.material.SetTextureScale("_MainTex", new Vector2(length, 1f));
		Guide1Transform.localScale = new Vector3(length, 1f, 1f) / 10f;
		Guide1Transform.localPosition = new Vector3(width, length, 0f) / 2f;
		Guide2Transform.localScale = new Vector3(length, 1f, 1f) / 10f;
		Guide2Transform.localPosition = new Vector3(0f - width, length, 0f) / 2f;
	}

	[KSPEvent(name = "Deactivate", guiName = "Deactivate", active = true, guiActive = false)]
	public void Deactivate()
	{
		IsActivated = false;
		Events["Deactivate"].guiActive = false;
		Events["Activate"].guiActive = true;
		Target = null;
		InstructionTransform.localScale = new Vector3(0f, 0f, 0f);
		Guide1Color.material.SetTextureScale("_MainTex", new Vector2(length, 1f));
		Guide2Color.material.SetTextureScale("_MainTex", new Vector2(length, 1f));
		Guide1Transform.localScale = new Vector3(length, 1f, 1f) / 10f;
		Guide1Transform.localPosition = new Vector3(width, length, 0f) / 2f;
		Guide2Transform.localScale = new Vector3(length, 1f, 1f) / 10f;
		Guide2Transform.localPosition = new Vector3(0f - width, length, 0f) / 2f;
	}

	[KSPAction("Toggle", KSPActionGroup.None, guiName = "Transform")]
	private void ActionTransform(KSPActionParam param)
	{
		if (IsLanding)
		{
			Launch();
		}
		else
		{
			Land();
		}
	}

	[KSPEvent(name = "Launch", guiName = "Launcher", active = true, guiActive = true)]
	public void Launch()
	{
		IsLanding = false;
		Events["Land"].guiActive = true;
		Events["Launch"].guiActive = false;
		Guide1Color.material.SetTextureScale("_MainTex", new Vector2(length, 1f));
		Guide2Color.material.SetTextureScale("_MainTex", new Vector2(length, 1f));
		Guide1Transform.localScale = new Vector3(length, 1f, 1f) / 10f;
		Guide1Transform.localPosition = new Vector3(width, length, 0f) / 2f;
		Guide2Transform.localScale = new Vector3(length, 1f, 1f) / 10f;
		Guide2Transform.localPosition = new Vector3(0f - width, length, 0f) / 2f;
		CoreTransform.localPosition = new Vector3(0f, length, 0f) / 2f;
		InstructionTransform.localPosition = new Vector3(0f, length, 0f);
	}

	[KSPEvent(name = "Land", guiName = "Arrester", active = true, guiActive = false)]
	public void Land()
	{
		IsLanding = true;
		Events["Land"].guiActive = false;
		Events["Launch"].guiActive = true;
	}

	public override void OnStart(StartState state)
	{
		enabled = true;
		part.force_activate();
		CoreTransform = part.FindModelTransform("Core");
		Guide1Transform = part.FindModelTransform("Guide1");
		Guide2Transform = part.FindModelTransform("Guide2");
		InstructionTransform = part.FindModelTransform("Instruction");
		BaseTransform = part.FindModelTransform("Base");
		TriggerTransform = part.FindModelTransform("Trigger");
		ColliderTransform = part.FindModelTransform("Collider");
		ColliderTransform.localScale = Vector3.zero;
		Guide1Transform.localScale = new Vector3(length, 1f, 1f) / 10f;
		Guide1Transform.localPosition = new Vector3(width, length, 0f) / 2f;
		Guide2Transform.localScale = new Vector3(length, 1f, 1f) / 10f;
		Guide2Transform.localPosition = new Vector3(0f - width, length, 0f) / 2f;
		CoreTransform.localPosition = new Vector3(0f, length, 0f) / 2f;
		InstructionTransform.localPosition = new Vector3(0f, length, 0f);
		Guide1Color = Guide1Transform.gameObject.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
		Guide1Color.material.SetTextureScale("_MainTex", new Vector2(length, 1f));
		Guide2Color = Guide2Transform.gameObject.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
		Guide2Color.material.SetTextureScale("_MainTex", new Vector2(length, 1f));
		InstructionColor = InstructionTransform.gameObject.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
		// SkizzyNaval: CLLS.mu has no "Base" transform, so BaseTransform is null here and the original code threw a
		// NullReferenceException that aborted OnStart (the saved state and the guide setup below never ran).
		BaseRenderer = (BaseTransform != null) ? (BaseTransform.gameObject.GetComponent(typeof(MeshRenderer)) as MeshRenderer) : null;
		if (HighLogic.LoadedSceneIsFlight)
		{
			enabled = true;
			part.force_activate();
			if (BaseRenderer != null)
			{
				BaseRenderer.enabled = false;
			}
		}
		if (!IsActivated)
		{
			Activate();
		}
		else
		{
			Deactivate();
		}
		if (IsLanding)
		{
			Launch();
		}
		else
		{
			Land();
		}
	}

	public override void OnFixedUpdate()
	{
		ColliderTransform.localScale = Vector3.zero;
		if (IsActivated)
		{
			Color value = Color.red;
			if ((bool)Target)
			{
				if (Target.ActionGroups.groups[5])
				{
					IsBreak = true;
				}
				else
				{
					IsBreak = false;
				}
			}
			if (!Guide1Color)
			{
				Guide1Color = Guide1Transform.gameObject.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
			}
			if (!Guide2Color)
			{
				Guide2Color = Guide2Transform.gameObject.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
			}
			if (!InstructionColor)
			{
				InstructionColor = InstructionTransform.gameObject.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
			}
			if ((bool)Target)
			{
				ReP = CoreTransform.InverseTransformDirection(CoreTransform.position - Target.transform.position);
				ReV = CoreTransform.InverseTransformDirection(part.rb.velocity - Target.rootPart.rb.velocity);
			}
			else
			{
				IsGrabing = false;
				IsReady = false;
				IsLaunching = false;
				IsReseting = false;
			}
			if (!IsLanding)
			{
				TriggerTransform.localScale = new Vector3(width, length, 2f);
				TriggerTransform.localPosition = new Vector3(0f, length / 2f, 0f);
				if (IsGrabing)
				{
					Vector3 direction = ReV + ReP;
					Vector3 vector = CoreTransform.TransformDirection(direction);
					value = Color.blue;
					InstructionColor.material.mainTexture = GameDatabase.Instance.GetTexture("KFC/Textures/StandBy", asNormalMap: false);
					if (time < maxlength)
					{
						time++;
						Guide1Color.material.SetTextureScale("_MainTex", new Vector2(time, 1f));
						Guide2Color.material.SetTextureScale("_MainTex", new Vector2(time, 1f));
						Guide1Transform.localScale = new Vector3(time, 1f, 1f) / 10f;
						Guide1Transform.localPosition = new Vector3(width, time, 0f) / 2f;
						Guide2Transform.localScale = new Vector3(time, 1f, 1f) / 10f;
						Guide2Transform.localPosition = new Vector3(0f - width, time, 0f) / 2f;
					}
					else if (time == maxlength && !IsReady && !IsLaunching && IsBreak)
					{
						IsReady = true;
					}
					if (!IsLaunching)
					{
						float num = 0f;
						foreach (Part part in Target.parts)
						{
							if (part.physicalSignificance == Part.PhysicalSignificance.FULL && part.rb != null)
							{
								part.AddForce(vector * part.rb.mass);
								num += part.rb.mass;
							}
						}
						foreach (Part part2 in vessel.parts)
						{
							if (part2.physicalSignificance == Part.PhysicalSignificance.FULL && part2.rb != null)
							{
								part2.AddForce(-vector * part2.rb.mass / num);
							}
						}
						double num2 = vector.magnitude * num * Time.fixedDeltaTime;
						double value2 = base.part.RequestResource("ElectricCharge", num2);
						if (Math.Round(num2, 4) != 0.0 && Math.Round(value2, 4) < Math.Round(num2, 4))
						{
							Deactivate();
						}
					}
				}
				if (IsReady)
				{
					value = Color.yellow;
					InstructionColor.material.mainTexture = GameDatabase.Instance.GetTexture("KFC/Textures/Ready", asNormalMap: false);
					if (!IsBreak && !IsLaunching)
					{
						IsLaunching = true;
					}
				}
				if (IsLaunching)
				{
					value = Color.green;
					Vector3 vector2 = CoreTransform.TransformDirection(Vector3.up) * Acceleration * Time.fixedDeltaTime;
					InstructionColor.material.mainTexture = GameDatabase.Instance.GetTexture("KFC/Textures/Launch", asNormalMap: false);
					float num3 = 0f;
					foreach (Part part3 in Target.parts)
					{
						if (part3.physicalSignificance == Part.PhysicalSignificance.FULL && part3.rb != null)
						{
							part3.AddForce(vector2 * part3.rb.mass * 10f);
							num3 += part3.rb.mass;
						}
					}
					foreach (Part part4 in vessel.parts)
					{
						if (part4.physicalSignificance == Part.PhysicalSignificance.FULL && part4.rb != null)
						{
							part4.AddForce(-vector2 * (float)vessel.totalMass * 10f / num3);
						}
					}
					double num4 = vector2.magnitude * num3 * Time.fixedDeltaTime;
					double value3 = base.part.RequestResource("ElectricCharge", num4);
					if (Math.Round(num4, 4) != 0.0 && Math.Round(value3, 4) < Math.Round(num4, 4))
					{
						Deactivate();
					}
					if (ReP.magnitude > maxlength || IsBreak)
					{
						IsReseting = true;
					}
				}
				if (IsReseting)
				{
					CoreTransform.localPosition = Vector3.zero;
					IsReseting = false;
					IsGrabing = false;
					IsReady = false;
					IsLaunching = false;
					Target = null;
					value = Color.red;
					time = 0f;
					Guide1Color.material.SetTextureScale("_MainTex", new Vector2(length, 1f));
					Guide2Color.material.SetTextureScale("_MainTex", new Vector2(length, 1f));
					Guide1Transform.localScale = new Vector3(length, 1f, 1f) / 10f;
					Guide1Transform.localPosition = new Vector3(width, length, 0f) / 2f;
					Guide2Transform.localScale = new Vector3(length, 1f, 1f) / 10f;
					Guide2Transform.localPosition = new Vector3(0f - width, length, 0f) / 2f;
					CoreTransform.localPosition = new Vector3(0f, length, 0f) / 2f;
					InstructionTransform.localPosition = new Vector3(0f, length, 0f);
				}
			}
			else
			{
				TriggerTransform.localScale = new Vector3(width, maxlength, 5f);
				TriggerTransform.localPosition = new Vector3(0f, maxlength / 2f, 0f);
				Guide1Color.material.SetTextureScale("_MainTex", new Vector2(0f - maxlength, 1f));
				Guide2Color.material.SetTextureScale("_MainTex", new Vector2(0f - maxlength, 1f));
				Guide1Transform.localScale = new Vector3(maxlength, 1f, 1f) / 10f;
				Guide1Transform.localPosition = new Vector3(width, maxlength, 0f) / 2f;
				Guide2Transform.localScale = new Vector3(maxlength, 1f, 1f) / 10f;
				Guide2Transform.localPosition = new Vector3(0f - width, maxlength, 0f) / 2f;
				if ((bool)Target)
				{
					Vector3 direction2 = ReP + ReV;
					direction2.y = Mathf.Max(direction2.y, 0f);
					Vector3 vector3 = CoreTransform.TransformDirection(direction2) * 10f;
					float num5 = 0f;
					foreach (Part part5 in Target.parts)
					{
						if (part5.physicalSignificance == Part.PhysicalSignificance.FULL && part5.rb != null)
						{
							part5.AddForce(vector3 * part5.rb.mass);
							num5 += part5.rb.mass;
						}
					}
					foreach (Part part6 in vessel.parts)
					{
						if (part6.physicalSignificance == Part.PhysicalSignificance.FULL && part6.rb != null)
						{
							part6.AddForce(-vector3 * (float)vessel.totalMass / num5);
						}
					}
					double num6 = vector3.magnitude * num5 * Time.fixedDeltaTime;
					double value4 = base.part.RequestResource("ElectricCharge", num6);
					if (Math.Round(num6, 4) != 0.0 && Math.Round(value4, 4) < Math.Round(num6, 4))
					{
						Deactivate();
					}
					if (!IsBreak)
					{
						Deactivate();
					}
					if (ReP.magnitude <= 0.1f && ReV.magnitude <= 0.5f)
					{
						Deactivate();
					}
				}
			}
			Guide1Color.material.SetColor("_TintColor", value);
			Guide2Color.material.SetColor("_TintColor", value);
			InstructionColor.material.SetColor("_TintColor", value);
		}
		else
		{
			Guide1Color.material.SetColor("_TintColor", Color.black);
			Guide2Color.material.SetColor("_TintColor", Color.black);
			InstructionColor.material.SetColor("_TintColor", Color.black);
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (IsReseting || IsReady || IsGrabing || IsLaunching || !IsActivated)
		{
			return;
		}
		Part componentInParent = other.gameObject.GetComponentInParent<Part>();
		if ((bool)componentInParent && (bool)componentInParent.vessel && componentInParent.vessel != vessel)
		{
			if (!IsLanding)
			{
				IsGrabing = true;
				Target = componentInParent.vessel;
			}
			else if (componentInParent.vessel.ActionGroups.groups[5])
			{
				IsGrabing = true;
				Target = componentInParent.vessel;
			}
		}
	}
}
