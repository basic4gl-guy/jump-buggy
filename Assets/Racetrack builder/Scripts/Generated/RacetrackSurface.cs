using UnityEngine;

/// <summary>
/// Attached to the main driving surface mesh, to identify it, and link it
/// back to its corresponding curve(s).
/// Used at runtime by RacetrackProgressTracker
/// </summary>
public class RacetrackSurface : MonoBehaviour
{
    // Link back to corresponding corner(s)
    public int StartCurveIndex;
    public int EndCurveIndex;

    public bool ContainsCurveIndex(int index)
    {
        return index >= StartCurveIndex && index <= EndCurveIndex;
    }
}
