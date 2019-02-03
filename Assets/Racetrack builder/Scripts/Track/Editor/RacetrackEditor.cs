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
            Undo.RecordObject(track, "Recreate all track meshes");
            track.CreateMeshes();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label(" ", GUILayout.Width(EditorGUIUtility.labelWidth - 5));
        if (GUILayout.Button("Add curve", GUILayout.MinHeight(RacetrackConstants.ButtonHeight)))
        {
            Undo.RecordObject(track, "Add curve");
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
            Undo.RecordObject(track, "Delete track meshes");
            track.DeleteMeshes();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label(" ", GUILayout.Width(EditorGUIUtility.labelWidth - 5));
        if (GUILayout.Button("Clear templates", GUILayout.MinHeight(RacetrackConstants.ButtonHeight)))
        {
            if (EditorUtility.DisplayDialog("Clear templates", "Really clear templates from all curves?", "Yes - Remove them", "Cancel"))
            {
                Undo.RecordObject(track, "Remove templates");
                track.RemoveTemplates();
            }
        }
        GUILayout.EndHorizontal();
    }


    [MenuItem("GameObject/3D Object/Racetrack", false, 10)]
    static void CreateNewRacetrack(MenuCommand menuCommand)
    {
        // Create object
        var obj = new GameObject("Racetrack");

        // Do that thing the Unity docs say is important
        GameObjectUtility.SetParentAndAlign(obj, menuCommand.context as GameObject);

        // Undo logic
        Undo.RegisterCreatedObjectUndo(obj, "Create " + obj.name);

        // Create a racetrack with a curve
        var racetrack = obj.AddComponent<Racetrack>();
        var curve = racetrack.AddCurve();

        // Attempt to load the "asphalt poles" mesh template and assign it to the curve
        var templateObj = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Racetrack Builder/Prefabs/Track Templates/asphalt poles.prefab");
        var template = templateObj != null ? templateObj.GetComponent<RacetrackMeshTemplate>() : null;
        if (template != null) {
            curve.Template = template;
            racetrack.CreateMeshes();
        }

        // Select new racetrack
        Selection.activeObject = curve;
    }
}
