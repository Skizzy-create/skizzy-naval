using System.Collections.Generic;
using UnityEngine;

public class ACE : PartModule
{
	[KSPField]
	public float maxlength = 90f;

	public float TargetDA = 0f;

	public float torque = 10000f;

	public float BlockA = 0f;

	public float maxgee = 50f;

	public bool engineIgnited = true;

	private PidController grabPid = new PidController(10f, 0.005f, 0.005f, 50, 5f);

	private List<KSPParticleEmitter> acEmitters = new List<KSPParticleEmitter>();

	public string Liftcoefficient = "0,0;0.5,1;1,0";

	protected FloatCurve lccurve = new FloatCurve();

	[KSPField(guiActive = true, guiActiveEditor = true, guiName = "Target Velocity", isPersistant = true)]
	[UI_FloatRange(minValue = 0f, maxValue = 3f, stepIncrement = 1f)]
	public float Overload = 1f;

	[KSPAction("Toggle Engine", KSPActionGroup.None)]
	private void ActionActivate(KSPActionParam param)
	{
		if (engineIgnited)
		{
			Deactivate();
		}
		else
		{
			Activate();
		}
	}

	[KSPEvent(name = "Activate", guiName = "Activate Engine", active = true, guiActive = true)]
	public void Activate()
	{
		engineIgnited = true;
		Events["Deactivate"].guiActive = true;
		Events["Activate"].guiActive = false;
	}

	[KSPEvent(name = "Deactivate", guiName = "Deactivate Engine", active = true, guiActive = false)]
	public void Deactivate()
	{
		engineIgnited = false;
		Events["Deactivate"].guiActive = false;
		Events["Activate"].guiActive = true;
	}

	public override void OnStart(StartState state)
	{
		enabled = true;
		part.force_activate();
		vessel.SetReferenceTransform(part);
		Transform[] array = part.FindModelTransforms("aceTransform");
		foreach (Transform transform in array)
		{
			KSPParticleEmitter component = transform.gameObject.GetComponent<KSPParticleEmitter>();
			acEmitters.Add(component);
		}
	}

	public override void OnFixedUpdate()
	{
		if (engineIgnited)
		{
			Activate();
			float pitch = vessel.ctrlState.pitch;
			float roll = vessel.ctrlState.roll;
			float yaw = vessel.ctrlState.yaw;
			float num = (0f - vessel.ctrlState.Y) * Overload * 10f;
			float num2 = (0f - vessel.ctrlState.X) * Overload * 10f;
			float num3 = (0f - vessel.ctrlState.Z) * Overload * 10f;
			Vector3 vector = FlightGlobals.getGeeForceAtPosition(transform.position);
			Vector3 vector2 = (vessel.ActionGroups.groups[5] ? ((-vessel.GetSrfVelocity()).normalized * Mathf.Min(vessel.GetSrfVelocity().magnitude / Time.fixedDeltaTime, Overload * 10f) - vector * 0.5f) : Vector3.zero);
			Vector3 vector3 = vessel.ReferenceTransform.up * num3 + vessel.ReferenceTransform.forward * num + vessel.ReferenceTransform.right * num2 + vector2;
			Vector3 vector4 = (-vessel.transform.right * pitch - vessel.transform.up * roll - vessel.transform.forward * yaw) * torque;
			part.AddForce(vector3 * part.rb.mass);
			part.AddTorque(vector4 * part.rb.mass);
			if (pitch != 0f || roll != 0f || yaw != 0f || num != 0f || num2 != 0f || num3 != 0f)
			{
				foreach (KSPParticleEmitter acEmitter in acEmitters)
				{
					acEmitter.emit = true;
					acEmitter.maxEmission = (int)(5000f * Mathf.Max(Mathf.Abs(pitch * 10f), Mathf.Abs(roll * 10f), Mathf.Abs(yaw * 10f), Mathf.Abs(num2), Mathf.Abs(num), Mathf.Abs(num3), Mathf.Abs(vector2.magnitude)) / 10f);
					acEmitter.minEmission = (int)(4000f * Mathf.Max(Mathf.Abs(pitch * 10f), Mathf.Abs(roll * 10f), Mathf.Abs(yaw * 10f), Mathf.Abs(num2), Mathf.Abs(num), Mathf.Abs(num3), Mathf.Abs(vector2.magnitude)) / 10f);
				}
			}
			else
			{
				foreach (KSPParticleEmitter acEmitter2 in acEmitters)
				{
					acEmitter2.emit = false;
				}
			}
		}
		else
		{
			Deactivate();
			foreach (KSPParticleEmitter acEmitter3 in acEmitters)
			{
				acEmitter3.emit = false;
			}
		}
		lccurve = stringToFloatCurve(Liftcoefficient);
		Vector3 srfVelocity = vessel.GetSrfVelocity();
		Vector3 vector5 = part.transform.InverseTransformDirection(srfVelocity);
		Vector3 position = part.partTransform.position;
		double externalTemperature = FlightGlobals.getExternalTemperature(position, vessel.mainBody);
		double staticPressure = FlightGlobals.getStaticPressure(position);
		double atmDensity = FlightGlobals.getAtmDensity(staticPressure, externalTemperature, vessel.mainBody);
		Vector3 vector6 = (float)atmDensity * vector5.x * part.rb.mass * -vessel.transform.right;
		if ((double)vector6.magnitude > vessel.totalMass * (double)maxgee)
		{
			vector6 *= (float)(vessel.totalMass * (double)maxgee / (double)vector6.magnitude);
		}
		Vector3 vector7 = (float)atmDensity * vector5.z * part.rb.mass * -vessel.transform.forward;
		if ((double)vector7.magnitude > vessel.totalMass * (double)maxgee)
		{
			vector7 *= (float)(vessel.totalMass * (double)maxgee / (double)vector7.magnitude);
		}
		part.AddForce(vector6);
		part.AddForce(vector7);
	}

	public static FloatCurve stringToFloatCurve(string curveString)
	{
		FloatCurve floatCurve = new FloatCurve();
		string[] array = curveString.Split(';');
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split(',');
			if (array2.Length >= 2)
			{
				Vector4 zero = Vector4.zero;
				float.TryParse(array2[0], out zero.x);
				float.TryParse(array2[1], out zero.y);
				if (array2.Length == 4)
				{
					float.TryParse(array2[2], out zero.z);
					float.TryParse(array2[3], out zero.w);
				}
				floatCurve.Add(zero.x, zero.y, zero.z, zero.w);
			}
		}
		return floatCurve;
	}
}
