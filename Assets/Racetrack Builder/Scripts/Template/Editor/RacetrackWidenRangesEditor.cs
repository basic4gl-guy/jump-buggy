using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RacetrackWidenRanges))]
public class RacetrackWidenRangesEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var ranges = (RacetrackWidenRanges)target;
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("<< Mirror to left", GUILayout.MinHeight(RacetrackConstants.ButtonHeight)))
        {
            using (var undo = new ScopedUndo("Mirror widen range"))
            {
                undo.RecordObject(ranges);
                ranges.Left = new RacetrackWidenRanges.Range(-ranges.Right.X1, -ranges.Right.X0, ranges.Right.MinWidth);
            }
        }
        if (GUILayout.Button("Mirror to right >>", GUILayout.MinHeight(RacetrackConstants.ButtonHeight)))
        {
            using (var undo = new ScopedUndo("Mirror widen range"))
            {
                undo.RecordObject(ranges);
                ranges.Right = new RacetrackWidenRanges.Range(-ranges.Left.X1, -ranges.Left.X0, ranges.Left.MinWidth);
            }
        }
        GUILayout.EndHorizontal();
    }

    public void OnSceneGUI()
    {
        var ranges = (RacetrackWidenRanges)target;
        Handles.matrix = Handles.matrix * ranges.transform.localToWorldMatrix * Matrix4x4.Translate(new Vector3(0.0f, ranges.Y, 0.0f));

        // Left range
        Handles.color = new Color(1.0f, 0.5f, 0.5f, 0.75f);
        Cross(ranges.Left.X0);
        Cross(ranges.Left.X1);
        DragHandle(-1.0f, ref ranges.Left.X1, ranges, ranges.Left.X0, ranges.Right.X0);
        DragHandle(-1.0f, ref ranges.Left.X0, ranges, -1000000.0f, ranges.Left.X1);

        // Right range
        Handles.color = new Color(0.5f, 0.5f, 1.0f, 0.75f);
        Cross(ranges.Right.X0);
        Cross(ranges.Right.X1);
        DragHandle(1.0f, ref ranges.Right.X0, ranges, ranges.Left.X1, ranges.Right.X1);
        DragHandle(1.0f, ref ranges.Right.X1, ranges, ranges.Right.X0, 1000000.0f);
    }

    private static void Cross(float x)
    {
        Vector3 center = new Vector3(x, 0.0f, 0.0f);
        float size = HandleUtility.GetHandleSize(center) * 0.075f;
        Handles.DrawLine(new Vector3(x, -size, 0.0f), new Vector3(x, size, 0.0f));
        Handles.DrawLine(new Vector3(x, 0.0f, -100.0f), new Vector3(x, 0.0f, 100.0f));
    }

    private void DragHandle(float side, ref float value, UnityEngine.Object obj, float min, float max)
    {
        Vector3 handleDir = new Vector3(1.0f, 0.0f, 0.0f) * side;

        // Range center point
        Vector3 center = new Vector3(value, 0.0f, 0.0f);

        // Handle offset
        float size = HandleUtility.GetHandleSize(center);
        Vector3 handle = center + handleDir * size * 0.1f;
        float handleSize = HandleUtility.GetHandleSize(handle);

        // Draw line to handle
        Handles.DrawLine(center, handle);

        // Draw drag handle itself and respond to drags
        EditorGUI.BeginChangeCheck();
        Vector3 movedHandle = Handles.Slider(handle, handleDir, handleSize * 0.05f, Handles.ConeHandleCap, 0.01f);
        if (EditorGUI.EndChangeCheck())
        {
            using (var undo = new ScopedUndo("Change widening range"))
            {
                undo.RecordObject(obj);
                value += movedHandle.x - handle.x;
                value = Mathf.Clamp(value, min, max);
            }
        }
    }
}
