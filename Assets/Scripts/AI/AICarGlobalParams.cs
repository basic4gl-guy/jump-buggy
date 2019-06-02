using UnityEngine;

/// <summary>
/// Global AI parameters shared across all cars.
/// Used when building RacetrackAIData
/// </summary>
public class AICarGlobalParams : MonoBehaviour
{
    [Header("Acceleration")]
    public float MaxAccel;
    public float MaxVelocity;
    public AnimationCurve AccelPerVelocity;
    public float AccelFactor = 0.8f;

    [Header("Braking")]
    public float MaxBrake;
    public AnimationCurve BrakePerVelocity;
    public float BrakeFactor = 0.9f;

    [Header("Physics")]
    public float GravityAccel = 9.8f;
    public float FrictionCoefficient = 1.0f;

    [Tooltip("Jump minimum speeds are multiplied by this factor, to compensate for drag")]
    public float JumpMinFactor = 1.5f;

    [Tooltip("Jump maximum speeds are multiplied by this factor")]
    public float JumpMaxFactor = 2.0f;
    
    public float StayOnRoadFactor = 1.0f;

    public float GetAccel(float velocity, float slope)
    {
        // Get acceleration for given velocity
        float engineAccel = AccelPerVelocity.Evaluate(Mathf.Clamp01(velocity / MaxVelocity)) * MaxAccel;

        // Calculate acceleration based on gravity and slope of road.
        // Note slope = sin(angle), where angle = 0 for flat road, 90 vertical etc.
        float gravityAccel = GravityAccel * -slope;

        float friction = Mathf.Sqrt(Mathf.Max(1.0f - slope * slope, 0.0f));                
        return engineAccel * AccelFactor * friction + gravityAccel;
    }

    public float GetBrake(float velocity, float slope)
    {
        // Get brake acceleration for given velocity
        float brakeAccel = BrakePerVelocity.Evaluate(Mathf.Clamp01(velocity / MaxVelocity)) * MaxBrake;

        // Calculate acceleration based on gravity and slope of road.
        float gravityAccel = GravityAccel * -slope;

        return brakeAccel * BrakeFactor - gravityAccel;
    }
}
