using UnityEngine;

public class RacetrackSpacingGroup : MonoBehaviour
{
    public int Index = 0;
    public float SpacingBefore = 0.0f;
    public float SpacingAfter = 10.0f;

    public float Spacing { get { return SpacingBefore + SpacingAfter; } }
}
