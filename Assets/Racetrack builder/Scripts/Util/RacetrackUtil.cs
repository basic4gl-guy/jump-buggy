using UnityEngine;

/// <summary>
/// Misc helper functions
/// </summary>
public static class RacetrackUtil {

    /// <summary>
    /// Returns the corresponding angle in the -180 to 180 degree range.
    /// </summary>
    /// <param name="angle">Angle in degrees</param>
    public static float LocalAngle(float angle)
    {
        angle -= Mathf.Floor(angle / 360.0f) * 360.0f;
        if (angle > 180.0f)
            angle -= 360.0f;
        return angle;
    }

    public static Vector3 ToVector3(Vector4 v)
    {
        return new Vector3(v.x, v.y, v.z);
    }

    public static Vector4 ToVector4(Vector3 v, float w = 0.0f)
    {
        return new Vector4(v.x, v.y, v.z, w);
    }

    public static CarState GetCarState(Rigidbody car, Racetrack track, int curveIndex)
    {
        var state = new CarState();
        GetCarState(car, track, curveIndex, state);
        return state;
    }

    public static void GetCarState(Rigidbody car, Racetrack track, int curveIndex, CarState state)
    {
        // Get curve information
        var curves = track.Curves;
        var infos = track.CurveInfos;
        if (curveIndex < 0 || curveIndex >= curves.Count)
        {
            Debug.LogError("Curve index " + curveIndex + " out of range. Must be 0 - " + (curves.Count - 1));
        }
        var curve = curves[curveIndex];
        var info = infos[curveIndex];
        track.GetSegments();            // (Ensures segments are generated)

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
            Matrix4x4 trackFromMidSeg = midSeg.GetSegmentToTrack();
            Vector3 segForward = trackFromMidSeg.MultiplyVector(Vector3.forward);
            var dp = Vector3.Dot(carPosTrack - midSeg.Position, segForward);
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
    public RacetrackCurve Curve { get; internal set; }
    public Racetrack.CurveRuntimeInfo Info { get; internal set; }
    public Racetrack.Segment Segment { get; internal set; }
    public int SegmentIndex { get; internal set; }
    public Vector3 Position { get; internal set; }
    public Vector3 Direction { get; internal set; }
    public Vector3 Velocity { get; internal set; }
    public float Angle { get; internal set; }
    public Matrix4x4 TrackFromSeg { get; internal set; }
    public Matrix4x4 SegFromTrack { get; internal set; }
}
