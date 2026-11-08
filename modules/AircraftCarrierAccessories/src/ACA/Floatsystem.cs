using System;
using UnityEngine;

public class Floatsystem : PartModule
{
	[KSPField]
	public float hoverForce = 0f;

	[KSPField]
	public float Draftoffset = 5f;

	[KSPField]
	public float maxav = 1f;

	[KSPField]
	private bool anchored = false;

	[KSPField(isPersistant = false)]
	public float Stability = 1f;

	[KSPField(isPersistant = false)]
	public float yaws = 1f;

	[KSPField(isPersistant = false)]
	public float yawc = 5f;

	protected Transform DraftTransform = null;

	protected Transform rudderTransform = null;

	public override void OnStart(StartState state)
	{
		base.OnStart(state);
		if (state != StartState.Editor && state != StartState.None)
		{
			enabled = true;
			part.force_activate();
		}
		else
		{
			enabled = false;
		}
		if (part.FindModelTransform("Draft") != null)
		{
			DraftTransform = part.FindModelTransform("Draft");
		}
		if (part.FindModelTransform("rudder") != null)
		{
			rudderTransform = part.FindModelTransform("rudder");
		}
	}

	public override void OnFixedUpdate()
	{
		if (HighLogic.LoadedSceneIsFlight && vessel.mainBody.ocean)
		{
			float pitch = vessel.ctrlState.pitch;
			float roll = vessel.ctrlState.roll;
			float yaw = vessel.ctrlState.yaw;
			Vector3 srfVelocity = vessel.GetSrfVelocity();
			float num = (float)vessel.verticalSpeed;
			Vector3 vector = part.transform.InverseTransformDirection(srfVelocity);
			Vector3 vector2 = FlightGlobals.getGeeForceAtPosition(transform.position);
			float altitudeAtPos = FlightGlobals.getAltitudeAtPos(DraftTransform.position);
			Vector3 vector3 = Vector3.SqrMagnitude(vector) * vector.x * Math.Abs(vector.y) * yaws * -vessel.transform.right;
			rudderTransform.localEulerAngles = Vector3.down * yaw * 10f;
			Vector3 vector4 = DraftTransform.InverseTransformDirection(vector2);
			Vector3 vector5 = vessel.transform.up * Stability * vector4.x * (float)vessel.totalMass;
			Vector3 vector6 = -vessel.transform.right * 5f * Stability * vector4.y * (float)vessel.totalMass;
			Vector3 vector7 = (float)vessel.totalMass * vector2 * (num + altitudeAtPos);
			if (altitudeAtPos < 3f)
			{
				part.AddTorque(vector5);
				part.AddTorque(vector6);
				part.AddForceAtPosition(vector3, vessel.transform.position);
				part.AddForce(vector7);
				part.AddTorque(-part.transform.forward * (float)vessel.totalMass * yaw * yawc * Vector3.SqrMagnitude(vector));
			}
		}
	}
}
