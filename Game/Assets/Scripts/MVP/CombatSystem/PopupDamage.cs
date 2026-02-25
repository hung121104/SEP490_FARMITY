using UnityEngine;
using TMPro;

public class PopupDamage : MonoBehaviour
{
    [Header("Motion")]
    public Vector3 moveOffset = new Vector3(0f, 0.8f, 0f);
    public float duration = 0.8f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Scale")]
    public Vector3 startScale = new Vector3(0.85f, 0.85f, 1f);
    public Vector3 peakScale = new Vector3(1.1f, 1.1f, 1f);
    public Vector3 endScale = new Vector3(1f, 1f, 1f);

    [Header("Fade")]
    public float fadeOutStart = 0.5f;

    private TMP_Text tmpText;
    private Vector3 startPos;

    private void Awake()
    {
        tmpText = GetComponentInChildren<TMP_Text>();
    }

    private void Start()
    {
        startPos = transform.position;
        transform.localScale = startScale;
        StartCoroutine(Animate());
    }

    private System.Collections.IEnumerator Animate()
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            // Motion
            float moveT = moveCurve.Evaluate(t);
            transform.position = startPos + moveOffset * moveT;

            // Scale (pop then settle)
            if (t < 0.25f)
            {
                float popT = t / 0.25f;
                transform.localScale = Vector3.Lerp(startScale, peakScale, popT);
            }
            else
            {
                float settleT = (t - 0.25f) / 0.75f;
                transform.localScale = Vector3.Lerp(peakScale, endScale, settleT);
            }

            // Fade out
            if (tmpText != null && t >= fadeOutStart)
            {
                float fadeT = (t - fadeOutStart) / (1f - fadeOutStart);
                Color c = tmpText.color;
                c.a = Mathf.Lerp(1f, 0f, fadeT);
                tmpText.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
