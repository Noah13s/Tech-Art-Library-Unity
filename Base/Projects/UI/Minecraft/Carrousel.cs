using System;
using UnityEngine;
using TMPro;


public class Carrousel : MonoBehaviour
{
    public int index = -1;
    [SerializeField] private string[] stringArray;
    [SerializeField] private TextMeshProUGUI text;

    private void Awake()
    {
        if (stringArray == null || stringArray.Length == 0)
        {
            Debug.LogError("String array is not set or empty", this);
            return;
        }
        index = 0; // Initialize index to the first element
    }

    public void Next()
    {
        index++;
        if (index >= stringArray.Length)
        {
            index = 0;
        }
        Debug.Log(stringArray[index]);
        SetTextMeshProUGUI();
    }

    public void Previous()
    {
        index--;
        if (index < 0)
        {
            index = stringArray.Length - 1;
        }
        Debug.Log(stringArray[index]);
        SetTextMeshProUGUI();
    }

    public void SetIndex(int i)
    {
        if (i >= 0 && i < stringArray.Length)
        {
            index = i;
            Debug.Log(stringArray[index]);
            SetTextMeshProUGUI();
        }
        else
        {
            Debug.LogError("Index out of range");
        }
    }

    public void SetIndex(float i)
    {
        if (i >= 0 && i < stringArray.Length)
        {
            index = (int)i;
            Debug.Log(stringArray[index]);
            SetTextMeshProUGUI();
        }
        else
        {
            Debug.LogError("Index out of range");
        }
    }
    public int GetIndex()
    {
        return index;
    }
    public string GetString()
    {
        return stringArray[index];
    }
    public void SetTextMeshProUGUI()
    {
        if (text != null)
        {
            text.text = stringArray[index];
        }
        else
        {
            Debug.LogError("TextMeshProUGUI is not set");
        }
    }
}
