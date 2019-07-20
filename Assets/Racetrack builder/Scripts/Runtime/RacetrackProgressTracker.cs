using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// Detects the progress of an object (e.g. the player's car) around a Racetrack.
/// Can detect when object has fallen off the track and respawn them.
/// Also collects lap time information.
/// Must be added to the object containing the rigid body to track.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RacetrackProgressTracker : MonoBehaviour
{
    // Components
    private Rigidbody carBody;

    [Header("Parameters")]
    public int CurveSearchAhead = 2;            // # of curves to search ahead when checking whether player has progressed to the next curve. Does not count jumps (which are skipped)
    public float OffRoadTimeout = 10.0f;        // # of seconds player is off the road before they will be placed back on.
    public bool AutoReset = true;               // Enables the auto-respawn logic

    public bool isTimerRunning = true;

    [Header("Working")]
    public int currentCurve = 0;
    public int lapCount = 0;
    public float offRoadTimer = 0.0f;           // # of seconds since the player was last on the road.
    public bool isAboveRoad = false;

    [Header("Lap times")]
    public float LastLapTime = 0.0f;
    public float BestLapTime = 0.0f;
    public float CurrentLapTime = 0.0f;

    [Header("Misc")]
    public Vector3 RayOffset = Vector3.zero;

    void Start()
    {
        carBody = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        var road = Racetrack.Instance;
        if (road == null || road.CurveInfos == null || !road.CurveInfos.Any()) return;

        // Search ahead to determine whether car is above the road, and which curve
        isAboveRoad = false;

        int curveIndex = currentCurve;
        for (int i = 0; i < CurveSearchAhead; i++)
        {
            // Ray cast from center of car in opposite direction to curve
            var ray = new Ray(carBody.transform.TransformPoint(RayOffset), -road.CurveInfos[curveIndex].Normal);
            var hit = Physics.RaycastAll(ray)
                .OrderBy(h => h.distance)
                .Select(h => h.transform.GetComponent<RacetrackSurface>())
                .FirstOrDefault(c => c != null);
            if (hit != null)
            {
                // RoadMeshInfo component indicates we've hit the road.
                var roadInfo = hit.transform.GetComponent<RacetrackSurface>();
                if (roadInfo != null && roadInfo.ContainsCurveIndex(curveIndex))
                {
                    if (curveIndex < currentCurve)
                        LapCompleted();
                    currentCurve = curveIndex;
                    isAboveRoad = true;
                }
            }

            // Find next non-jump curve to check
            int prevCurveIndex = curveIndex;            // (Detect if we loop all the way around)
            do
            {
                curveIndex = (curveIndex + 1) % road.CurveInfos.Length;
            } while (curveIndex != prevCurveIndex && road.CurveInfos[curveIndex].IsJump);
        }

        // Off-road timer logic
        if (isTimerRunning)
        {
            if (isAboveRoad || !AutoReset)
            {
                offRoadTimer = 0.0f;
            }
            else
            {
                offRoadTimer += Time.fixedDeltaTime;
                if (offRoadTimer > OffRoadTimeout)
                    StartCoroutine(PutCarOnRoadCoroutine());
            }
        }

        // Update lap timer
        CurrentLapTime += Time.fixedDeltaTime;
    }

    /// <summary>
    /// Coroutine to put the car back on the road.
    /// Default implementation puts the car there instantly, but could be overridden in a 
    /// subclass to perform an animation.
    /// </summary>
    public virtual IEnumerator PutCarOnRoadCoroutine()
    {
        return RacetrackCoroutineUtil.Do(() => PutCarOnRoad());
    }

    /// <summary>
    /// Place the player car back on the road.
    /// Player is positioned above the last curve that they drove on that is flagged as "CanRespawn"
    /// </summary>
    public void PutCarOnRoad()
    {
        // Find racetrack and curve runtime information
        var track = Racetrack.Instance;
        if (track == null)
        {
            Debug.LogError("Racetrack instance not found. Cannot place car on track.");
            return;
        }
        var curveInfos = track.CurveInfos;
        if (curveInfos == null)
        {
            Debug.LogError("Racetrack curves have not been generated. Cannot place car on track.");
            return;
        }
        if (curveInfos.Length == 0)
        {
            Debug.LogError("Racetrack has no curves. Cannot place car on track.");
            return;
        }

        if (currentCurve < 0) currentCurve = 0;
        if (currentCurve >= curveInfos.Length) currentCurve = curveInfos.Length - 1;

        // Search backwards from current curve for a respawnable curve. Don't go back past 
        // the start of the track though (otherwise player could clock up an extra lap).
        int curveIndex = currentCurve;
        while (curveIndex > 0 && !curveInfos[curveIndex].CanRespawn)
            curveIndex--;

        // Position player at spawn point.
        // Spawn point is in track space, so must transform to get world space.
        var curveInfo = curveInfos[curveIndex];
        transform.position = track.transform.TransformPoint(curveInfo.RespawnPosition);
        transform.rotation = track.transform.rotation * curveInfo.RespawnRotation;

        // Kill all linear and angular velocity
        if (carBody != null)
        {
            carBody.velocity = Vector3.zero;
            carBody.angularVelocity = Vector3.zero;
        }

        // Reset state
        offRoadTimer = 0.0f;
        currentCurve = curveIndex;
    }

    /// <summary>
    /// Update state after lap completed
    /// </summary>
    private void LapCompleted()
    {
        lapCount++;

        // Update lap times
        LastLapTime = CurrentLapTime;
        CurrentLapTime = 0.0f;
        if (BestLapTime == 0.0f || LastLapTime < BestLapTime)
            BestLapTime = LastLapTime;
    }
}
