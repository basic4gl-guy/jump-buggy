using System.Linq;
using UnityEngine;

public class RacetrackAIData : MonoBehaviour
{
    public Racetrack Racetrack;
    public AICarParams AICarParams;

    private SegmentAIInfo[] segmentInfos;

    private void Start()
    {
        if (Racetrack == null)
        {
            Debug.LogErrorFormat("RacetrackAIData - Please set the Racetrack field");
            return;
        }

        if (AICarParams == null)
        {
            Debug.LogFormat("RacetrackAIData - Please set the AICarParams field");
            return;
        }

        var curves = Racetrack.Curves;
        var curveInfos = Racetrack.CurveInfos;
        if (!curves.Any())
        {
            Debug.LogFormat("RacetrackAIData - Racetrack has no curves");
            return;
        }

        var segments = Racetrack.GetSegments();
        if (!segments.Any())
        {
            Debug.LogFormat("RacetrackAIData - Racetrack has no segments");
            return;
        }

        // Build initial segment AI info. Essentially no min/max speed limits
        var segmentInfos = new SegmentAIInfo[segments.Count];
        for (int i = 0; i < segmentInfos.Length; i++)
            segmentInfos[i] = new SegmentAIInfo { MinSpeed = 0.0f, MaxSpeed = 1000.0f };

        // Work backwards over length of track.
        // Apply explicit speed limits defined on corners.
        // Propagate limits backwards based on acceleration/deceleration required
        // 2 passes are required, so that limits at the start of the track propagate back to the end of the track.
        for (int pass = 0; pass < 2; pass++)
        {
            int ci = curves.Count - 1;
            bool isLastSegment = true;
            for (int i = segmentInfos.Length - 1; i >= 0; i--)
            {
                // Get segment data
                var segment = segments[i];
                var segInfo = segmentInfos[i];
                var nextSegInfo = segmentInfos[(i + 1) % segmentInfos.Length];

                // Calculate gradient
                Matrix4x4 trackFromSeg = segment.GetSegmentToTrack();
                Vector3 segForward = trackFromSeg.MultiplyVector(Vector3.forward);
                float gradient = segForward.normalized.y;

                // Calculating this segments max/min speed
                // Let: v0 = Max vel at current segment
                //      v1 = Max vel at next segment
                //       d = Distance between segments
                //       t = Time taken to travel from v0 to v1
                //       a = Acceleration (negative if decelerating)
                //
                // 1)       v1 = v0 + at
                // 2)       d = v0t + 1/2at^2
                // 1->2)    d = (v0(v1-v0))/a + 1/2a((v1-v0)/a)^2
                //            = (v0v1-v0^2)/a + a/2((v1^2-2v0v1+v0^2)/a^2)
                //            = (v0v1-v0^2)/a + 1/2((v1^2-2v0v1+v0^2)/a)
                //         ad = v0v1-v0^2+1/2v1^2-v0v1+1/2v0^2
                //            = 1/2v1^2 - 1/2v0^2
                //        2ad = v1^2 - v0^2
                //       v0^2 = v1^2 - 2ad
                //       v0   = sqrt(v1^2 - 2ad)
                if (nextSegInfo.MaxSpeed < 1000.0f)
                {
                    // Next segment has a maximum speed.
                    // Calculate this segment's maximum speed to allow car to decelerate
                    float a = -AICarParams.GetBrake(nextSegInfo.MaxSpeed, gradient);
                    float d = Racetrack.SegmentLength;
                    float v1 = nextSegInfo.MaxSpeed;
                    float v0 = Mathf.Sqrt(v1*v1 - 2*a*d);
                    if (v0 < segInfo.MaxSpeed)
                        segInfo.MaxSpeed = v0;
                }

                if (nextSegInfo.MinSpeed > 0.0f)
                {
                    // Next segment has a maximum speed.
                    // Calculate this segment's maximum speed to allow car to decelerate
                    float a = AICarParams.GetAccel(nextSegInfo.MaxSpeed, gradient);
                    float d = Racetrack.SegmentLength;
                    float v1 = nextSegInfo.MaxSpeed;
                    float v0 = Mathf.Sqrt(v1*v1 - 2*a*d);
                    if (v0 > segInfo.MinSpeed)
                        segInfo.MinSpeed = v0;
                }

                // Look for explicit curve speed limits.
                // Applies only to the last segment of the curve
                if (isLastSegment)
                {
                    var aiData = curves[ci].GetComponent<RacetrackCurveAIData>();
                    if (aiData != null)
                    {
                        if (aiData.MaxSpeed != 0.0f && aiData.MaxSpeed < segInfo.MaxSpeed)
                            segInfo.MaxSpeed = aiData.MaxSpeed;
                        if (aiData.MinSpeed != 0.0f && aiData.MinSpeed > segInfo.MinSpeed)
                            segInfo.MinSpeed = aiData.MinSpeed;
                    }
                }

                // Walk down curve arrays in parallel.
                // Detect when we are on the last segment of the curve.
                isLastSegment = false;
                float zOffset = (i-1) * Racetrack.SegmentLength;
                while (zOffset < curveInfos[ci].zOffset && ci > 0)
                {
                    ci--;
                    isLastSegment = true;
                }                
            }
        }
    }

    public class SegmentAIInfo
    {
        public float MinSpeed;
        public float MaxSpeed;
    }
}
