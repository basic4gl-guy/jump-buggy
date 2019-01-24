using UnityEngine;

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
