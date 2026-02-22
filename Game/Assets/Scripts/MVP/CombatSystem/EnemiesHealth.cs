using UnityEngine;

public class EnemiesHealth : MonoBehaviour
{
    public int currentHealth;
    public int maxHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void ChangeHealth(int amount)
    {
        currentHealth += amount;

        // Alert enemy AI that they were hit
        EnemyAI enemyAI = GetComponent<EnemyAI>();
        if (enemyAI != null && amount < 0) // Only on damage, not healing
        {
            enemyAI.OnHit();
        }

        if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }
    }
}
