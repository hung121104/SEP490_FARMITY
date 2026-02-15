using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public Slider healthBar;
    public Slider healthBarEase; // The smooth following health bar

    private float targetHealthValue;
    private StatsManager statsManager;

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

        statsManager.currentHealth = statsManager.maxHealth;

        if (healthBar != null)
        {
            healthBar.maxValue = statsManager.maxHealth;
            healthBar.value = statsManager.currentHealth;
        }

        if (healthBarEase != null)
        {
            healthBarEase.maxValue = statsManager.maxHealth;
            healthBarEase.value = statsManager.currentHealth;
        }

        targetHealthValue = statsManager.currentHealth;
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
    }

    public void ChangeHealth(int amount)
    {
        if (statsManager == null)
            return;

        statsManager.currentHealth += amount;
        statsManager.currentHealth = Mathf.Clamp(
            statsManager.currentHealth,
            0,
            statsManager.maxHealth
        );

        if (healthBar != null)
        {
            healthBar.value = statsManager.currentHealth;
        }

        targetHealthValue = statsManager.currentHealth;

        if (statsManager.currentHealth <= 0)
        {
            gameObject.SetActive(false);
        }
    }
}