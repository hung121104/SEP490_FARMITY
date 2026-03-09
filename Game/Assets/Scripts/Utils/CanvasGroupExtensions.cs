using UnityEngine;

public static class CanvasGroupExtensions
{
    public static void Show(this CanvasGroup cg)
    {
        cg.alpha          = 1f;
        cg.interactable   = true;
        cg.blocksRaycasts = true;
    }

    public static void Hide(this CanvasGroup cg)
    {
        cg.alpha          = 0f;
        cg.interactable   = false;
        cg.blocksRaycasts = false;
    }
}
