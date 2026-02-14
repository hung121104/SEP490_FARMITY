using UnityEngine;
using TMPro;

public class EnemyCombat : MonoBehaviour
{
    public int damageAmount = 1;
    public float knockbackForce = 30f; // Adjust knockback strength
    public GameObject damagePopupPrefab; // Prefab for damage popup

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.ChangeHealth(-damageAmount);

            PlayerKnockback playerKnockback = collision.gameObject.GetComponent<PlayerKnockback>();
            if (playerKnockback != null)
            {
                playerKnockback.Knockback(transform, knockbackForce);
            }

            Vector3 spawnPos = collision.transform.position + Vector3.up * 1f;
            GameObject damagePopup = Instantiate(damagePopupPrefab, spawnPos, Quaternion.identity);
            damagePopup.GetComponentInChildren<TMP_Text>().text = damageAmount.ToString();
        }
    }
}