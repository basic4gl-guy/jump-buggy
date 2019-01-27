﻿using UnityEngine;

/// <summary>
/// Used to mark subtrees that should be repeated along the racetrack at spaced intervals.
/// Should be nested underneath a RacetrackSpacingGroup which supplies the spacing information.
/// </summary>
public class RacetrackSpaced : MonoBehaviour
{
    /// <summary>
    /// Force vertical. (Align Y axis with world space Y.)
    /// </summary>
    public bool IsVertical = false;

    /// <summary>
    /// Maximum bank angle (positive or negative).
    /// Subtree will not be cloned if track exceeds this angle.
    /// </summary>
    public float MaxZAngle = 90.0f;

    /// <summary>
    /// Maximum pitch angle (positive or negative).
    /// Subtree will not be cloned if track exceeds this angle.
    /// </summary>
    public float MaxXAngle = 90.0f;
}
