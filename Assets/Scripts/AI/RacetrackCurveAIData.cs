using UnityEngine;

[RequireComponent(typeof(RacetrackCurve))]
public class RacetrackCurveAIData : MonoBehaviour
{
    [Tooltip("Maximum speed in meters per second, at the END of the curve. 0 => no limit")]
    public float MaxSpeed;

    [Tooltip("Minimum speed in meters per second, at the END of the curve. 0 => no limit")]
    public float MinSpeed;
}
