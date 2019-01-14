using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(CurveBasedRoad))]
public class CurveBasedRoadEditor : Editor {
    public override void OnInspectorGUI()
    {
        CurveBasedRoad road = ((CurveBasedRoad)target);
        DrawDefaultInspector();

        GUILayout.Space(20);

        if (GUILayout.Button("Update road"))
        {
            road.RebuildMeshes();
        }

        GUILayout.Space(40);

        if (GUILayout.Button("Create preview"))
        {
            road.CreatePreviewCopy();
        }

        GUILayout.Space(40);

        if (GUILayout.Button("Override meshes"))
        {
            if (EditorUtility.DisplayDialog("Confirm override", "Really override all meshes all corners?", "Override", "Cancel"))
                foreach (var curve in road.Curves)
                    if (!curve.IsJump)
                        curve.Mesh = road.OverrideMesh;
        }
        if (GUILayout.Button("Clear meshes"))
        {
            if (EditorUtility.DisplayDialog("Confirm clear meshes", "Really clear meshes from all corners?", "Clear", "Cancel"))
                foreach (var curve in road.Curves)
                    curve.Mesh = null;
        }
        if (GUILayout.Button("Clear LOD groups"))
        {
            if (EditorUtility.DisplayDialog("Confirm clear meshes", "Really clear LOD groups from all corners?", "Clear", "Cancel"))
                foreach (var curve in road.Curves)
                    curve.LODGroup = null;
        }
        if (GUILayout.Button("Delete child meshes"))
        {
            road.DeleteMeshes();
        }
        if (GUILayout.Button("Convert to track"))
        {
            var track = Track.Instance;
            if (track != null)
            {
                foreach (var src in road.Curves)
                {
                    var dst = track.AddCurve();
                    dst.Length = src.Length;
                    dst.Angles = src.Angles;
                    dst.CanRespawn = src.CanRespawn;
                    dst.IsJump = src.IsJump;
                }
            }
        }
    }
}