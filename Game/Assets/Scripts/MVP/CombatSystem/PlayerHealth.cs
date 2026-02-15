using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    public Slider healthBar;
    public Slider healthBarEase;
    public TextMeshProUGUI healthText;

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
        if (statsManager == null)
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
        }
        
        if (healthBarEase != null)
        {
            healthBarEase.maxValue = maxHealth;
        }
        
        UpdateHealthText();
    }

    private void UpdateHealthText()
    {
        if (healthText != null)
        {
            healthText.text = statsManager.CurrentHealth.ToString();
        }
    }
}