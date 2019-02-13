using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        GUILayout.BeginHorizontal();
        GUILayout.Label(" ", GUILayout.Width(EditorGUIUtility.labelWidth - 5));
        if (GUILayout.Button("Create closed circuit", GUILayout.MinHeight(RacetrackConstants.ButtonHeight)))
        {
            this.CreateClosedCircuit(track);
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

    private void CreateClosedCircuit(Racetrack track)
    {
        try
        {
            // Must have at least 4 curves
            var curves = track.Curves;
            if (curves.Count < 4)
                throw new ApplicationException("Racetrack must have at least 4 curves.");

            // Must have eactly 3 curves with AutoAdjustLength selected
            var adjCurves = curves.Where(c => c.AutoAdjustLength).ToArray();
            if (adjCurves.Length != 3)
                throw new ApplicationException("Please tick 'Auto Adjust Length' to select exactly 3 curves. Currently " + adjCurves.Length + " curve(s) are selected.");

            // Attempt to line up last curve
            var lastCurve = curves[curves.Count - 1];
            var yAng = curves.Sum(c => c.Angles.y) - lastCurve.Angles.y;
            var lastCurveY = RacetrackUtil.LocalAngle(0.0f - yAng);
            if (Mathf.Abs(lastCurveY) > 90.0f)
                throw new ApplicationException("Please adjust the racetrack so that the last curve does not need to turn more than +/- 90 degrees.\nCurrently it would need to turn " + lastCurveY + " degrees");

            // Update the last curve
            Undo.RecordObjects(new[] { adjCurves[0], adjCurves[1], adjCurves[2], lastCurve }, "Create closed circuit");
            var prevLastCurveAngles = lastCurve.Angles;
            try
            {
                lastCurve.Angles = new Vector3(0.0f, lastCurveY, 0.0f);

                // Recalculate racetrack curve segments
                track.UpdateSegments();
                track.PositionCurves();
                var segments = track.GetSegments().ToList();
                if (!segments.Any())
                    throw new ApplicationException("Racetrack doesn't generate any segments (very short?)");
                var lastSegment = segments.Last();

                // Calculate auto-adjust-curve deltas, per unit length
                Vector3[] deltas = new Vector3[3];
                for (int i = 0; i < 3; i++)
                {
                    var curve = adjCurves[i];
                    Vector3 curveStartPos = curve.transform.localPosition;
                    Vector3 curveEndPos;
                    if (curve.Index < curves.Count - 1)
                    {
                        // Curve ends where next curve starts
                        var nextCurve = curves[curve.Index + 1];
                        curveEndPos = nextCurve.transform.localPosition;
                    }
                    else
                    {
                        // Curve ends at end of racetrack
                        curveEndPos = lastSegment.Position;
                    }

                    deltas[i] = (curveEndPos - curveStartPos) / curve.Length;
                }

                // Build basis matrix from curve deltas
                Matrix4x4 deltaFromLength = new Matrix4x4(
                    RacetrackUtil.ToVector4(deltas[0]),
                    RacetrackUtil.ToVector4(deltas[1]),
                    RacetrackUtil.ToVector4(deltas[2]),
                    new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

                // Check it is invertable
                if (Mathf.Abs(deltaFromLength.determinant) < 0.001f)
                    throw new ApplicationException("Auto-adjust-length curves are in too similar a direction. Please choose different curves.");

                // Invert to get position delta -> lengths transform
                Matrix4x4 lengthFromDelta = deltaFromLength.inverse;

                // Find the position difference between the end of the last curve 
                // and the start of the racetrack
                Vector3 racetrackStart = Vector3.zero;
                Vector3 racetrackEnd = lastSegment.Position;
                Vector3 circuitDelta = racetrackStart - racetrackEnd;

                // Calculate length adjustments for curves
                Vector3 lengthDeltas = lengthFromDelta.MultiplyVector(circuitDelta);

                // Check that this results in sensible lengths
                for (int i = 0; i < 3; i++)
                {
                    var length = adjCurves[i].Length + lengthDeltas[i];
                    if (length < track.SegmentLength || length > 250.0f)
                        throw new ApplicationException("Creating a closed circuit would require setting curve " + adjCurves[i].Index + " length to " + length + ".\nPlease select different curves to auto adjust the lengths");
                }

                // Update the curve lengths
                for (int i = 0; i < 3; i++)
                    adjCurves[i].Length += lengthDeltas[i];

                // Rebuild the racetrack
                track.CreateMeshes();
            }
            catch (ApplicationException)
            {
                // Undo changes on exception
                lastCurve.Angles = prevLastCurveAngles;
                track.UpdateSegments();
                track.PositionCurves();
                throw;
            }
        }
        catch (ApplicationException ex)
        {
            EditorUtility.DisplayDialog("Create closed circuit", ex.Message, "OK");
        }
    }
}
