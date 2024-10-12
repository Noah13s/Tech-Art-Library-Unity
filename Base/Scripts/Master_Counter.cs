using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Master_Counter : MonoBehaviour
{
    #if UNITY_EDITOR
        [StyledString(12,1,1,1)]
    #endif
    [SerializeField]
    private string counterValueStrg;

    public GameObject textToUpdate;

    private int counterValue = 0;
    private void OnDrawGizmos()
    {
        counterValueStrg = counterValue.ToString();
    }

    [ContextMenu("IncrementCounter")]
    public void IncrementCounter()
    {
        counterValue++;
    }

    [ContextMenu("DecrementCounter")]
    public void DecrementCounter()
    {
        counterValue--;
    }

    public void SetCounterValue(int newCounterValue)
    {
        counterValue = newCounterValue;
    }

    public int GetCounterValue() { 
        return counterValue;
    }
}
