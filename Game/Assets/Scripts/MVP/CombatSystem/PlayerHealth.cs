using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public Slider healthBar;
    public Slider healthBarEase;
    public TextMeshProUGUI healthText;

    private float targetHealthValue;
    private StatsManager statsManager;
    private bool isInvulnerable = false;

    private void Start()
    {
        statsManager = StatsManager.Instance;
        if (statsManager == null)
        {
            statsManager = FindObjectOfType<StatsManager>();
            if (statsManager == null)
            {
                enabled = false;
                return;
            }
        }

        int maxHealth = statsManager.GetMaxHealth();
        statsManager.CurrentHealth = maxHealth;

        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = statsManager.CurrentHealth;
        }

        if (healthBarEase != null)
        {
            healthBarEase.maxValue = maxHealth;
            healthBarEase.value = statsManager.CurrentHealth;
        }

        targetHealthValue = statsManager.CurrentHealth;
    }

    private void Update()
    {
        if (healthBarEase != null)
        {
            healthBarEase.value = Mathf.Lerp(
                healthBarEase.value,
                targetHealthValue,
                statsManager.easeSpeed * Time.deltaTime
            );
        }

        UpdateHealthText();
    }

    public void ChangeHealth(int amount)
    {
        // Don't apply damage if invulnerable
        if (statsManager == null || (isInvulnerable && amount < 0))
            return;

        statsManager.CurrentHealth += amount;

        if (healthBar != null)
        {
            healthBar.value = statsManager.CurrentHealth;
        }

        targetHealthValue = statsManager.CurrentHealth;

        if (statsManager.CurrentHealth <= 0)
        {
            gameObject.SetActive(false);
        }
    }

    public void RefreshHealthBar()
    {
        int maxHealth = statsManager.GetMaxHealth();

        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = statsManager.CurrentHealth;
        }

        if (healthBarEase != null)
        {
            healthBarEase.maxValue = maxHealth;
            healthBarEase.value = statsManager.CurrentHealth;
        }

        targetHealthValue = statsManager.CurrentHealth;
        UpdateHealthText();
    }

    private void UpdateHealthText()
    {
        if (healthText != null)
        {
            healthText.text = statsManager.CurrentHealth.ToString();
        }
    }

    #region Invulnerability (iFrames)

    /// <summary>
    /// Make player invulnerable for a duration
    /// </summary>
    /// <param name="duration">Duration in seconds</param>
    public void SetInvulnerable(float duration)
    {
        StartCoroutine(InvulnerabilityCoroutine(duration));
    }

    /// <summary>
    /// Instantly enable/disable invulnerability
    /// </summary>
    public void SetInvulnerable(bool invulnerable)
    {
        isInvulnerable = invulnerable;
    }

    private IEnumerator InvulnerabilityCoroutine(float duration)
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(duration);
        isInvulnerable = false;
    }

    public bool IsInvulnerable => isInvulnerable;

    #endregion
}