using System;
using System.Collections.Generic;
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

    public static T FindEffectiveComponent<T>(Component searchFrom, Component searchTo) where T : Component
    {
        // Search up parent chain from "searchFrom"
        for (Transform t = searchFrom.transform; t != null; t = t.parent)
        {
            // Found component?
            var component = t.GetComponent<T>();
            if (component != null)
                return component;

            // Reached searchTo ancestor?
            if (t.gameObject == searchTo.gameObject)
                return null;
        }

        // Reaching here implies searchTo is not an ancestor of searchFrom.
        return null;
    }

    public static Matrix4x4 GetAncestorFromDescendentMatrix(Component ancestor, Component descendent)
    {
        return ancestor.transform.localToWorldMatrix.inverse * descendent.transform.localToWorldMatrix;
    }

    public static int FindIndex<T>(IEnumerable<T> items, T item) where T: class
    {
        int index = 0;
        foreach (var i in items)
        {
            if (i == item)
                return index;
            index++;
        }

        // Item not found
        return -1;
    }

    public static int FindIndex<T>(IEnumerable<T> items, Func<T, bool> predicate)
    {
        int index = 0;
        foreach (var i in items)
        {
            if (predicate(i))
                return index;
            index++;
        }

        // Item not found
        return -1;

    }
}
