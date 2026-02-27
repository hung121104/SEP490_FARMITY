using UnityEngine;
using TMPro;
using Photon.Pun;

public class EnemyCombat : MonoBehaviour
{
    public int damageAmount = 1;
    public float knockbackForce = 30f;
    public GameObject damagePopupPrefab;

    private PlayerHealthManager playerHealthManager;
    private PlayerKnockbackManager playerKnockback;

    private void Start()
    {
        // Find local player's managers only (Photon multiplayer support)
        playerHealthManager = FindLocalPlayerHealthManager();
        playerKnockback = FindLocalPlayerKnockbackManager();
    }

    private PlayerHealthManager FindLocalPlayerHealthManager()
    {
        // Try "Player" tag first (multiplayer spawn)
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Player"))
        {
            PhotonView pv = go.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                return go.GetComponent<PlayerHealthManager>();
            }
        }

        // Fallback to "PlayerEntity" tag (test scenes)
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
        {
            PhotonView pv = go.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                return go.GetComponent<PlayerHealthManager>();
            }
        }

        return null;
    }

    private PlayerKnockbackManager FindLocalPlayerKnockbackManager()
    {
        // Try "Player" tag first (multiplayer spawn)
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Player"))
        {
            PhotonView pv = go.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                return go.GetComponent<PlayerKnockbackManager>();
            }
        }

        // Fallback to "PlayerEntity" tag (test scenes)
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
        {
            PhotonView pv = go.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                return go.GetComponent<PlayerKnockbackManager>();
            }
        }

        return null;
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