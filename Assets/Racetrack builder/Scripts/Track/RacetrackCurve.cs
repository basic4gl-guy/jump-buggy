using System;
using UnityEngine;

/// <summary>
/// A single curve within a Racetrack.
/// RacetrackCurves are created as immediate children to the Racetrack object which manages
/// laying them out and generating the track meshes from them.
/// </summary>
public class RacetrackCurve : MonoBehaviour {

    [Header("Shape")]    
    [RacetrackCurveLength]    
    public float Length = 50.0f;

    /// <summary>
    /// Euler angles defining the turn (Y axis), pitch (X axis) and bank (Z axis) angles.
    /// The y angle (turn) is relative. 
    /// X and Z angles are absolute and specify the angles at the END of the curve.
    /// The X and Z at the start of the curve are inherited from the previous curve and
    /// lerp to the current curve's values across the length of the curve.
    /// </summary>
    [RacetrackCurveAngles]
    public Vector3 Angles = new Vector3();

    [Header("Flags")]

    /// <summary>
    /// True to create a "Jump".
    /// No meshes are generated for curves flagged as jumps.
    /// </summary>
    [Tooltip("Don't create any meshes for this curve.")]
    public bool IsJump = false;

    /// <summary>
    /// True if the player can respawn on this curve.
    /// Useful to mark parts of the track where respawning makes it difficult/impossible
    /// to progress (e.g. not enough run-up for a jump)
    /// </summary>
    [Tooltip("Whether this curve is a suitable respawn point, for when the car falls off the track. Used by the RacetrackProgressTracker script component.")]
    public bool CanRespawn = true;

    /// <summary>
    /// Template for generating meshes along the curve.
    /// If null, will inherit the template from the previous curve.
    /// </summary>
    [Header("Meshes")]
    [Tooltip("The template object supplying the meshes to warp to the racetrack curves. If null, will use the previous curve's template.")]
    public RacetrackMeshTemplate Template;

    /// <summary>
    /// The index of this curve within the Racetrack sequence
    /// </summary>
    [HideInInspector]
    public int Index;

    [Header("Miscellaneous")]
    [Tooltip("Allow this curve's length to be adjusted to create a closed circuit racetrack.")]
    public bool AutoAdjustLength = false;

    /// <summary>
    /// Find the track
    /// </summary>
    public Racetrack Track
    {
        get
        {
            if (transform.parent == null)
                throw new Exception("Curve (" + name + ") parent is not set. Parent should exist and have a 'Track' component.");
            var track = transform.parent.GetComponent<Racetrack>();
            if (track == null)
                throw new Exception("Curve (" + name + ") parent does not have a 'Racetrack' component.");
            return track;
        }
    }
}
