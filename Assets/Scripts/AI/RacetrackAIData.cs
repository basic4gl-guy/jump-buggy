using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RacetrackAIData : MonoBehaviour
{
    public Racetrack Racetrack;
    public AICarGlobalParams AICarParams;

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
            var seg = segments[i];

            // Defaults
            float min = 0.0f;
            float max = 1000.0f;
            float distToJump = curves[ci].IsJump ? 0.0f : 1000000.0f;

            // Apply limits on last segment of curve
            float segZ = i * Racetrack.SegmentLength;
            if (segZ + Racetrack.SegmentLength > curveEndZ)
            {
                if (!curves[ci].IsJump)
                {
                    // Calculate speed for jumps
                    int nextCI = (ci + 1) % curves.Count;
                    if (curves[nextCI].IsJump)
                    {
                        // Find end of jump
                        while (nextCI != ci && curves[nextCI].IsJump)
                            nextCI = (nextCI + 1) % curves.Count;
                        int nextI = Mathf.FloorToInt(curveInfos[nextCI].zOffset / Racetrack.SegmentLength);

                        // Set jump speed as minimum speed
                        float s = GetJumpSpeed(curveInfos, segments, seg, segments[nextI]);

                        if (s > 0.0f)
                        {
                            // Calculate minimum and maximum jump speeds
                            float sMin = s * AICarParams.JumpMinFactor;
                            if (s > min)
                                min = sMin;

                            float sMax = s * AICarParams.JumpMaxFactor;
                            if (s < max)
                                max = sMax;
                        }
                    }
                    else
                    {
                        // Next curve is not a jump.
                        // If curve curves downwards, calculate jump speed and set that as a *maximum*
                        // This should help keep the wheels on the ground.                        
                        if (curves[nextCI].Angles.x > curves[ci].Angles.x)
                        {
                            // Find end of next curve
                            int nextI = Mathf.FloorToInt((curveInfos[nextCI].zOffset + curves[nextCI].Length) / Racetrack.SegmentLength);
                            float s = GetJumpSpeed(curveInfos, segments, seg, segments[nextI]);
                            s *= AICarParams.StayOnRoadFactor;
                            if (s > 0.0f && s < max)
                                max = s;
                        }
                    }

                    // Calculate cornering limits
                    float cmin, cmax;
                    GetCorneringSpeeds(curves[nextCI], out cmin, out cmax);
                    if (cmin > min)
                        min = cmin;
                    if (cmax < max)
                        max = cmax;
                }

                // Look for explicit min/max override
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
                curveEndZ = ci < curves.Count
                    ? curveInfos[ci].zOffset + curves[ci].Length
                    : segments.Count * Racetrack.SegmentLength;
            }

            segmentDatas[i] = new SegmentAIData { MinSpeed = min, MaxSpeed = max, DistToJump = distToJump };
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
                    // Calculate this segment's minimum speed which would allow car to accelerate up to next seg's min speed
                    // Note: If curve is on a steep slope, then car's maximum "acceleration" may actually be a deceleration.
                    // Therefore the necessary minimum speed may actually be *greater* than the next segment.
                    // This is why we perform the calculation even if the next seg's min speed is 0.
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

                {
                    // Calculate distance to jump
                    float d = Mathf.Min(nextSegData.DistToJump + Racetrack.SegmentLength, 1000000.0f);
                    if (d < segData.DistToJump)
                        segData.DistToJump = d;
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

    private float GetJumpSpeed(Racetrack.CurveRuntimeInfo[] curveInfos, ICollection<Racetrack.Segment> segments, Racetrack.Segment seg, Racetrack.Segment nextSeg)
    {
        // Let  D = Jump distance (x = horizontal, y = vertical)
        //      T = Road tangent at jump
        //      g = Gravity acceleration
        //      s = Minimum speed required to complete the jump
        //       ________________
        //      /   g(Dx/Tx)^2
        // s = / ---------------
        //   \/   2(Dx/Tx - Dy)
        Vector3 diff = nextSeg.Position - seg.Position;
        Vector2 D = new Vector2(new Vector2(diff.x, diff.z).magnitude, diff.y);
        Vector3 dir = seg.GetSegmentToTrack().MultiplyVector(Vector3.forward).normalized;
        Vector2 T = new Vector2(new Vector2(dir.x, dir.z).magnitude, dir.y);
        float g = AICarParams.GravityAccel;

        // Jump must have a horizontal component. Otherwise the mathematics breaks down.
        if (T.x > 0.01f)
        {
            float f = D.x / T.x;
            float denom = 2.0f * (f - D.y);
            if (denom > 0.01f)
            {
                float num = g * f * f;
                return Mathf.Sqrt(num / denom);
            }
        }

        return 0.0f;
    }

    private void GetCorneringSpeeds(RacetrackCurve curve, out float min, out float max)
    {
        // Defaults
        min = 0.0f;
        max = 1000.0f;

        // Get variables
        float a = curve.Angles.y * Mathf.Deg2Rad;
        float O = -curve.Angles.z * Mathf.Deg2Rad;
        float l = curve.Length;
        float f = AICarParams.FrictionCoefficient;
        float g = Physics.gravity.magnitude;

        // Flip horizontally if necessary to make corner angle positive 
        if (a < 0.0f)
        {
            a = -a;
            O = -O;
        }

        // No limits for straight road
        if (a < 0.0001f)
            return;

        float c = g * l / a;
        float num, denom, lim;

        // Maximum speed
        num   = Mathf.Sin(O) + f * Mathf.Cos(O);
        denom = Mathf.Cos(O) - f * Mathf.Sin(O);
        if (denom > 0.0001f)
        {
            lim = c * num / denom;
            if (lim >= 0.0f)
            {
                lim = Mathf.Sqrt(lim);
                if (lim < max)
                    max = lim;
            }
        }

        // Minimum speed
        if (O <= 0.0f)
            return;

        num   = Mathf.Sin(O) - f * Mathf.Cos(O);
        denom = Mathf.Cos(O) + f * Mathf.Sin(O);
        if (denom > 0.0001f)
        {
            lim = c * num / denom;
            if (lim >= 0.0f)
            {
                lim = Mathf.Sqrt(lim);
                if (lim > min)
                    min = lim;
            }
        }
    }

    public class SegmentAIData
    {
        public float MinSpeed;
        public float MaxSpeed;
        public float DistToJump;
    }
}
