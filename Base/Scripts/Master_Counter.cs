using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public class Master_Counter : MonoBehaviour
{
    [Header("Conditions")]
    [SerializeField] public bool equal = false;
    [NonSerialized] public int equalTo = 0;
    [SerializeField] public bool superior = false;
    [NonSerialized] public int superiorTo = 0;
    [SerializeField] public bool inferior = false;
    [NonSerialized] public int inferiorTo = 0;

    [Header("Other")]
    [SerializeField]
    private string counterValueStrg;

    [SerializeField]
    private int startValue = 0;

    [SerializeField] UnityEvent onConditionEvent;

    private int counterValue;

    private void Start()
    {
        counterValue = startValue;
    }

    [ContextMenu("IncrementCounter")]
    public void IncrementCounter()
    {
        counterValue++;
        CheckConditions();
    }

    [ContextMenu("DecrementCounter")]
    public void DecrementCounter()
    {
        counterValue--;
        CheckConditions();
    }

    [ContextMenu("ResetCounter")]
    public void ResetCounter()
    {
        counterValue = 0;
        CheckConditions();
    }

    public void SetCounterValue(int newCounterValue)
    {
        counterValue = newCounterValue;
        CheckConditions();
    }

    public int GetCounterValue()
    {
        return counterValue;
    }

    private void CheckConditions()
    {
        counterValueStrg = counterValue.ToString();

        if (equal && counterValue == equalTo)
        {
            if (onConditionEvent != null) {  onConditionEvent.Invoke(); }
        }
        if (superior && counterValue > superiorTo)
        {
            if (onConditionEvent != null) { onConditionEvent.Invoke(); }
        }
        if (inferior && counterValue < inferiorTo)
        {
            if (onConditionEvent != null) { onConditionEvent.Invoke(); }
        }
    }
}

#region Custom Editor
#if UNITY_EDITOR
[CustomEditor(typeof(Master_Counter))]
public class CustomEditorMasterCounter : Editor
{
    public override void OnInspectorGUI()
    {
        Master_Counter playerScript = (Master_Counter)target;

        // Draw default inspector
        DrawDefaultInspector();

        // Show 'Equal to value' field when 'equal' is checked
        if (playerScript.equal)
        {
            playerScript.equalTo = EditorGUILayout.IntField("Equal to value: ", playerScript.equalTo);
        }

        // You can add similar checks for 'superior' and 'inferior' if needed
        if (playerScript.superior)
        {
            playerScript.superiorTo = EditorGUILayout.IntField("Superior to value: ", playerScript.superiorTo);
        }

        if (playerScript.inferior)
        {
            playerScript.inferiorTo = EditorGUILayout.IntField("Inferior to value: ", playerScript.inferiorTo);
        }

        // Ensure to save the modified values
        EditorUtility.SetDirty(playerScript);
    }
}
#endif
#endregion