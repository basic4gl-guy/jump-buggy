using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class RacetrackUVGenerator : MonoBehaviour
{
    public Material[] Materials;

    [Tooltip("Which side of the mesh to generate UVs for")]
    public RacetrackUVGenerationSide Side = RacetrackUVGenerationSide.Top;

    [Tooltip("Maximum angle between triangle normal and world up vector (degrees)")]
    public float MaxAngle = 30.0f;

    [Tooltip("Scale factor applied to UVs")]
    public Vector2 Scale = new Vector2(1.0f, 1.0f);

    [Tooltip("Offset applied to UVs")]
    public Vector2 Offset = Vector2.zero;

    [Tooltip("Rotation applied to UVs (degrees)")]
    public float Rotation;
}

public enum RacetrackUVGenerationSide
{
    Top,
    Bottom,
    TopAndBottom
}