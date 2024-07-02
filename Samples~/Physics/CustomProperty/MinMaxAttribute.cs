using UnityEditor;
using UnityEngine;

public class MinMaxAttribute : PropertyAttribute
{
    public float Min { get; private set; }
    public float Max { get; private set; }
    public bool HasMin { get; private set; }
    public bool HasMax { get; private set; }

    public MinMaxAttribute(float min = float.MinValue, float max = float.MaxValue)
    {
        Min = min;
        Max = max;
        HasMin = min != float.MinValue;
        HasMax = max != float.MaxValue;
    }
}


[CustomPropertyDrawer(typeof(MinMaxAttribute))]
public class MinMaxDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        MinMaxAttribute minMax = (MinMaxAttribute)attribute;

        if (property.propertyType == SerializedPropertyType.Float)
        {
            float value = property.floatValue;
            if (minMax.HasMin && minMax.HasMax)
            {
                value = EditorGUI.Slider(position, label, value, minMax.Min, minMax.Max);
            }
            else if (minMax.HasMin)
            {
                value = Mathf.Max(EditorGUI.FloatField(position, label, value), minMax.Min);
            }
            else if (minMax.HasMax)
            {
                value = Mathf.Min(EditorGUI.FloatField(position, label, value), minMax.Max);
            }
            else
            {
                value = EditorGUI.FloatField(position, label, value);
            }
            property.floatValue = value;
        }
        else if (property.propertyType == SerializedPropertyType.Integer)
        {
            int value = property.intValue;
            if (minMax.HasMin && minMax.HasMax)
            {
                value = EditorGUI.IntSlider(position, label, value, (int)minMax.Min, (int)minMax.Max);
            }
            else if (minMax.HasMin)
            {
                value = Mathf.Max(EditorGUI.IntField(position, label, value), (int)minMax.Min);
            }
            else if (minMax.HasMax)
            {
                value = Mathf.Min(EditorGUI.IntField(position, label, value), (int)minMax.Max);
            }
            else
            {
                value = EditorGUI.IntField(position, label, value);
            }
            property.intValue = value;
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "Use MinMax with float or int.");
        }
    }
}
