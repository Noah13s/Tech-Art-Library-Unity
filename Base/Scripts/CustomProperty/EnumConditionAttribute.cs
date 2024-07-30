using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

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

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(EnumConditionalVisibilityAttribute))]
public class EnumConditionalVisibilityDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EnumConditionalVisibilityAttribute conditionalAttribute = (EnumConditionalVisibilityAttribute)attribute;

        SerializedProperty enumProp = FindEnumProperty(property, conditionalAttribute.enumPropertyName);
        if (enumProp == null)
        {
            Debug.LogWarning($"Enum property {conditionalAttribute.enumPropertyName} not found.");
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        bool showProperty = enumProp.enumValueIndex == conditionalAttribute.enumValue;

        if (showProperty)
        {
            DrawNestedProperties(position, property, label);
        }
    }

    private void DrawNestedProperties(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Check if the property is an array and if it's a struct array
        if (property.isArray && property.propertyType == SerializedPropertyType.Generic)
        {
            bool showArray = ShouldShowArray(property);
            if (showArray)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }
        else
        {
            EditorGUI.PropertyField(position, property, label, true);
        }

        EditorGUI.EndProperty();
    }

    private bool ShouldShowArray(SerializedProperty arrayProperty)
    {
        // Check each element in the array to decide if the array should be shown
        for (int i = 0; i < arrayProperty.arraySize; i++)
        {
            SerializedProperty elementProperty = arrayProperty.GetArrayElementAtIndex(i);
            // Here you can check any condition on the element property
            // For example, check a specific field within the struct
            if (!IsElementVisible(elementProperty))
            {
                return false;
            }
        }
        return true;
    }

    private bool IsElementVisible(SerializedProperty elementProperty)
    {
        // Implement your condition to determine if the element should be visible
        // For example, check a specific field within the struct
        return true; // Replace with your visibility condition logic
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        EnumConditionalVisibilityAttribute conditionalAttribute = (EnumConditionalVisibilityAttribute)attribute;

        SerializedProperty enumProp = FindEnumProperty(property, conditionalAttribute.enumPropertyName);
        if (enumProp == null)
        {
            Debug.LogWarning($"Enum property {conditionalAttribute.enumPropertyName} not found.");
            return EditorGUIUtility.singleLineHeight;
        }

        bool showProperty = enumProp.enumValueIndex == conditionalAttribute.enumValue;

        if (showProperty)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
        else
        {
            return 0f;
        }
    }

    private SerializedProperty FindEnumProperty(SerializedProperty property, string enumPropertyName)
    {
        SerializedProperty enumProp = property.serializedObject.FindProperty(enumPropertyName);
        return enumProp;
    }
}
#endif
