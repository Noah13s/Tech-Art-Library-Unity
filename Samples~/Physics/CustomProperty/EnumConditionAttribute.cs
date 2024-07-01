using System;
using UnityEngine;
using UnityEditor;

[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
public class EnumConditionalVisibilityAttribute : PropertyAttribute
{
    public string enumPropertyName;
    public int enumValue;

    public EnumConditionalVisibilityAttribute(string enumPropertyName, int enumValue)
    {
        this.enumPropertyName = enumPropertyName;
        this.enumValue = enumValue;
    }
}

[CustomPropertyDrawer(typeof(EnumConditionalVisibilityAttribute))]
public class EnumConditionalVisibilityDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EnumConditionalVisibilityAttribute conditionalAttribute = (EnumConditionalVisibilityAttribute)attribute;

        // Find the serialized property of the enum property
        SerializedProperty enumProp = FindPropertyRelative(property, conditionalAttribute.enumPropertyName);
        if (enumProp == null)
        {
            Debug.LogWarning($"Enum property {conditionalAttribute.enumPropertyName} not found.");
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        // Check if the enum property value matches the expected value
        bool showProperty = enumProp.enumValueIndex == conditionalAttribute.enumValue;

        // If the property should be hidden based on the condition, return without drawing it
        if (!showProperty)
            return;

        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        EnumConditionalVisibilityAttribute conditionalAttribute = (EnumConditionalVisibilityAttribute)attribute;

        SerializedProperty enumProp = FindPropertyRelative(property, conditionalAttribute.enumPropertyName);
        if (enumProp == null)
        {
            Debug.LogWarning($"Enum property {conditionalAttribute.enumPropertyName} not found.");
            return EditorGUIUtility.singleLineHeight;
        }

        // Check if the enum property value matches the expected value
        bool showProperty = enumProp.enumValueIndex == conditionalAttribute.enumValue;

        // Return the height of the property field if it's visible, otherwise zero to hide it
        return showProperty ? EditorGUI.GetPropertyHeight(property, label) : 0f;
    }

    private SerializedProperty FindPropertyRelative(SerializedProperty property, string relativePath)
    {
        var path = property.propertyPath.Replace(property.name, relativePath);
        return property.serializedObject.FindProperty(path);
    }
}
