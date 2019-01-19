using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Curve))]
public class CurveEditor : Editor
{
    private const float ButtonHeight = 25.0f;
    private const float SpaceHeight = 10.0f;
    private readonly Color CurveColor1 = new Color(0.8f, 0.1f, 0.1f, 1.0f);
    private readonly Color CurveColor2 = new Color(0.8f, 0.8f, 0.1f, 1.0f);

    public override void OnInspectorGUI()
    {
        var curve = (Curve)target;
        var track = curve.Track;

        DrawDefaultInspector();

        if (track != null)
        {
            GUILayout.Space(SpaceHeight);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Update", GUILayout.Width(EditorGUIUtility.labelWidth - 5));
            if (GUILayout.Button("Curve", GUILayout.MinHeight(ButtonHeight)))
            {
                track.CreateMeshes(curve.Index - 1, curve.Index + 2);
            }
            if (GUILayout.Button("Rest of track", GUILayout.MinHeight(ButtonHeight)))
            {
                track.CreateMeshes(curve.Index - 1, curve.Track.Curves.Count);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(SpaceHeight);

            GUILayout.BeginHorizontal();
            GUILayout.Label(" ", GUILayout.Width(EditorGUIUtility.labelWidth - 5));
            if (GUILayout.Button("Add curve", GUILayout.MinHeight(ButtonHeight)))
            {
                var newCurve = track.AddCurve();
                Selection.activeGameObject = newCurve.gameObject;
            }
            GUILayout.EndHorizontal();
        }
    }

    public void OnSceneGUI()
    {
        var curve = (Curve)target;
        var track = curve.Track;

        if (track == null) return;

        // Draw line along all curves.
        // Highlight current curve.
        // Also record end point and direction of current curve so we can draw the length handle there.
        var segments = track.GetSegments();
        Vector3 lastPos = track.transform.position;
        Vector3 handlePos = curve.transform.position;
        Vector3 handleDir = curve.transform.TransformVector(Vector3.forward);
        int counter = 0;
        foreach (var seg in segments)
        {
            bool isCurve = seg.Curve.Index == curve.Index;

            Vector3 pos = track.transform.TransformPoint(seg.Position);
            if (++counter >= 4)
            {
                if (isCurve)
                    Handles.color = Color.white;
                else
                    Handles.color = (seg.Curve.Index % 2) == 0 ? CurveColor1 : CurveColor2;
                Handles.DrawLine(lastPos, pos);
                lastPos = pos;
                counter = 0;
            }

            if (isCurve)
            {
                handlePos = pos;
                handleDir = track.transform.TransformVector(seg.GetSegmentToTrack().MultiplyVector(Vector3.forward));
            }
        }

        Handles.color = Color.white;

        EditorGUI.BeginChangeCheck();
        float size = HandleUtility.GetHandleSize(handlePos) * 0.2f;
        Vector3 newPos = Handles.Slider(
            handlePos, 
            handleDir,
            size,
            Handles.ConeHandleCap,
            1.0f);
        if (EditorGUI.EndChangeCheck())
        {
            float newLength = curve.Length + Vector3.Dot((newPos - handlePos), handleDir);
            if (curve.Length != newLength && newLength >= 1.0f) {
                Undo.RecordObject(target, "Changed Curve Length");
                curve.Length = newLength;
                track.UpdateSegments();
            }
        }
    }
}