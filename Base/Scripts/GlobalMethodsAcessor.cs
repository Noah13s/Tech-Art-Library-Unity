using UnityEngine;
using UnityEngine.Events;
using System;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class GlobalMethodsAccessor : MonoBehaviour
{
    [Serializable]
    public class Method
    {
        public string methodName;
        public MethodEventType eventType;

        // Only one event will be used based on the method's return type
        [SerializeField] private UnityEvent voidEvent;
        [SerializeField] private StringUnityEvent stringEvent;
        [SerializeField] private IntUnityEvent intEvent;
        [SerializeField] private FloatUnityEvent floatEvent;
        [SerializeField] private BoolUnityEvent boolEvent;

        // Method to get the appropriate event based on type
        public UnityEventBase GetEvent()
        {
            return eventType switch
            {
                MethodEventType.Void => voidEvent,
                MethodEventType.String => stringEvent,
                MethodEventType.Int => intEvent,
                MethodEventType.Float => floatEvent,
                MethodEventType.Bool => boolEvent,
                _ => null
            };
        }

        // Methods to invoke the event with appropriate result
        public void InvokeVoid() => voidEvent?.Invoke();
        public void InvokeWithString(string result) => stringEvent?.Invoke(result);
        public void InvokeWithInt(int result) => intEvent?.Invoke(result);
        public void InvokeWithFloat(float result) => floatEvent?.Invoke(result);
        public void InvokeWithBool(bool result) => boolEvent?.Invoke(result);
    }

    public enum MethodEventType
    {
        Void,
        String,
        Int,
        Float,
        Bool,
        Unsupported
    }

    public MonoBehaviour targetScript;
    public Method[] publicMethods;

    [ContextMenu("Refresh Public Methods List")]
    public void Refresh()
    {
        if (targetScript == null)
        {
            publicMethods = Array.Empty<Method>();
            Debug.LogWarning("No target script assigned.", this);
            return;
        }

        Type type = targetScript.GetType();
        MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        // Filter to only include parameterless methods
        var parameterlessMethods = Array.FindAll(methods, m => m.GetParameters().Length == 0);
        publicMethods = new Method[parameterlessMethods.Length];

        for (int i = 0; i < parameterlessMethods.Length; i++)
        {
            MethodInfo methodInfo = parameterlessMethods[i];
            Method m = new Method { methodName = methodInfo.Name };

            Type returnType = methodInfo.ReturnType;
            if (returnType == typeof(void))
            {
                m.eventType = MethodEventType.Void;
            }
            else if (returnType == typeof(string))
            {
                m.eventType = MethodEventType.String;
            }
            else if (returnType == typeof(int))
            {
                m.eventType = MethodEventType.Int;
            }
            else if (returnType == typeof(float))
            {
                m.eventType = MethodEventType.Float;
            }
            else if (returnType == typeof(bool))
            {
                m.eventType = MethodEventType.Bool;
            }
            else
            {
                m.eventType = MethodEventType.Unsupported;
                Debug.LogWarning($"Return type {returnType} is not supported.");
            }

            publicMethods[i] = m;
        }

        Debug.Log($"Found {publicMethods.Length} parameterless public methods in {type.Name}");
    }

    // Method to invoke the appropriate event based on the method's return type
    public void InvokeMethod(int methodIndex)
    {
        if (methodIndex < 0 || methodIndex >= publicMethods.Length || targetScript == null)
            return;

        Method method = publicMethods[methodIndex];
        string methodName = method.methodName;

        // Get the method info
        MethodInfo methodInfo = targetScript.GetType().GetMethod(methodName);
        if (methodInfo == null)
            return;

        // Invoke the method on the target script
        object result = methodInfo.Invoke(targetScript, null);

        // Trigger the appropriate event with the result
        switch (method.eventType)
        {
            case MethodEventType.Void:
                method.InvokeVoid();
                break;
            case MethodEventType.String:
                method.InvokeWithString((string)result);
                break;
            case MethodEventType.Int:
                method.InvokeWithInt((int)result);
                break;
            case MethodEventType.Float:
                method.InvokeWithFloat((float)result);
                break;
            case MethodEventType.Bool:
                method.InvokeWithBool((bool)result);
                break;
        }
    }
}

[Serializable]
public class StringUnityEvent : UnityEvent<string> { }

[Serializable]
public class IntUnityEvent : UnityEvent<int> { }

[Serializable]
public class FloatUnityEvent : UnityEvent<float> { }

[Serializable]
public class BoolUnityEvent : UnityEvent<bool> { }

#if UNITY_EDITOR
[CustomEditor(typeof(GlobalMethodsAccessor))]
public class GlobalMethodsAccessorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("targetScript"));

        GlobalMethodsAccessor accessor = (GlobalMethodsAccessor)target;

        EditorGUILayout.Space();
        if (GUILayout.Button("Refresh Methods"))
        {
            accessor.Refresh();
        }

        if (accessor.publicMethods != null && accessor.publicMethods.Length > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Available Methods", EditorStyles.boldLabel);

            for (int i = 0; i < accessor.publicMethods.Length; i++)
            {
                var method = accessor.publicMethods[i];
                EditorGUILayout.BeginVertical(GUI.skin.box);

                EditorGUILayout.LabelField($"Method: {method.methodName} Index: {i}");

                // Display only the appropriate event type
                string propName = method.eventType switch
                {
                    GlobalMethodsAccessor.MethodEventType.Void => "voidEvent",
                    GlobalMethodsAccessor.MethodEventType.String => "stringEvent",
                    GlobalMethodsAccessor.MethodEventType.Int => "intEvent",
                    GlobalMethodsAccessor.MethodEventType.Float => "floatEvent",
                    GlobalMethodsAccessor.MethodEventType.Bool => "boolEvent",
                    _ => ""
                };

                if (!string.IsNullOrEmpty(propName))
                {
                    SerializedProperty eventProp = serializedObject.FindProperty($"publicMethods.Array.data[{i}].{propName}");
                    EditorGUILayout.PropertyField(eventProp, new GUIContent("Event"));
                }
                else
                {
                    EditorGUILayout.HelpBox("This method return type is not supported.", MessageType.Warning);
                }

                if (GUILayout.Button("Invoke Method"))
                {
                    accessor.InvokeMethod(i);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
