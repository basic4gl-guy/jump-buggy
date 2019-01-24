using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Racetrack))]
public class RacetrackEditor : Editor {
    public override void OnInspectorGUI()
    {
        var track = (Racetrack)target;
        DrawDefaultInspector();

        GUILayout.Space(RacetrackConstants.SpaceHeight);

        GUILayout.Label("Track building");

        GUILayout.BeginHorizontal();
        GUILayout.Label(" ", GUILayout.Width(EditorGUIUtility.labelWidth - 5));
        if (GUILayout.Button("Update whole track", GUILayout.MinHeight(RacetrackConstants.ButtonHeight)))
        {
            track.CreateMeshes();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label(" ", GUILayout.Width(EditorGUIUtility.labelWidth - 5));
        if (GUILayout.Button("Add curve", GUILayout.MinHeight(RacetrackConstants.ButtonHeight)))
        {
            var curve = track.AddCurve();
            Selection.activeGameObject = curve.gameObject;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(RacetrackConstants.SpaceHeight);

        GUILayout.Label("Miscellaneous");

        GUILayout.BeginHorizontal();
        GUILayout.Label(" ", GUILayout.Width(EditorGUIUtility.labelWidth - 5));
        if (GUILayout.Button("Delete meshes", GUILayout.MinHeight(RacetrackConstants.ButtonHeight)))
        {
            track.DeleteMeshes();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label(" ", GUILayout.Width(EditorGUIUtility.labelWidth - 5));
        if (GUILayout.Button("Clear templates", GUILayout.MinHeight(RacetrackConstants.ButtonHeight)))
        {
            if (EditorUtility.DisplayDialog("Clear templates", "Really clear templates from all curves?", "Yes - Remove them", "Cancel"))
                track.RemoveTemplates();
        }
        GUILayout.EndHorizontal();
    }
}
