using System.Collections.Generic;
using UnityEngine;

public static class Util {
    public static int GetHash(params object[] values)
    {
        unchecked
        {
            int hash = (int)2166136261;
            foreach (var value in values)
            {
                hash = (hash * 16777619) ^ (value != null ? value.GetHashCode() : 0);
            }
            return hash;
        }
    }

    public static int GetArrayHash<T>(IEnumerable<T> array)
    {
        unchecked
        {
            int hash = (int)2166136261;
            foreach (var value in array)
            {
                hash = (hash * 16777619) ^ (value != null ? value.GetHashCode() : 0);
            }
            return hash;
        }
    }

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
}
