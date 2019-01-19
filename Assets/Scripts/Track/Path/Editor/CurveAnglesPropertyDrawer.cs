using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(CurveAnglesAttribute))]
public class CurveAnglesPropertyDrawer : PropertyDrawer
{
    private const float ButtonHeight = 25.0f;
    private const float ButtonXPadding = 2.0f;
    private const float ButtonYPadding = 2.0f;
    private const float LineSpacing = 8.0f;
    private const string AssetPath = "Assets/Scripts/Track/Path/Editor/EditorAssets/";

    private bool isAssetsLoaded = false;
    private AngleButton[] XAngleButtons;
    private AngleButton[] YAngleButtons;
    private AngleButton[] ZAngleButtons;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        LoadAssets();

        if (property.propertyType != SerializedPropertyType.Vector3)
        {
            EditorGUI.LabelField(position, label.text, "[CurveAngles] must be applied to a Vector3");
            return;
        }

        //var attr = (CurveAnglesAttribute)attribute;
        var angles = property.vector3Value;

        float lineHeight = base.GetPropertyHeight(property, label);
        position.height = lineHeight;
        position.y += LineSpacing;

        EditorGUI.LabelField(position, "Turn (Y)");
        angles.y = DrawAngleButtons(position, angles.y, YAngleButtons);
        position.y += ButtonHeight;
        angles.y = EditorGUI.Slider(position, new GUIContent(" "), angles.y, -180.0f, 180.0f);
        position.y += lineHeight + LineSpacing;

        EditorGUI.LabelField(position, "Gradient (X)");
        angles.x = DrawAngleButtons(position, angles.x, XAngleButtons);
        position.y += ButtonHeight;
        angles.x = EditorGUI.Slider(position, new GUIContent(" "), angles.x, -180.0f, 180.0f);
        position.y += lineHeight + LineSpacing;

        EditorGUI.LabelField(position, "Bank (Z)");
        angles.z = DrawAngleButtons(position, angles.z, ZAngleButtons);
        position.y += ButtonHeight;
        angles.z = -EditorGUI.Slider(position, new GUIContent(" "), -angles.z, -90.0f, 90.0f);

        property.vector3Value = angles;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return (base.GetPropertyHeight(property, label) + ButtonHeight + LineSpacing) * 3;
    }

    private void LoadAssets()
    {
        if (isAssetsLoaded) return;

        YAngleButtons = new[] {
            new AngleButton("YAngN90", -90.0f),
            new AngleButton("YAngN45", -45.0f),
            new AngleButton("YAngN30", -30.0f),
            new AngleButton("YAng0", 0.0f),
            new AngleButton("YAng30", 30.0f),
            new AngleButton("YAng45", 45.0f),
            new AngleButton("YAng90", 90.0f)
        };

        XAngleButtons = new[]
        {
            new AngleButton("Grad45", -45.0f),
            new AngleButton("Grad30", -30.0f),
            new AngleButton("Grad15", -15.0f),
            new AngleButton("Grad0", 0.0f),
            new AngleButton("GradN15", 15.0f),
            new AngleButton("GradN30", 30.0f),
            new AngleButton("GradN45", 45.0f)
        };

        ZAngleButtons = new[]
        {
            new AngleButton("Grad45", 45.0f),
            new AngleButton("Grad30", 30.0f),
            new AngleButton("Grad15", 15.0f),
            new AngleButton("Grad0", 0.0f),
            new AngleButton("GradN15", -15.0f),
            new AngleButton("GradN30", -30.0f),
            new AngleButton("GradN45", -45.0f)
        };

        isAssetsLoaded = true;
    }

    private float DrawAngleButtons(Rect position, float value, params AngleButton[] buttons)
    {
        float width = position.width - EditorGUIUtility.labelWidth;
        float buttonXDelta = (width + ButtonXPadding) / buttons.Length;
        float buttonWidth = buttonXDelta - ButtonXPadding;
        for (int i = 0; i < buttons.Length; i++)
        {
            var rect = new Rect(position.x + EditorGUIUtility.labelWidth + i * buttonXDelta, position.y, buttonWidth, ButtonHeight - ButtonYPadding);
            var content = buttons[i].Texture != null ? new GUIContent(buttons[i].Texture) : new GUIContent(buttons[i].Text);
            if (GUI.Button(rect, content))
                value = buttons[i].Value;
        }

        return value;
    }

    private class AngleButton
    {
        public string Text;
        public float Value;
        public Texture Texture;

        public AngleButton(string textureAsset, float value)
        {
            Texture = AssetDatabase.LoadAssetAtPath<Texture>(AssetPath + textureAsset + ".png");
            Value = value;
        }

        public AngleButton(Texture texture, float value)
        {
            Texture = texture;
            Value = value;
        }
    }
}
