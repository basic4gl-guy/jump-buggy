using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(CurveBasedRoad))]
public class CurveBasedRoadEditor : Editor {
    public override void OnInspectorGUI()
    {
        CurveBasedRoad road = ((CurveBasedRoad)target);
        DrawDefaultInspector();
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
                    if (curve.Mesh != null || curve.LODGroup != null)           // (Don't set mesh on jumps)
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
    }
}