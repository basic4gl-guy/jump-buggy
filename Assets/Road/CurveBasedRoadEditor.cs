#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CurveBasedRoad))]
public class CurveBasedRoadEditor : Editor {
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Update road"))
        {
            ((CurveBasedRoad)target).RebuildMeshes();
        }
    }
}

#endif