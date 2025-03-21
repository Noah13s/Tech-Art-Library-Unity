using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class Master_Counter : MonoBehaviour
{
    [Header("Conditions")]
    [SerializeField] bool equal = false;
    [SerializeField] int equalTo = 0;
    [SerializeField] bool superior = false;
    [SerializeField] int superiorTo = 0;
    [SerializeField] bool inferior = false;
    [SerializeField] int inferiorTo = 0;

    [Header("Other")]
    [SerializeField]
    private string counterValueStrg;

    [SerializeField]
    private int startValue = 0;

    public TextMeshProUGUI textToUpdate;

    [SerializeField] UnityEvent onConditionEvent;

    private int counterValue;

    private void Start()
    {
        counterValue = startValue;
    }

    private void OnDrawGizmos()
    {
        counterValueStrg = counterValue.ToString();
        if (textToUpdate != null)
            textToUpdate.text = counterValueStrg;
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
        if (textToUpdate != null)
            textToUpdate.text = counterValueStrg;

        if (equal && counterValue == equalTo)
        {
            onConditionEvent?.Invoke();
        }
        if (superior && counterValue > superiorTo)
        {
            onConditionEvent?.Invoke();
        }
        if (inferior && counterValue < inferiorTo)
        {
            onConditionEvent?.Invoke();
        }
    }
}