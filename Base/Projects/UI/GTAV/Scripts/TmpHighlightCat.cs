using TMPro;
using UnityEngine;

public class TmpHighlightCat : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    public void Highlight()
    {
        if (text == null) return;
        text.color = Color.black;
    }

    public void Unhighlight()
    {
        if (text == null) return;
        text.color = Color.white;
    }
}