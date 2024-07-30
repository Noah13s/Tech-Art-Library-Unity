using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class ButtonAttribute : PropertyAttribute
{
    public string buttonText;
    public string methodName;

    public ButtonAttribute(string buttonText, string methodName)
    {
        this.buttonText = buttonText;
        this.methodName = methodName;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ButtonAttribute))]
public class ButtonDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ButtonAttribute buttonAttribute = (ButtonAttribute)attribute;

        if (GUI.Button(position, buttonAttribute.buttonText))
        {
            // Get the target object of the serialized property
            object targetObject = property.serializedObject.targetObject;

            // Get the method info based on the method name specified in the attribute
            System.Reflection.MethodInfo methodInfo = targetObject.GetType().GetMethod(buttonAttribute.methodName);

            // Invoke the method if found
            if (methodInfo != null)
            {
                methodInfo.Invoke(targetObject, null);
            }
            else
            {
                Debug.LogWarning($"Method '{buttonAttribute.methodName}' not found!");
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}
#endif