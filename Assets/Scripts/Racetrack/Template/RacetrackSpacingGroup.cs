using UnityEngine;

/// <summary>
/// Attached to a RacetrackMeshTemplate object (typically a prefab) above 
/// one or more RacetrackSpaced.
/// Controls how far apart they are spaced along the race track.
/// </summary>
public class RacetrackSpacingGroup : MonoBehaviour
{
    /// <summary>
    /// The spacing group index.
    /// For correct spacing, repeated objects should be grouped into numbered spacing groups.
    /// E.g.
    ///     Support poles = 0
    ///     Median barrier posts = 1
    /// Multiple spacing groups with different indices can be active at the same time, and
    /// can have different spacing.
    /// Multiple spacing groups with the SAME index and different spacing will cause incorrect
    /// behaviour and should be avoided.
    /// </summary>
    public int Index = 0;

    /// <summary>
    /// Space to add BEFORE the repeated object(s)
    /// </summary>
    public float SpacingBefore = 10.0f;

    /// <summary>
    /// Space to add AFTER the repeated object(s)
    /// </summary>
    public float SpacingAfter = 10.0f;

    /// <summary>
    /// Gets the total spacing
    /// </summary>
    public float Spacing { get { return SpacingBefore + SpacingAfter; } }
}
