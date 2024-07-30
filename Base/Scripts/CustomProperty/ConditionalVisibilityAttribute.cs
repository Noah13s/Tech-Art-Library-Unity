using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class ConditionalVisibilityAttribute : PropertyAttribute
{
    public string conditionalProperty;
    public bool invertCondition;

    public ConditionalVisibilityAttribute(string conditionalProperty, bool invertCondition = false)
    {
        this.conditionalProperty = conditionalProperty;
        this.invertCondition = invertCondition;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ConditionalVisibilityAttribute))]
public class ConditionalVisibilityDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ConditionalVisibilityAttribute conditionalAttribute = (ConditionalVisibilityAttribute)attribute;

        // Check if the conditional property exists in the serialized object
        SerializedProperty conditionalProp = FindPropertyRelative(property, conditionalAttribute.conditionalProperty);
        if (conditionalProp == null)
        {
            Debug.LogWarning($"Conditional property {conditionalAttribute.conditionalProperty} not found.");
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        // Check the value of the conditional property
        bool showProperty = conditionalProp.boolValue;

        if (conditionalAttribute.invertCondition)
        {
            showProperty = !showProperty;
        }

        // If showProperty is false, hide the property
        if (!showProperty)
            return;

        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        ConditionalVisibilityAttribute conditionalAttribute = (ConditionalVisibilityAttribute)attribute;

        SerializedProperty conditionalProp = FindPropertyRelative(property, conditionalAttribute.conditionalProperty);
        if (conditionalProp == null)
        {
            Debug.LogWarning($"Conditional property {conditionalAttribute.conditionalProperty} not found.");
            return EditorGUIUtility.singleLineHeight;
        }

        bool showProperty = conditionalProp.boolValue;

        if (conditionalAttribute.invertCondition)
        {
            showProperty = !showProperty;
        }

        // Return the height of the property field if it's visible, otherwise zero to hide it
        return showProperty ? EditorGUI.GetPropertyHeight(property, label) : 0f;
    }

    private SerializedProperty FindPropertyRelative(SerializedProperty property, string relativePath)
    {
        var path = property.propertyPath.Replace(property.name, relativePath);
        return property.serializedObject.FindProperty(path);
    }
}
#endif