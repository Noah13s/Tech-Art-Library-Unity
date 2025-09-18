using TMPro;
using UnityEngine;

public class TmpHighlight : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    public void Highlight()
    {
        if (text == null) return;
        text.fontStyle |= FontStyles.Bold;
    }

    public void Unhighlight()
    {
        if (text == null) return;
        text.fontStyle &= ~FontStyles.Bold;
    }
}
