using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Tailhook : PartModule
{
	[KSPField]
	public float maxwiretension = 100f;

	public float wiretension = 100f;

	public float TargetDA = 0f;

	public float BlockA = 0f;

	private Transform HookTransform = null;

	private Transform ARP1Transform = null;

	private Transform ARP2Transform = null;

	public bool IsDeploy = false;

	public bool IsHooked = false;

	public bool IsBlocked = false;

	private Arrestwire AW = null;

	public AnimationState[] deployStates;

	[KSPField(isPersistant = false)]
	public string deployAnimName = "HookAnim";

	[KSPField(isPersistant = true)]
	public float MaxDA = 60f;

	[KSPField(guiActive = true, guiActiveEditor = false, guiName = "Deploy Angle", isPersistant = true)]
	[UI_FloatRange(minValue = 0f, maxValue = 90f, stepIncrement = 5f)]
	public float DeployAngle = 45f;

	[KSPAction("Toggle Hook", KSPActionGroup.None)]
	private void ActionActivate(KSPActionParam param)
	{
		if (!IsDeploy)
		{
			HookDeploy();
		}
		else
		{
			HookRetract();
		}
	}

	[KSPEvent(name = "HookDeploy", guiName = "Deploy Hook", active = true, guiActive = true)]
	public void HookDeploy()
	{
		IsDeploy = true;
		Events["HookRetract"].guiActive = true;
		Events["HookDeploy"].guiActive = false;
		wiretension = maxwiretension;
		Debug.Log("Hook Deployed");
	}

	[KSPEvent(name = "HookRetract", guiName = "Retract Hook", active = true, guiActive = false)]
	public void HookRetract()
	{
		IsDeploy = false;
		Events["HookRetract"].guiActive = false;
		Events["HookDeploy"].guiActive = true;
		IsHooked = false;
		wiretension = maxwiretension;
		LineRenderer component = part.gameObject.GetComponent<LineRenderer>();
		if ((bool)component)
		{
			Object.Destroy(component);
		}
		if ((bool)AW)
		{
			AW.IsHooked = false;
			AW = null;
		}
	}

	public override void OnStart(StartState state)
	{
		enabled = true;
		part.force_activate();
		if ((bool)part.FindModelTransform("HookTransform"))
		{
			HookTransform = part.FindModelTransform("HookTransform");
		}
		else
		{
			Debug.Log("Missing HookTransform");
		}
		if (deployAnimName != "")
		{
			deployStates = SetUpAnimation(deployAnimName, part);
			AnimationState[] array = deployStates;
			foreach (AnimationState animationState in array)
			{
				animationState.enabled = false;
				animationState.speed = 0f;
				animationState.normalizedTime = 0f;
			}
		}
	}

	public override void OnFixedUpdate()
	{
		if (IsDeploy)
		{
			if (IsHooked)
			{
				TargetDA = 15f;
				if (ARP1Transform != null && ARP2Transform != null)
				{
					LineRenderer component = part.gameObject.GetComponent<LineRenderer>();
					Vector3 vector = ARP1Transform.position - HookTransform.position;
					Vector3 vector2 = ARP2Transform.position - HookTransform.position;
					Vector3 vector3 = AW.part.vessel.GetSrfVelocity() - part.vessel.GetSrfVelocity();
					Vector3 vector4 = Vector3.Normalize(vector3);
					if (Vector3.Dot(vector3, vector + vector2) < 0f)
					{
						wiretension = 0.5f;
					}
					else
					{
						wiretension = maxwiretension;
					}
					part.AddForce(vector4 * Mathf.Min(wiretension * Vector3.Magnitude(vector + vector2) * AW.ForceRatio, (float)vessel.totalMass * 42f * AW.ForceRatio));
					if (!component)
					{
						component = part.gameObject.AddComponent<LineRenderer>();
						Color white = Color.white;
						component.material = new Material(Shader.Find("KSP/Diffuse"));
						component.material.SetColor("_BurnColor", white);
						component.material.mainTexture = GameDatabase.Instance.GetTexture("KFC/Textures/SteelWire", asNormalMap: false);
						component.material.mainTextureScale = Vector2.right;
						component.shadowCastingMode = ShadowCastingMode.Off;
						component.receiveShadows = false;
						component.widthCurve = new AnimationCurve(new Keyframe(0f, 0.1f), new Keyframe(1f, 0.1f));
						component.positionCount = 3;
						component.SetPosition(0, ARP1Transform.position + AW.part.rb.velocity * Time.fixedDeltaTime);
						component.SetPosition(1, HookTransform.position + part.rb.velocity * Time.fixedDeltaTime);
						component.SetPosition(2, ARP2Transform.position + AW.part.rb.velocity * Time.fixedDeltaTime);
						component.material.SetTextureScale("_MainTex", new Vector2((Vector3.Distance(ARP1Transform.position, HookTransform.position) + Vector3.Distance(ARP2Transform.position, HookTransform.position)) * 20f, 1f));
						component.useWorldSpace = true;
						component.enabled = true;
					}
					else
					{
						component.positionCount = 3;
						component.SetPosition(0, ARP1Transform.position + AW.part.rb.velocity * Time.fixedDeltaTime);
						component.SetPosition(1, HookTransform.position + part.rb.velocity * Time.fixedDeltaTime);
						component.SetPosition(2, ARP2Transform.position + AW.part.rb.velocity * Time.fixedDeltaTime);
						component.material.SetTextureScale("_MainTex", new Vector2((Vector3.Distance(ARP1Transform.position, HookTransform.position) + Vector3.Distance(ARP2Transform.position, HookTransform.position)) * 20f, 1f));
						component.enabled = true;
					}
				}
			}
			else
			{
				TargetDA = DeployAngle;
			}
			if (!(deployAnimName != ""))
			{
				return;
			}
			AnimationState[] array = deployStates;
			foreach (AnimationState animationState in array)
			{
				animationState.enabled = true;
				if (animationState.normalizedTime < 0.5f + TargetDA / 180f && animationState.speed < 1f)
				{
					animationState.speed = 1f;
				}
				if (animationState.normalizedTime == 0.5f + TargetDA / 180f)
				{
					animationState.enabled = false;
				}
				if (animationState.normalizedTime > 0.5f + TargetDA / 180f)
				{
					animationState.speed = 0f;
					animationState.normalizedTime = 0.5f + TargetDA / 180f;
					animationState.enabled = false;
				}
				if (animationState.normalizedTime < 0f)
				{
					animationState.speed = 0f;
					animationState.normalizedTime = 0f;
				}
			}
			return;
		}
		AnimationState[] array2 = deployStates;
		foreach (AnimationState animationState2 in array2)
		{
			if (animationState2.normalizedTime > 1f)
			{
				animationState2.speed = 0f;
				animationState2.normalizedTime = 1f;
			}
			if (animationState2.normalizedTime < 0f)
			{
				animationState2.speed = 0f;
				animationState2.normalizedTime = 0f;
			}
			animationState2.enabled = true;
			if (animationState2.normalizedTime > 0f && animationState2.speed > -1f)
			{
				animationState2.speed = -1f;
			}
			if (animationState2.normalizedTime == 0f)
			{
				animationState2.enabled = false;
			}
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (!IsDeploy || IsHooked || !other.gameObject.GetComponentInParent<Part>())
		{
			return;
		}
		Part componentInParent = other.gameObject.GetComponentInParent<Part>();
		if ((bool)componentInParent && (bool)componentInParent.vessel && componentInParent.vessel != vessel && IsWire(componentInParent))
		{
			Debug.Log("Hook Wire!");
			IsHooked = true;
			ARP1Transform = componentInParent.FindModelTransform("ARP1");
			ARP2Transform = componentInParent.FindModelTransform("ARP2");
			LineRenderer component = part.gameObject.GetComponent<LineRenderer>();
			if (!component)
			{
				component = part.gameObject.AddComponent<LineRenderer>();
				Color white = Color.white;
				component.material = new Material(Shader.Find("KSP/Diffuse"));
				component.material.SetColor("_BurnColor", white);
				component.material.mainTexture = GameDatabase.Instance.GetTexture("KFC/Textures/SteelWire", asNormalMap: false);
				component.shadowCastingMode = ShadowCastingMode.Off;
				component.receiveShadows = false;
				component.startWidth = 0.05f;
				component.endWidth = 0.05f;
				component.positionCount = 3;
				component.SetPosition(0, ARP1Transform.position);
				component.SetPosition(1, HookTransform.position);
				component.SetPosition(2, ARP2Transform.position);
				component.material.SetTextureScale("_MainTex", new Vector2((Vector3.Distance(ARP1Transform.position, HookTransform.position) + Vector3.Distance(ARP2Transform.position, HookTransform.position)) * 20f, 1f));
				component.useWorldSpace = true;
				component.enabled = true;
			}
			else
			{
				component.positionCount = 3;
				component.SetPosition(0, ARP1Transform.position);
				component.SetPosition(1, HookTransform.position);
				component.SetPosition(2, ARP2Transform.position);
				component.material.SetTextureScale("_MainTex", new Vector2((Vector3.Distance(ARP1Transform.position, HookTransform.position) + Vector3.Distance(ARP2Transform.position, HookTransform.position)) * 20f, 1f));
				component.enabled = true;
			}
		}
		else if ((bool)componentInParent && (bool)componentInParent.vessel && componentInParent.vessel != vessel && !IsWire(componentInParent))
		{
			IsBlocked = true;
		}
	}

	private bool IsWire(Part IRpart)
	{
		foreach (PartModule module in IRpart.Modules)
		{
			if (module.moduleName == "Arrestwire")
			{
				AW = (Arrestwire)module;
				if (!AW.IsHooked)
				{
					AW.IsHooked = true;
					Debug.Log("Hook");
					return true;
				}
			}
		}
		return false;
	}

	public static AnimationState[] SetUpAnimation(string animationName, Part part)
	{
		List<AnimationState> list = new List<AnimationState>();
		Animation[] array = part.FindModelAnimators(animationName);
		foreach (Animation animation in array)
		{
			AnimationState animationState = animation[animationName];
			animationState.speed = 0f;
			animationState.enabled = true;
			animationState.wrapMode = WrapMode.ClampForever;
			animation.Blend(animationName);
			list.Add(animationState);
		}
		return list.ToArray();
	}
}
