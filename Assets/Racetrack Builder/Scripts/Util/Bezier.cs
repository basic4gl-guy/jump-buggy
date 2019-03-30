using System.Collections.Generic;
using UnityEngine;

public class Bezier 
{
    private readonly Vector3[] p;

    public Bezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        this.p = new[] { p0, p1, p2, p3 };
    }

    /// <summary>
    /// Get point at t, where t [0,1]
    /// </summary>
    public Vector3 GetPt(float t)
    {
        float mt = 1.0f - t;
        return mt * mt * mt * p[0] + 3.0f * mt * mt * t * p[1] + 3.0f * mt * t * t * p[2] + t * t * t * p[3];
    }

    /// <summary>
    /// Get tangent vector at t, where t [0,1]
    /// </summary>
    public Vector3 GetDerivative(float t)
    {
        float mt = 1.0f - t;
        return -3.0f * mt * mt * p[0] 
              + 3.0f * mt * mt * p[1] 
              - 6.0f * t * mt * p[1] 
              + 6.0f * t * mt * p[2]
              - 3.0f * t * t * p[2]
              + 3.0f * t * t * p[3];
    }

    /// <summary>
    /// Get normalised tangent vector at t, where t [0,1]
    /// </summary>
    public Vector3 GetTangent(float t)
    {
        Vector3 derivative = GetDerivative(t);
        return derivative.normalized;
    }

    public List<float> BuildDistanceLookup(float step, float interval, out float length)
    {
        // Start with start of curve
        var lookup = new List<float>();
        lookup.Add(0.0f);

        // Step along curve summing (approximate) distance.
        // Log t values for each interval.
        float distance = 0.0f;
        float nextDistance = interval;
        Vector3 prevPos = p[0];
        for (float t = step; t < 1.0f; t += step) {

            // Get next point and measure distance from previous
            // Add to distance sum
            Vector3 pos = GetPt(t);
            distance += (pos - prevPos).magnitude;
            
            // Log if reached next interval
            if (distance >= nextDistance)
            {
                lookup.Add(t);
                nextDistance += interval;
            }

            prevPos = pos;
        }

        length = distance;
        return lookup;
    }
}
