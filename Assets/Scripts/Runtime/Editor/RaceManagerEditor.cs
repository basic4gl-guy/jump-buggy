using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RaceManager))]
public class RaceManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var manager = (RaceManager)target;
        DrawDefaultInspector();

        GUILayout.Space(20);
        if (GUILayout.Button("Setup for race"))
        {
            manager.SetupForRace();
        }
    }
}
