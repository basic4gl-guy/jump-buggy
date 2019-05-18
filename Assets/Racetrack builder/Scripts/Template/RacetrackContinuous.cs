using UnityEngine;

/// <summary>
/// Used to mark subtrees inside a racetrack mesh template (typically a prefab) as containing "continuous" meshes.
/// All meshes contained in the object's subtree will be copied and warped around the road curves.
/// </summary>
public class RacetrackContinuous : MonoBehaviour
{
    [Tooltip("Whether to remove internal faces between adjacent meshes of the same model")]
    public bool RemoveInternalFaces = true;

    [Tooltip("Face is considered internal if all vertices are less than this distance from the start/end of the mesh")]
    public float InternalFaceZThreshold = 0.001f;
}
