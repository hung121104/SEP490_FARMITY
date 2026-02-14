using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    public int damageAmount = 1;
    public float knockbackForce = 30f; // Adjust knockback strength

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.ChangeHealth(-damageAmount);
            
            // Apply knockback to player
            PlayerKnockback playerKnockback = collision.gameObject.GetComponent<PlayerKnockback>();
            if (playerKnockback != null)
            {
                playerKnockback.Knockback(transform, knockbackForce);
            }
        }
    }
}
