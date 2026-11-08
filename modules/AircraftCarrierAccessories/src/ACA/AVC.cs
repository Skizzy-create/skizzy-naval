using System;
using UnityEngine;

public class AVC : PartModule
{
	[KSPField]
	public float yaws = 1f;

	[KSPField]
	public float pitchs = 1f;

	[KSPField]
	public float fuelefficiency = 0.002f;

	[KSPField]
	public string fuelType = "AntiProton";

	[KSPField(guiActive = true, guiActiveEditor = true, guiName = "Max Overload", isPersistant = true)]
	[UI_FloatRange(minValue = 0f, maxValue = 5f, stepIncrement = 0.1f)]
	public float Overload = 1f;

	public override void OnFixedUpdate()
	{
		if (HighLogic.LoadedSceneIsFlight && vessel.isActiveVessel)
		{
			float pitch = vessel.ctrlState.pitch;
			float roll = vessel.ctrlState.roll;
			float yaw = vessel.ctrlState.yaw;
			float num = (0f - vessel.ctrlState.Y) * Overload * 10f;
			float num2 = (0f - vessel.ctrlState.X) * Overload * 10f;
			float num3 = (0f - vessel.ctrlState.Z) * Overload * 10f;
			Vector3 vector = FlightGlobals.getGeeForceAtPosition(transform.position);
			Vector3 vector2 = (vessel.ActionGroups.groups[5] ? ((-vessel.GetSrfVelocity()).normalized * Mathf.Min(vessel.GetSrfVelocity().magnitude / Time.fixedDeltaTime, Overload * 10f) - vector * 0.9f) : Vector3.zero);
			Vector3 vector3 = vessel.ReferenceTransform.up * num3 + vessel.ReferenceTransform.forward * num + vessel.ReferenceTransform.right * num2 + vector2;
			double num4 = vessel.totalMass * (double)Math.Abs(vector3.magnitude) * (double)fuelefficiency;
			double num5 = num4 * (double)TimeWarp.deltaTime;
			double num6 = part.RequestResource(fuelType, num5);
			if (num6 == 0.0 && num5 > 0.0)
			{
				vector3 = Vector3.zero;
			}
			part.AddForce(vector3 * part.mass);
			Vector3 srfVelocity = vessel.GetSrfVelocity();
			float num7 = (float)vessel.verticalSpeed;
			Vector3 vector4 = part.transform.InverseTransformDirection(srfVelocity);
			double externalTemperature = FlightGlobals.getExternalTemperature(vessel.GetTransform().position, vessel.mainBody);
			double staticPressure = FlightGlobals.getStaticPressure(vessel.GetTransform().position);
			double atmDensity = FlightGlobals.getAtmDensity(staticPressure, externalTemperature, vessel.mainBody);
			Vector3 vector5 = vessel.GetTotalMass() * vector4.x * yaws * -vessel.transform.right * (float)atmDensity;
			Vector3 vector6 = vessel.GetTotalMass() * vector4.z * pitchs * -vessel.transform.forward * (float)atmDensity;
		}
	}
}
