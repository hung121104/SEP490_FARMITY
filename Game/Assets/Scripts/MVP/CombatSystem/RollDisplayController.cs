using UnityEngine;
using System.Collections;

public class RollDisplayController : MonoBehaviour
{
    private Transform followTarget;
    private Vector3 followOffset;
    private Coroutine rollRoutine;
    private GameObject currentDiceInstance;

    private void LateUpdate()
    {
        if (followTarget != null)
        {
            Vector3 targetPosition = followTarget.position + followOffset;
            transform.position = targetPosition;
            
            if (currentDiceInstance != null)
            {
                currentDiceInstance.transform.position = targetPosition;
            }
        }
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
        if (rollRoutine != null)
            StopCoroutine(rollRoutine);

        rollRoutine = StartCoroutine(RollRoutine(finalValue, tier, duration));
    }

    private IEnumerator RollRoutine(int finalValue, DiceTier tier, float duration)
    {
        InstantiateDice(tier);

        if (currentDiceInstance == null)
            yield break;

        float elapsed = 0f;
        int sides = (int)tier;

        while (elapsed < duration)
        {
            int tempValue = Random.Range(1, sides + 1);
            UpdateDiceDisplay(tempValue);

            elapsed += Time.deltaTime;
            yield return null;
        }

        UpdateDiceDisplay(finalValue);
        rollRoutine = null;
    }

    private void InstantiateDice(DiceTier tier)
    {
        if (currentDiceInstance != null)
            Destroy(currentDiceInstance);

        GameObject prefabToUse = DiceDisplayManager.Instance.GetDicePrefab(tier);
        if (prefabToUse == null)
        {
            Debug.LogError($"Dice prefab for {tier} not assigned in DiceDisplayManager!");
            return;
        }

        Vector3 spawnPosition = followTarget != null ? followTarget.position + followOffset : transform.position;
        currentDiceInstance = Instantiate(prefabToUse, spawnPosition, Quaternion.identity);
    }

    private void UpdateDiceDisplay(int value)
    {
        if (currentDiceInstance == null)
            return;

        TMPro.TextMeshProUGUI numberText = currentDiceInstance.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (numberText != null)
        {
            numberText.text = value.ToString();
            numberText.color = Color.white;
        }
    }

    public void Hide()
    {
        if (currentDiceInstance != null)
            currentDiceInstance.SetActive(false);
    }

    public void Show()
    {
        if (currentDiceInstance != null)
            currentDiceInstance.SetActive(true);
    }
}