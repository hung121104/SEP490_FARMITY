using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public int currentHealth;
    public int maxHealth;
    public Slider healthBar;
    public Slider healthBarEase; // The smooth following health bar
    
    [Header("Health Bar Ease Settings")]
    public float easeSpeed = 1f; // How fast the ease bar catches up

    private float targetHealthValue;

    void Start()
    {
        currentHealth = maxHealth;
        healthBar.maxValue = maxHealth;
        healthBar.value = currentHealth;
        
        if (healthBarEase != null)
        {
            healthBarEase.maxValue = maxHealth;
            healthBarEase.value = currentHealth;
        }
        
        targetHealthValue = currentHealth;
    }

    void Update()
    {
        // Smoothly lerp the ease health bar towards the target
        if (healthBarEase != null)
        {
            healthBarEase.value = Mathf.Lerp(healthBarEase.value, targetHealthValue, easeSpeed * Time.deltaTime);
        }
    }

    public void ChangeHealth(int amount)
    {
        currentHealth += amount;
        
        // Clamp health between 0 and max
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        // Update main health bar instantly
        healthBar.value = currentHealth;
        
        // Set target for ease bar to smoothly follow
        targetHealthValue = currentHealth;
        
        if (currentHealth <= 0)
        {
            gameObject.SetActive(false);
        }
    }
}