using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoadProgressTracker))]
public class RoadProgressTrackerEditor : Editor {
    public override void OnInspectorGUI()
    {
        var progress = (RoadProgressTracker)target;
        DrawDefaultInspector();

        GUILayout.Space(20);
        if (GUILayout.Button("Reset car"))
        {
            progress.PutCarOnRoad();
        }
    }
}
