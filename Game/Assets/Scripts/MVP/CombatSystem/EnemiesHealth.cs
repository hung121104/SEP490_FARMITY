using UnityEngine;

public class EnemiesHealth : MonoBehaviour
{
    public int currentHealth;
    public int maxHealth;

    public void ChangeHealth(int amount)
    {
        currentHealth += amount;

        if (currentHealth <= 0)
        {
            
        }
    }
}
