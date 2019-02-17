using UnityEngine;

public class AICarParams : MonoBehaviour
{
    [Header("Acceleration")]
    public float MaxAccel;
    public float MaxVelocity;
    public AnimationCurve AccelPerVelocity;
    public float AccelUnderestimate = 1.0f;

    [Header("Braking")]
    public float MaxBrake;
    public AnimationCurve BrakePerVelocity;
    public float BrakeUnderestimate = 1.0f;

    [Header("Physics")]
    public float GravityAccel = 9.8f;

    public float GetAccel(float velocity, float slope)
    {
        // Get acceleration for given velocity
        float engineAccel = AccelPerVelocity.Evaluate(Mathf.Clamp01(velocity / MaxVelocity)) * MaxAccel;

        // Calculate acceleration based on gravity and slope of road.
        // Note slope = sin(angle), where angle = 0 for flat road, 90 vertical etc.
        float gravityAccel = GravityAccel * -slope;
                
        return engineAccel + gravityAccel - AccelUnderestimate;
    }

    public float GetBrake(float velocity, float slope)
    {
        // Get brake acceleration for given velocity
        float brakeAccel = BrakePerVelocity.Evaluate(Mathf.Clamp01(velocity / MaxVelocity)) * MaxBrake;

        // Calculate acceleration based on gravity and slope of road.
        float gravityAccel = GravityAccel * -slope;

        return brakeAccel - gravityAccel - BrakeUnderestimate;
    }
}
