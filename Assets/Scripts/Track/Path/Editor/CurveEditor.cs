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
                track.AddCurve();
            }

            if (GUILayout.Button("Update track"))
            {
                track.CreateMeshes();
            }
        }
    }
}