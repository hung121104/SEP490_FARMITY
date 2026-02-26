using UnityEngine;
using TMPro;

public class EnemyCombat : MonoBehaviour
{
    public int damageAmount = 1;
    public float knockbackForce = 30f;
    public GameObject damagePopupPrefab;  // ← Already public, good!

    private PlayerHealthManager playerHealthManager;
    private PlayerKnockbackManager playerKnockback;

    private void Start()
    {
        // Cache references
        playerHealthManager = FindObjectOfType<PlayerHealthManager>();
        playerKnockback = FindObjectOfType<PlayerKnockbackManager>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (playerHealthManager == null)
            return;

        // Check if player is invulnerable
        if (playerHealthManager.IsInvulnerable)
            return;

        // Apply damage
        playerHealthManager.ChangeHealth(-damageAmount);

        // Apply knockback
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