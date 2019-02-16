using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AICarParams : MonoBehaviour
{
    public float MaxAccel;
    public float MaxVelocity;
    public float MaxBrake;
    public AnimationCurve AccelPerVelocity;
    public AnimationCurve AccelPerSlope;
    public AnimationCurve BrakePerSlope;

    public float GetAccel(float velocity, float gradient)
    {
        // TODO
        return MaxAccel * 0.5f;
    }

    public float GetBrake(float velocity, float gradient)
    {
        // TODOS
        return MaxBrake * 0.5f;
    }
}
