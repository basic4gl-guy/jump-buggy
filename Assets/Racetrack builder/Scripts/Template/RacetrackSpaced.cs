using UnityEngine;

/// <summary>
/// Used to mark subtrees that should be repeated along the racetrack at spaced intervals.
/// Should be nested underneath a RacetrackSpacingGroup which supplies the spacing information.
/// </summary>
public class RacetrackSpaced : MonoBehaviour
{
    [Tooltip("Force vertical. (Align Y axis with world space Y.)")]
    public bool IsVertical = false;

    [Tooltip("Maximum bank angle (positive or negative). Subtree will not be cloned if track exceeds this angle.")]
    public float MaxZAngle = 90.0f;

    [Tooltip("Maximum pitch angle (positive or negative). Subtree will not be cloned if track exceeds this angle.")]
    public float MaxXAngle = 90.0f;

    [Tooltip("Should this object be moved horizontally when the track is widened")]
    public bool ApplyWidening = false;
}
