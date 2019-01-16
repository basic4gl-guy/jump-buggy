using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Track))]
public class TrackEditor : Editor {
    public override void OnInspectorGUI()
    {
        var track = (Track)target;
        DrawDefaultInspector();

        GUILayout.Space(20);

        if (GUILayout.Button("Add curve"))
        {
            var curve = track.AddCurve();
            Selection.activeGameObject = curve.gameObject;
        }

        if (GUILayout.Button("Delete meshes"))
        {
            track.DeleteMeshes();
        }

        if (GUILayout.Button("Remove templates"))
        {
            if (EditorUtility.DisplayDialog("Remove templates", "Really set template to null on all curves?", "Yes - Remove them", "Cancel"))
                track.RemoveTemplates();
        }

        if (GUILayout.Button("Update track"))
        {
            track.CreateMeshes();
        }
    }
}
