using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Curve))]
public class CurveEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var curve = (Curve)target;
        var track = curve.Track;

        DrawDefaultInspector();

        if (track != null)
        {
            GUILayout.Space(20);

            if (GUILayout.Button("Add curve"))
            {
                var newCurve = track.AddCurve();
                Selection.activeGameObject = newCurve.gameObject;
            }

            if (GUILayout.Button("Update curve"))
            {
                track.CreateMeshes(curve.Index - 1, curve.Index + 2);
            }
            if (GUILayout.Button("Update track from here"))
            {
                track.CreateMeshes(curve.Index - 1, curve.Track.Curves.Count);
            }
        }
    }
}