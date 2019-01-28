using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RacetrackProgressTracker))]
public class RacetrackProgressTrackerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var progress = (RacetrackProgressTracker)target;
        DrawDefaultInspector();

        GUILayout.Space(20);
        if (GUILayout.Button("Reset car"))
        {
            progress.PutCarOnRoad();
        }
    }
}
