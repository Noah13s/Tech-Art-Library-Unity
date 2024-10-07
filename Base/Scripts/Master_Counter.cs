using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Master_Counter : MonoBehaviour
{
    #if UNITY_EDITOR
        [StyledString(12,1,1,1)]
    #endif
    [SerializeField]
    private string counterValueStrg;

    private int counterValue;
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
}
