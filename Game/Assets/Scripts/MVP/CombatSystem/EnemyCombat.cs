using UnityEngine;
using TMPro;

public class EnemyCombat : MonoBehaviour
{
    public int damageAmount = 1;
    public float knockbackForce = 30f;
    public GameObject damagePopupPrefab;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            // Check if player is invulnerable - if so, don't apply ANY damage effects
            if (playerHealth.IsInvulnerable)
                return;

            // Apply damage
            playerHealth.ChangeHealth(-damageAmount);

            // Apply knockback
            PlayerKnockback playerKnockback = collision.gameObject.GetComponent<PlayerKnockback>();
            if (playerKnockback != null)
            {
                playerKnockback.Knockback(transform, knockbackForce);
            }

            // Show damage popup
            if (damagePopupPrefab != null)
            {
                Vector3 spawnPos = collision.transform.position + Vector3.up * 1f;
                GameObject damagePopup = Instantiate(damagePopupPrefab, spawnPos, Quaternion.identity);
                damagePopup.GetComponentInChildren<TMP_Text>().text = damageAmount.ToString();
            }
        }
    }
}