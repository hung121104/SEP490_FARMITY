using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MonthCellView : MonoBehaviour
{
    public TMP_Text text;
    public Image background;

    public void SetMonth(int month, bool active)
    {
        text.text = month.ToString();
        background.color = active
            ? new Color(1f, 0.75f, 0.2f)   // highlight
            : new Color(1f, 0.9f, 0.7f);   // normal
    }
}
