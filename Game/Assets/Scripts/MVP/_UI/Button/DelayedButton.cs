using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class DelayedButton : Button
{
    [SerializeField] private float pressDelay = 0.2f;
    [SerializeField] private float highlightDelay = 0f;

    private Coroutine pressCoroutine;
    private Coroutine highlightCoroutine;

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (!IsActive() || !IsInteractable())
            return;

        if (pressCoroutine != null)
            StopCoroutine(pressCoroutine);

        pressCoroutine = StartCoroutine(DelayedPress(eventData));
    }

    private IEnumerator DelayedPress(PointerEventData eventData)
    {
        yield return new WaitForSeconds(pressDelay);
        base.OnPointerDown(eventData);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsActive() || !IsInteractable())
            return;

        if (highlightCoroutine != null)
            StopCoroutine(highlightCoroutine);

        highlightCoroutine = StartCoroutine(DelayedHighlight(eventData));
    }

    private IEnumerator DelayedHighlight(PointerEventData eventData)
    {
        yield return new WaitForSeconds(highlightDelay);
        base.OnPointerEnter(eventData);
    }
}
