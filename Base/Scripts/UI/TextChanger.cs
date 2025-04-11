using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class TextChanger : MonoBehaviour
{
    [SerializeField] private Text legacyText;

    private Type tmpType;
    private object tmpTextComponent;

    private void Awake()
    {
        // Check for legacy Text component
        if (legacyText == null && gameObject.GetComponent<Text>() != null)
        {
            legacyText = GetComponent<Text>();
        }

        // Check if TextMeshPro is available at runtime using reflection
        tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");

        // If TMP is available, get the TMP component at runtime
        if (tmpType != null)
        {
            tmpTextComponent = gameObject.GetComponent(tmpType);
        }
    }

    private IEnumerator ForceLayoutUpdate()
    {
        // Wait one frame for the text change to take effect
        yield return null;

        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
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

        // Force layout update on the next frame
        StartCoroutine(ForceLayoutUpdate());
    }

    // Overloads for other types
    public void SetText(int text)
    {
        SetText(text.ToString());
    }

    public void SetText(Int64 text)
    {
        SetText(text.ToString());
    }

    public void SetText(double text)
    {
        SetText(text.ToString());
    }

    public void SetText(float text)
    {
        SetText(text.ToString());
    }

    public void SetString(params string[] texts)
    {
        // Example: Concatenate texts and update once
        string combined = string.Join(" ", texts);
        SetText(combined);
    }
}
