using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(RacetrackProgressTracker))]
public class RacetrackCarState : MonoBehaviour
{
    private Rigidbody car;
    private RacetrackProgressTracker progressTracker;
    private CarState state = new CarState();

    public RacetrackCarStates Track;

    private void Awake()
    {
        car = GetComponent<Rigidbody>();
        progressTracker = GetComponent<RacetrackProgressTracker>();
    }

    // Start is called before the first frame update
    void Start()
    {
        var cars = GetCars();
        if (cars == null) return;

        // Register this car
        cars.RegisterCarState(this);
    }

    /// <summary>
    /// Get the current car's state
    /// </summary>
    public CarState State
    {
        get { return state; }
    }

    /// <summary>
    /// Get the car infront's state
    /// </summary>
    public RacetrackCarState GetNextCarState()
    {
        var cars = GetCars();
        if (cars == null) return null;

        return cars.GetNextCarState(this);
    }

    private Racetrack GetRacetrack()
    {
        // Set explicitly?
        if (Track != null)
            return Track.Track;

        // Otherwise look for component on Racetrack instance
        if (Racetrack.Instance == null)
        {
            Debug.LogError("RacetrackCarState - Could not find the Racetrack");
            return null;
        }

        return Racetrack.Instance;
    }

    /// <summary>
    /// Resolve the RacetrackCarStates object
    /// </summary>
    private RacetrackCarStates GetCars()
    {
        // Set explicitly?
        if (Track != null)
            return Track;

        // Otherwise look for component on Racetrack instance
        if (Racetrack.Instance == null)
        {
            Debug.LogError("RacetrackCarState - Could not find the Racetrack");
            return null;
        }

        var cars = Racetrack.Instance.GetComponent<RacetrackCarStates>();
        if (cars == null)
        {
            Debug.LogError("RacetrackCarState - Racetrack does not have a RacetrackCarStates component");
            return null;
        }

        return cars;
    }

    public void UpdateState(Racetrack track)
    {
        // Get curve index from progress tracker
        State.IsAboveRoad = progressTracker.isAboveRoad;
        if (!State.IsAboveRoad) return;
        int curveIndex = progressTracker.currentCurve;

        // Get curve information
        var curves = track.Curves;
        var infos = track.CurveInfos;
        if (curveIndex < 0 || curveIndex >= curves.Count)
        {
            Debug.LogError("Curve index " + curveIndex + " out of range. Must be 0 - " + (curves.Count - 1));
        }
        var curve = curves[curveIndex];
        var info = infos[curveIndex];

        // Calculate car position and direction in track space
        Matrix4x4 worldFromTrack = track.transform.localToWorldMatrix;
        Matrix4x4 trackFromWorld = worldFromTrack.inverse;
        Vector3 carPosTrack = trackFromWorld.MultiplyPoint(car.position);
        Vector3 carDirTrack = trackFromWorld.MultiplyVector(car.transform.TransformVector(Vector3.forward));
        Vector3 carVelTrack = trackFromWorld.MultiplyVector(car.velocity);

        // Binary search for segment index
        float loZ = info.zOffset;
        float hiZ = info.zOffset + curve.Length;
        int lo = Mathf.FloorToInt(loZ / track.SegmentLength);
        int hi = Mathf.FloorToInt(hiZ / track.SegmentLength);

        // Allow for curve index to be too far ahead.
        // This is because curve index will often be supplied from the RacetrackProgressTracker component, 
        // which can be optimistic sometimes, depending on which curves generate geometry
        {
            float dp;
            int count = 0;
            do
            {
                var loSeg = track.GetSegment(lo);
                dp = Vector3.Dot(carPosTrack - loSeg.Position, loSeg.PositionDelta);
                if (dp < 0.0f)
                {
                    hi = lo;
                    lo = lo - 50;
                    if (lo < 0)
                        lo = 0;
                }
                count++;
            } while (dp < 0.0f && count < 10);
        }

        while (hi > lo)
        {
            int mid = (lo + hi + 1) / 2;
            var midSeg = track.GetSegment(mid);
            var dp = Vector3.Dot(carPosTrack - midSeg.Position, midSeg.PositionDelta);
            if (dp >= 0)
                lo = mid;
            else
                hi = mid - 1;
        }

        // Calculate car position and direction in segment space
        var seg = track.GetSegment(lo);
        Matrix4x4 trackFromSeg = seg.GetSegmentToTrack();
        Matrix4x4 segFromTrack = trackFromSeg.inverse;

        Vector3 carPos = segFromTrack.MultiplyPoint(carPosTrack);
        Vector3 carDir = segFromTrack.MultiplyVector(carDirTrack);
        Vector3 carVel = segFromTrack.MultiplyVector(carVelTrack);
        float carAng = Mathf.Atan2(carDir.x, carDir.z) * Mathf.Rad2Deg;

        state.Track = track;
        state.Curve = curve;
        state.Info = info;
        state.Segment = seg;
        state.SegmentIndex = lo;
        state.Position = carPos;
        state.Direction = carDir;
        state.Velocity = carVel;
        state.Angle = carAng;
        state.TrackFromSeg = trackFromSeg;
        state.SegFromTrack = segFromTrack;
    }
}

public class CarState
{
    public Racetrack Track { get; set; }
    public RacetrackCurve Curve { get; set; }
    public Racetrack.CurveRuntimeInfo Info { get; set; }
    public Racetrack.Segment Segment { get; set; }
    public int SegmentIndex { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Direction { get; set; }
    public Vector3 Velocity { get; set; }
    public float Angle { get; set; }
    public Matrix4x4 TrackFromSeg { get; set; }
    public Matrix4x4 SegFromTrack { get; set; }
    public bool IsAboveRoad { get; set; }

    public float TrackZ
    {
        get { return SegmentIndex * Track.SegmentLength + Position.z; }
    }

    public Vector3 GetRelativePosition(CarState nextState)
    {
        float z = nextState.TrackZ - TrackZ;
        float trackLength = Track.Segments.Count * Track.SegmentLength;
        if (z < -trackLength / 2.0f)
            z += trackLength;
        if (z >= trackLength / 2.0f)
            z -= trackLength;
        return new Vector3(nextState.Position.x - Position.x, nextState.Position.y - Position.y, z);
    }
}