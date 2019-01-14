using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProgressTracker))]
public class ProgressTrackerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var progress = (ProgressTracker)target;
        DrawDefaultInspector();

        GUILayout.Space(20);
        if (GUILayout.Button("Reset car"))
        {
            progress.PutCarOnRoad();
        }
    }
}
