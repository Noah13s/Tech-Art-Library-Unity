using System;
using UnityEditor;
using UnityEngine;

public class StyledStringAttribute : PropertyAttribute
{
    public float size;
    public Color color;

    public StyledStringAttribute(float size, float r, float g, float b)
    {
        this.size = size;
        this.color = new Color(r, g, b);
    }
}
[CustomPropertyDrawer(typeof(StyledStringAttribute))]
public class StyledStringDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        StyledStringAttribute styledString = (StyledStringAttribute)attribute;

        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.fontSize = (int)styledString.size;
        style.normal.textColor = styledString.color;

        // Get the content of the string property
        string stringValue = property.stringValue;

        // Calculate the position for the styled text label
        Rect labelPosition = EditorGUI.PrefixLabel(position, label);
        EditorGUI.LabelField(labelPosition, stringValue, style);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}