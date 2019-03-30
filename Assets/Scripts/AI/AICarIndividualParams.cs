using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AI parameters for an individual car.
/// Note: These can be placed in a child object, underneath the AICarController, so that the params
/// can be edited and prefabbed individually.
/// </summary>
public class AICarIndividualParams : MonoBehaviour
{
    [Header("Dimensions")]
    public float CarLength = 6.0f;
    public float CarWidth = 2.0f;
    public float RoadWidth = 12.0f;

    [Header("Steering")]
    [Tooltip("When recentering, will calculate the angle such that the car reaches the center in this time (seconds)")]
    public float RecenterTime = 1.0f;
    [Tooltip("Maximum angle in relation to track car can turn in order to recenter (degrees)")]
    public float RecenterAngleRange = 10.0f;
    [Tooltip("Divided by the velocity to determine the maximum steering wheel angle (degrees)")]
    public float SteeringSpeedFactor = 5000.0f;
    public float SteeringRate = 250.0f;
    [Tooltip("Absolute steering wheel limit (degrees)")]
    public float SteeringLimit = 90.0f;
    [Tooltip("Steering wheel smoothing factor (0,1] where 1 = no smoothing")]
    public float SteeringSmooth = 0.1f;
    [Tooltip("Limit the car can stray from the centerline before it attempts to steer back into the center (units)")]
    public float CenterXOffsetLimit = 2.0f;
    [Tooltip("Distance from jump at which to ignore other cars and just straighten with track (units)")]
    public float StraightenForJumpDistance = 50.0f;
    public float SteeringAngleOffset = 0.0f;

    [Header("Speed")]
    [Tooltip("Car will aim for this speed when possible (units/second)")]
    public float PreferredSpeed = 50.0f;
    [Tooltip("How close to the minimum/maximum speed car can get before accelerating/braking (units/second)")]
    public float MinMaxSpeedBuffer = 1.0f;

    [Header("Avoiding other cars")]
    [Tooltip("Estimated catch up time at which car will steer to avoid car in front (seconds)")]
    public float CatchupDurationAvoidLimit = 3.0f;
    [Tooltip("Estimated catch up time at which car will brake to avoid car in front (seconds)")]
    public float CatchupDurationBrakeLimit = 2.0f;
    [Tooltip("Maximum horizontal gap between side of car in front and side of road (units)")]
    public float AvoidGapLimit = 2.5f;
    [Tooltip("Car will brake to avoid a collision if X offset from car in front is less than this limit (units)")]
    public float BrakeXOffsetLimit = 2.25f;
}
