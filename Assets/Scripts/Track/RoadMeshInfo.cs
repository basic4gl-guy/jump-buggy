using System;
using UnityEngine;

/// <summary>
/// Extra information stored with generated meshes
/// </summary>
public class RoadMeshInfo : MonoBehaviour {

    // Link back to corresponding corner(s)
    public int StartCurveIndex;
    public int EndCurveIndex;

    internal bool ContainsCurveIndex(int index)
    {
        return index >= StartCurveIndex && index <= EndCurveIndex;
    }
}
