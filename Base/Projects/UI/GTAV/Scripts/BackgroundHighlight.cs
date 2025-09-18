using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundHighlight : MonoBehaviour
{
    [SerializeField] private Color unhighlightColor;
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Image background;
    public void Highlight() 
    {
        if (background == null) return;
        if (unhighlightColor == null) unhighlightColor = background.color;
        background.color = highlightColor;
    }

    public void Unhighlight() 
    {
        if (background == null) return;
        background.color = unhighlightColor;
    }

    public void SetUnhighlightColor(Color color) 
    {
        unhighlightColor = color;
        if (background != null)
            background.color = unhighlightColor;
    }

    public void SetHighlightColor(Color color) 
    {
        highlightColor = color;
    }

    public void StoreCurrentColor()
    {
        unhighlightColor = background.color;
    }
}
