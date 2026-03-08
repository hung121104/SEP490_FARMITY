using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject hoverOverlay;

    private void Awake()
    {
        // Add a transparent full-rect child Image so pointer events fire over the entire button area,
        // not just where the sprite has opaque pixels. Events bubble up to this component.
        GameObject raycastCatcher = new GameObject("HoverRaycastCatcher");
        raycastCatcher.transform.SetParent(transform, false);
        RectTransform rt = raycastCatcher.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = raycastCatcher.AddComponent<Image>();
        img.color = Color.clear;
        img.raycastTarget = true;

        if (hoverOverlay != null)
            hoverOverlay.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverOverlay != null)
            hoverOverlay.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverOverlay != null)
            hoverOverlay.SetActive(false);
    }
}
