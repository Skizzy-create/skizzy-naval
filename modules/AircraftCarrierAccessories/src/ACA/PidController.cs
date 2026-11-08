using UnityEngine;

internal class PidController
{
	private float mKp;

	private float mKd;

	private float mKi;

	private float mOldVal;

	private float mOldTime;

	private float mOldD;

	private float mClamp;

	private float[] mBuffer = null;

	private int mPtr;

	private float mSum;

	private float mValue;

	public float value => mValue;

	public PidController(float Kp, float Ki, float Kd, int integrationBuffer, float clamp)
	{
		mKp = Kp;
		mKi = Ki;
		mKd = Kd;
		mClamp = clamp;
		if (integrationBuffer >= 1)
		{
			mBuffer = new float[integrationBuffer];
		}
		Reset();
	}

	public void Reset()
	{
		mSum = 0f;
		mOldTime = -1f;
		mOldD = 0f;
		if (mBuffer != null)
		{
			for (int i = 0; i < mBuffer.Length; i++)
			{
				mBuffer[i] = 0f;
			}
		}
		mPtr = 0;
	}

	public float Control(float v)
	{
		if (Time.fixedTime > mOldTime)
		{
			if (mOldTime >= 0f)
			{
				mOldD = (v - mOldVal) / (Time.fixedTime - mOldTime);
				float num = v / (Time.fixedTime - mOldTime);
				if (mBuffer != null)
				{
					mSum -= mBuffer[mPtr];
					mBuffer[mPtr] = num;
					mPtr++;
					if (mPtr >= mBuffer.Length)
					{
						mPtr = 0;
					}
				}
				mSum += num;
			}
			mOldTime = Time.fixedTime;
			mOldVal = value;
		}
		mValue = mKp * v + mKi * mSum + mKd * mOldD;
		if (mClamp > 0f)
		{
			if (mValue > mClamp)
			{
				mValue = mClamp;
			}
			if (mValue < 0f - mClamp)
			{
				mValue = 0f - mClamp;
			}
		}
		return mValue;
	}

	public static implicit operator float(PidController v)
	{
		return v.mValue;
	}

	private float step(float value, bool up)
	{
		float num = Mathf.Log10(value);
		if (num < 0f)
		{
			num--;
		}
		num = (int)num;
		float num2 = Mathf.Pow(10f, num);
		float num3 = Mathf.Round(value / num2);
		if (up)
		{
			num3 = ((!(num3 >= 10f)) ? (num3 + 1f) : (num3 + 10f));
		}
		else
		{
			num3 = ((!(num3 <= 1f)) ? (num3 - 1f) : (num3 - 0.1f));
		}
		return num3 * num2;
	}

	public void calibrate()
	{
		if (Input.GetKeyDown("[7]"))
		{
			mKp = step(mKp, up: false);
			Debug.Log("Kp: " + mKp);
		}
		if (Input.GetKeyDown("[9]"))
		{
			mKp = step(mKp, up: true);
			Debug.Log("Kp: " + mKp);
		}
		if (Input.GetKeyDown("[4]"))
		{
			mKi = step(mKi, up: false);
			Debug.Log("Ki: " + mKi);
		}
		if (Input.GetKeyDown("[6]"))
		{
			mKi = step(mKi, up: true);
			Debug.Log("Ki: " + mKi);
		}
		if (Input.GetKeyDown("[1]"))
		{
			mKd = step(mKd, up: false);
			Debug.Log("Kd: " + mKd);
		}
		if (Input.GetKeyDown("[3]"))
		{
			mKd = step(mKd, up: true);
			Debug.Log("Kd: " + mKd);
		}
	}

	public void calibrateclamp(float newclamp)
	{
		mClamp = newclamp;
	}
}
