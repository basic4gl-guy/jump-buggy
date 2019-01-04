using System.Linq;
using UnityEngine;

/// <summary>
/// Tracks the player's progress along the track
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RoadProgressTracker : MonoBehaviour {

    // Components
    private Rigidbody carBody;

    [Header("Parameters")]
    public int CurveSearchAhead = 2;            // # of curves to search ahead when checking whether player has progressed to the next curve. Does not count jumps (which are skipped)
    public float OffRoadTimeout = 10.0f;        // # of seconds player is off the road before they will be placed back on.

    // Working
    [Header("Working")]
    public int currentCurve = 0;
    public int lapCount = 0;
    public float offRoadTimer = 0.0f;

    [Header("Lap times")]
    public float LastLapTime = 0.0f;
    public float BestLapTime = 0.0f;
    public float CurrentLapTime = 0.0f;

	void Start () {
        carBody = GetComponent<Rigidbody>();
	}
	
	void FixedUpdate () {

        var road = CurveBasedRoad.Instance;
        if (road == null || road.CurveInfos == null || !road.CurveInfos.Any()) return;
               
        // Search ahead to determine whether car is above the road, and which curve
        bool isAboveRoad = false;

        int curveIndex = currentCurve;
        for (int i = 0; i < CurveSearchAhead; i++)
        {
            // Ray cast from center of car in opposite direction to curve
            var ray = new Ray(carBody.transform.position, -road.CurveInfos[curveIndex].Normal);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                // RoadMeshInfo component indicates we've hit the road.
                var roadInfo = hit.transform.GetComponent<RoadMeshInfo>();
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
            } while (curveIndex != prevCurveIndex && road.Curves[curveIndex].IsJump);
        }

        // Off-road timer logic
        if (isAboveRoad)
        {
            offRoadTimer = 0.0f;
        }
        else
        {
            offRoadTimer += Time.fixedDeltaTime;
            if (offRoadTimer > OffRoadTimeout)
                PutCarOnRoad();
        }

        // Update lap timer
        CurrentLapTime += Time.fixedDeltaTime;
	}

    private void PutCarOnRoad()
    {
        var road = CurveBasedRoad.Instance;

        // Search backwards from current curve for a respawnable curve. Don't go back past 
        // the start of the track though (otherwise player could clock up an extra lap).
        int curveIndex = currentCurve;
        while (curveIndex > 0 && !road.Curves[curveIndex].CanSpawnPlayer)
            curveIndex--;

        // Position player at spawn point
        var curveInfo = road.CurveInfos[curveIndex];
        transform.position = curveInfo.RespawnPosition;
        transform.rotation = curveInfo.RespawnRotation;
        
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

    private void LapCompleted()
    {
        lapCount++;

        // Update lap times
        LastLapTime = CurrentLapTime;
        CurrentLapTime = 0.0f;
        if (BestLapTime == 0.0f || CurrentLapTime < BestLapTime)
            BestLapTime = CurrentLapTime;
    }
}
