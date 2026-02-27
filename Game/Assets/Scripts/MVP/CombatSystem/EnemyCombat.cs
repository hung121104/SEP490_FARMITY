using UnityEngine;
using TMPro;
using Photon.Pun;

/// <summary>
/// Handles enemy combat - applies damage, knockback, and displays damage popups on player collision.
/// Automatically finds and uses global PlayerHealthManager and PlayerKnockbackManager from CombatSystem.
/// Works with multiplayer - finds local player using Photon tags.
/// </summary>
public class EnemyCombat : MonoBehaviour
{
    #region Serialized Fields

    [Header("Damage Settings")]
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float knockbackForce = 30f;
    [SerializeField] private GameObject damagePopupPrefab;

    [Header("Cooldown")]
    [SerializeField] private float damageThrottleTime = 0.5f;

    #endregion

    #region Public Properties

    public GameObject DamagePopupPrefab => damagePopupPrefab;

    #endregion

    #region Private Fields

    private PlayerHealthManager playerHealthManager;
    private PlayerKnockbackManager playerKnockback;
    private Rigidbody2D rb;
    private Collider2D col;
    private float lastDamageTime = -999f;
    private bool isInitialized = false;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeComponents();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isInitialized || playerHealthManager == null)
            return;

        if (!IsCollidingWithPlayer(collision))
            return;

        if (!CanDealDamage())
            return;

        ApplyDamageToPlayer(collision);
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // Ensure collider is not a trigger
        if (col != null && col.isTrigger)
            col.isTrigger = false;

        // Find local player
        GameObject playerObj = FindLocalPlayerEntity();
        if (playerObj == null)
        {
            Debug.LogError($"[{GetType().Name}] Local player not found!");
            enabled = false;
            return;
        }

        // Get global CombatSystem managers
        playerHealthManager = FindObjectOfType<PlayerHealthManager>();
        playerKnockback = FindObjectOfType<PlayerKnockbackManager>();

        if (playerHealthManager == null)
        {
            Debug.LogError($"[{GetType().Name}] PlayerHealthManager not found in scene!");
            enabled = false;
            return;
        }

        isInitialized = true;
    }

    #endregion

    #region Player Detection

    private GameObject FindLocalPlayerEntity()
    {
        // Try "Player" tag first (multiplayer spawn)
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Player"))
        {
            PhotonView pv = go.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
                return go;
        }

        // Fallback to "PlayerEntity" tag (test scenes)
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
        {
            PhotonView pv = go.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
                return go;
        }

        return null;
    }

    private bool IsCollidingWithPlayer(Collision2D collision)
    {
        return collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("PlayerEntity");
    }

    #endregion

    #region Damage Logic

    private bool CanDealDamage()
    {
        // Check throttle
        if (Time.time - lastDamageTime < damageThrottleTime)
            return false;

        // Check invulnerability
        if (playerHealthManager.IsInvulnerable)
            return false;

        return true;
    }

    private void ApplyDamageToPlayer(Collision2D collision)
    {
        lastDamageTime = Time.time;

        // Apply damage
        playerHealthManager.ChangeHealth(-damageAmount);

        // Apply knockback
        if (playerKnockback != null)
            playerKnockback.Knockback(transform, knockbackForce);

        // Show damage popup
        CreateDamagePopup(collision.transform.position);
    }

    private void CreateDamagePopup(Vector3 position)
    {
        if (damagePopupPrefab == null)
            return;

        Vector3 spawnPos = position + Vector3.up;
        GameObject popup = Instantiate(damagePopupPrefab, spawnPos, Quaternion.identity);

        TMP_Text damageText = popup.GetComponentInChildren<TMP_Text>();
        if (damageText != null)
            damageText.text = damageAmount.ToString();
    }

    #endregion
}