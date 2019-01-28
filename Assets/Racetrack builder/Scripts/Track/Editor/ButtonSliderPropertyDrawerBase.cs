using UnityEditor;
using UnityEngine;

public abstract class ButtonSliderPropertyDrawerBase : PropertyDrawer
{
    protected const string AssetPath = "Assets/Scripts/Racetrack/Track/Editor/EditorAssets/";
    protected const float ButtonHeight = 25.0f;
    protected const float ButtonXPadding = 2.0f;
    protected const float ButtonYPadding = 2.0f;
    protected const float LineSpacing = 8.0f;

    private bool isAssetsLoaded = false;

    protected abstract void InternalLoadAssets();

    protected void LoadAssets()
    {
        if (!isAssetsLoaded)
        {
            InternalLoadAssets();
            isAssetsLoaded = true;
        }
    }

    protected float DrawAngleButtons(Rect position, float value, params PresetValueButton[] buttons)
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

    protected class PresetValueButton
    {
        public float Value;
        public Texture Texture;
        public string Text;

        public PresetValueButton(string textureAsset, float value)
        {
            Texture = AssetDatabase.LoadAssetAtPath<Texture>(AssetPath + textureAsset + ".png");
            Value = value;
        }

        public PresetValueButton(Texture texture, float value)
        {
            Texture = texture;
            Value = value;
        }

        public PresetValueButton(float value, string text = null)
        {
            Value = value;
            Text = text ?? value.ToString();
        }
    }
}
