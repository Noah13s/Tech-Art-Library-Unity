using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ReadOnlyAttribute : PropertyAttribute { }


#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        bool previousEnabled = GUI.enabled;
        GUI.enabled = false;

        EditorGUI.PropertyField(position, property, label, true);

        GUI.enabled = previousEnabled;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
#endif