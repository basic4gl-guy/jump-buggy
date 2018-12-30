#if UNITY_EDITOR    
using UnityEditor;
using UnityEngine;

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

        if (GUILayout.Button("Override meshes"))
        {
            foreach (var curve in road.Curves)
                if (curve.Mesh != null || curve.LODGroup != null)           // (Don't set mesh on jumps)
                    curve.Mesh = road.OverrideMesh;
        }
        if (GUILayout.Button("Clear meshes"))
        {
            foreach (var curve in road.Curves)
                curve.Mesh = null;
        }
        if (GUILayout.Button("Clear LOD groups"))
        {
            foreach (var curve in road.Curves)
                curve.LODGroup = null;
        }
    }
}

#endif