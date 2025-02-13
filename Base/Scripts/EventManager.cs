using UnityEngine;
using UnityEngine.Events;
using UnityEditor;

using System;
using System.Collections.Generic;

[Serializable]
public class NamedEvent
{
    public string eventName;
    [TextArea(1,10)]
    [SerializeField] string description;
    public UnityEvent eventAction;
}
public class EventManager : MonoBehaviour
{
    [SerializeField] private List<NamedEvent> events = new();
    public void TriggerEvent(string eventName)
    {
        NamedEvent namedEvent = events.Find(e => e.eventName == eventName);
        namedEvent?.eventAction.Invoke();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(EventManager))]
public class CustomEditorEventManager : Editor
{
    bool debugMode = false;// Tracks if in debug mode or not
    string eventToExecute;//  Stores the string of the event the user wants to execute

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EventManager script = (EventManager)target;

        GUILayout.Space(10);

        debugMode = GUILayout.Toggle(debugMode, debugMode? "Disable debug tools" : "Enable debug tools");
        if (debugMode)
        {
            eventToExecute = EditorGUILayout.TextField(new GUIContent("Event to execute", "Enter the event name to execute here"), eventToExecute);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Execute", "Be careful executing events manually especially in editor out of play mode !")))
            {
                script.TriggerEvent(eventToExecute);
            }
            EditorGUILayout.EndHorizontal();

        }
    }
}
#endif