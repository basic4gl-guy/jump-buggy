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
}

public class CarState
{
    public bool IsAboveRoad { get; internal set; }

    // Remaining properties are only valid if IsAboveRoad is true.
    
    public Racetrack Track { get; internal set; }
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

    public float DistanceDownTrack
    {
        get { return SegmentIndex * Track.SegmentLength + Position.z; }
    }

    /// <summary>
    /// Get position of other car relative to this one
    /// </summary>
    public Vector3 GetRelativePosition(CarState other)
    {
        float relDist = other.DistanceDownTrack - this.DistanceDownTrack;
        float trackLength = Track.GetSegments().Count * Track.SegmentLength;
        if (relDist < -trackLength / 2)
            relDist += trackLength;
        else if (relDist > trackLength / 2)
            relDist -= trackLength;

        return new Vector3(other.Position.x - Position.x, other.Position.y - Position.y, relDist);
    }
}
