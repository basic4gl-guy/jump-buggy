using UnityEngine;

public class SpacedMesh : MonoBehaviour{
    public int SpacingGroup = 0;
    public float SpacingBefore = 0.0f;
    public float SpacingAfter = 10.0f;
    public bool IsVertical = false;

    public float Spacing { get { return SpacingBefore + SpacingAfter; } }
}
