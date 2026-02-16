using UnityEngine;
using TMPro;
using System.Collections;

public class RollDisplayController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI diceNumber;

    [Header("Colors")]
    [SerializeField] private Color lowColor = new Color(0.9f, 0.3f, 0.2f);
    [SerializeField] private Color midColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color highColor = new Color(0.3f, 0.9f, 0.4f);

    [Header("Animation")]
    [SerializeField] private float wobbleScale = 1.15f;
    [SerializeField] private float wobbleSpeed = 10f;

    private Transform followTarget;
    private Vector3 followOffset;
    private Coroutine rollRoutine;
    private Vector3 baseScale;

    private void Awake()
    {
        if (diceNumber == null)
            diceNumber = GetComponentInChildren<TextMeshProUGUI>();

        baseScale = transform.localScale;
    }

    private void LateUpdate()
    {
        if (followTarget != null)
            transform.position = followTarget.position + followOffset;
    }

    public void AttachTo(Transform target, Vector3 offset)
    {
        followTarget = target;
        followOffset = offset;

        if (followTarget != null)
            transform.position = followTarget.position + followOffset;
    }

    public void PlayRoll(int finalValue, DiceTier tier, float duration)
    {
        if (diceNumber == null)
            return;

        if (rollRoutine != null)
            StopCoroutine(rollRoutine);

        rollRoutine = StartCoroutine(RollRoutine(finalValue, tier, duration));
    }

    private IEnumerator RollRoutine(int finalValue, DiceTier tier, float duration)
    {
        float elapsed = 0f;
        int sides = (int)tier;

        while (elapsed < duration)
        {
            int tempValue = Random.Range(1, sides + 1);
            SetNumber(tempValue, sides);

            float wobble = 1f + Mathf.Sin(Time.time * wobbleSpeed) * 0.05f;
            transform.localScale = baseScale * wobble * wobbleScale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        SetNumber(finalValue, sides);
        transform.localScale = baseScale;
        rollRoutine = null;
    }

    private void SetNumber(int value, int sides)
    {
        diceNumber.text = value.ToString();
        diceNumber.color = GetColorForRoll(value, sides);
    }

    private Color GetColorForRoll(int value, int sides)
    {
        float ratio = value / (float)sides;

        if (ratio <= 0.34f)
            return lowColor;

        if (ratio <= 0.67f)
            return midColor;

        return highColor;
    }
}