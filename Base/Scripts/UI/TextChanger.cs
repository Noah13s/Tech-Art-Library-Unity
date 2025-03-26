using UnityEngine;
using UnityEngine.UI;
using System;

public class TextChanger : MonoBehaviour
{
    [SerializeField] private Text legacyText;

    private Type tmpType;
    private object tmpTextComponent;

    private void Start()
    {
        // Check for legacy Text component
        if (legacyText == null && gameObject.GetComponent<Text>() != null)
        {
            legacyText = GetComponent<Text>();
        }

        // Check if TextMesh Pro is available at runtime using reflection
        tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");

        // If TMP is available, get the TMP component at runtime
        if (tmpType != null)
        {
            tmpTextComponent = gameObject.GetComponent(tmpType);
        }
    }

    public void SetText(string text)
    {
        // If TMP is available, set its text using reflection
        if (tmpTextComponent != null)
        {
            var textProperty = tmpType.GetProperty("text");
            if (textProperty != null)
            {
                textProperty.SetValue(tmpTextComponent, text);
            }
        }

        // Set legacy text (always available)
        if (legacyText != null)
        {
            legacyText.text = text;
        }
    }

    public void SetText(int text)
    {
        // If TMP is available, set its text using reflection
        if (tmpTextComponent != null)
        {
            var textProperty = tmpType.GetProperty("text");
            if (textProperty != null)
            {
                textProperty.SetValue(tmpTextComponent, text.ToString());
            }
        }

        // Set legacy text (always available)
        if (legacyText != null)
        {
            legacyText.text = text.ToString();
        }
    }

    public void SetText(float text)
    {
        // If TMP is available, set its text using reflection
        if (tmpTextComponent != null)
        {
            var textProperty = tmpType.GetProperty("text");
            if (textProperty != null)
            {
                textProperty.SetValue(tmpTextComponent, text.ToString());
            }
        }

        // Set legacy text (always available)
        if (legacyText != null)
        {
            legacyText.text = text.ToString();
        }
    }

}
