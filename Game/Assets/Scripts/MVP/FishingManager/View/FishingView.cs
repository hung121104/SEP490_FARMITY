using UnityEngine;
using UnityEngine.UI;

public class FishingView : MonoBehaviour
{
    [Header("UI References")]

    public RectTransform fishIcon;

    public RectTransform greenZone;

    public Image progressBar;

    public float barHeight = 400f;

    public void SetFishPosition(float value)
    {
        Vector2 pos = fishIcon.anchoredPosition;
        pos.y = value * barHeight;
        fishIcon.anchoredPosition = pos;
    }

    public void SetZonePosition(float value)
    {
        Vector2 pos = greenZone.anchoredPosition;
        pos.y = value * barHeight;
        greenZone.anchoredPosition = pos;
    }

    public void SetProgress(float value)
    {
        progressBar.fillAmount = value;
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}