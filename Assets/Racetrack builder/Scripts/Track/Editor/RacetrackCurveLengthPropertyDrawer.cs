using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(RacetrackCurveLengthAttribute))]
public class RacetrackCurveLengthPropertyDrawer : ButtonSliderPropertyDrawerBase
{
    private PresetValueButton[] LengthButtons;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        LoadAssets();

        if (property.propertyType != SerializedPropertyType.Float)
        {
            EditorGUI.LabelField(position, label.text, "[RacetrackCurveLength] must be applied to a float ");
            return;
        }

        var length = property.floatValue;

        float lineHeight = base.GetPropertyHeight(property, label);
        position.height = lineHeight;
        position.y += LineSpacing;

        bool rebuildCurve = false;

        EditorGUI.LabelField(position, "Length");
        float newLength = DrawAngleButtons(position, length, LengthButtons);
        if (length != newLength)
        {
            length = newLength;
            rebuildCurve = true;
        }
        position.y += ButtonHeight;
        length = EditorGUI.Slider(position, new GUIContent(" "), length, 1.0f, 250.0f);
        position.y += lineHeight + LineSpacing;

        property.floatValue = length;

        if (rebuildCurve)
        {
            var curve = property.serializedObject.targetObject as RacetrackCurve;
            curve.Length = length;
            if (curve != null && curve.Track != null)
                curve.Track.CreateMeshes(curve.Index - 1, curve.Track.Curves.Count);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return base.GetPropertyHeight(property, label) + ButtonHeight + LineSpacing;
    }

    protected override void InternalLoadAssets()
    {
        LengthButtons = new[]
        {
            new PresetValueButton(10),
            new PresetValueButton(20),
            new PresetValueButton(30),
            new PresetValueButton(50),
            new PresetValueButton(75),
            new PresetValueButton(100)
        };
    }
}
