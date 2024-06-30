using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ConditionalVisibilityAttribute : PropertyAttribute
{
    public string conditionalProperty;
    public bool showInInspector;

    public ConditionalVisibilityAttribute(string conditionalProperty, bool showInInspector = true)
    {
        this.conditionalProperty = conditionalProperty;
        this.showInInspector = showInInspector;
    }
}
[CustomPropertyDrawer(typeof(ConditionalVisibilityAttribute))]
public class ConditionalVisibilityDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ConditionalVisibilityAttribute conditionalAttribute = attribute as ConditionalVisibilityAttribute;

        // Check if the conditional property exists in the serialized object
        SerializedProperty conditionalProp = property.serializedObject.FindProperty(conditionalAttribute.conditionalProperty);
        if (conditionalProp == null)
        {
            Debug.LogWarning($"Conditional property {conditionalAttribute.conditionalProperty} not found.");
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        // Check the value of the conditional property
        bool showProperty = conditionalProp.boolValue == conditionalAttribute.showInInspector;

        // If showInInspector is false, hide the property
        if (!showProperty)
            return;

        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        ConditionalVisibilityAttribute conditionalAttribute = attribute as ConditionalVisibilityAttribute;

        SerializedProperty conditionalProp = property.serializedObject.FindProperty(conditionalAttribute.conditionalProperty);
        if (conditionalProp == null)
        {
            Debug.LogWarning($"Conditional property {conditionalAttribute.conditionalProperty} not found.");
            return EditorGUIUtility.singleLineHeight;
        }

        bool showProperty = conditionalProp.boolValue == conditionalAttribute.showInInspector;

        // Return the height of the property field if it's visible, otherwise zero to hide it
        return showProperty ? EditorGUI.GetPropertyHeight(property, label) : 0f;
    }
}

