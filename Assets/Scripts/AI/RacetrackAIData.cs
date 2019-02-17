using System.Linq;
using UnityEngine;

public class RacetrackAIData : MonoBehaviour
{
    public Racetrack Racetrack;
    public AICarParams AICarParams;

    private SegmentAIData[] segmentDatas;

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

        // Build initial segment AI info. Set min/max speed limits when:
        //  * When specified explicitly
        //  * To complete jumps (min)
        //  * To prevent flying off road (max)
        //  * To turn corners successfully (max)
        //  * To prevent sliding down banked corners (min)
        segmentDatas = new SegmentAIData[segments.Count];
        int ci = 0;
        float curveEndZ = curveInfos[ci].zOffset + curves[ci].Length;
        for (int i = 0; i < segmentDatas.Length; i++)
        {
            // Defaults
            float min = 0.0f;
            float max = 1000.0f;

            // Apply limits on last segment of curve
            float segZ = i * Racetrack.SegmentLength;
            if (segZ + Racetrack.SegmentLength > curveEndZ)
            {
                // Look for AI data
                var aiData = ci < curves.Count ? curves[ci].GetComponent<RacetrackCurveAIData>() : null;
                if (aiData != null)
                {
                    if (aiData.MaxSpeed != 0.0f)
                        max = aiData.MaxSpeed;
                    if (aiData.MinSpeed != 0.0f)
                        min = aiData.MinSpeed;
                }

                // Move on to next curve
                ci++;
                //curveEndZ = ci < curves.Count
                //    ? curveInfos[ci].zOffset + curves[ci].Length
                //    : segments.Count * Racetrack.SegmentLength;
                if (ci < curves.Count)
                    curveEndZ = curveInfos[ci].zOffset + curves[ci].Length;
                else
                    curveEndZ = segments.Count * Racetrack.SegmentLength;
            }

            segmentDatas[i] = new SegmentAIData { MinSpeed = min, MaxSpeed = max };
        }

        // Propagate speed limits backwards, based on acceleration and braking limitations
        for (int pass = 0; pass < 2; pass++)
        {
            ci = curves.Count - 1;
            for (int i = segmentDatas.Length - 1; i >= 0; i--)
            {
                // Get segment data
                var segment = segments[i];
                var segData = segmentDatas[i];
                var nextSegData = segmentDatas[(i + 1) % segmentDatas.Length];

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
                if (nextSegData.MaxSpeed < 1000.0f)
                {
                    // Next segment has a maximum speed.
                    // Calculate this segment's maximum speed which would allow car to brake down to next seg's max speed
                    float a = -AICarParams.GetBrake(nextSegData.MaxSpeed, gradient);
                    float d = Racetrack.SegmentLength;
                    float v1 = nextSegData.MaxSpeed;
                    float sqrtTerm = v1*v1 - 2*a*d;
                    if (sqrtTerm >= 0.0f)
                    { 
                        float v0 = Mathf.Sqrt(sqrtTerm);
                        if (v0 < segData.MaxSpeed)
                            segData.MaxSpeed = v0;
                    }
                }

                {
                    // Next segment has a minimum speed.
                    // Calculate this segment's minimum speed which would allow car to accelerate up to next seg's min speed
                    float a = AICarParams.GetAccel(nextSegData.MinSpeed, gradient);
                    float d = Racetrack.SegmentLength;
                    float v1 = nextSegData.MinSpeed;
                    float sqrtTerm = v1*v1 - 2*a*d;
                    if (sqrtTerm >= 0.0f)
                    {
                        float v0 = Mathf.Sqrt(sqrtTerm);
                        if (v0 > segData.MinSpeed)
                            segData.MinSpeed = v0;
                    }
                }
            }
        }
    }

    public SegmentAIData GetAIData(int index)
    {
        if (segmentDatas == null || index < 0 || index >= segmentDatas.Length)
            return new SegmentAIData { MinSpeed = 0.0f, MaxSpeed = 1000.0f };
        else
            return segmentDatas[index];
    }

    public class SegmentAIData
    {
        public float MinSpeed;
        public float MaxSpeed;
    }
}
