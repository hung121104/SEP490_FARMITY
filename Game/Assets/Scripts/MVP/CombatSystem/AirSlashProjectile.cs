using UnityEngine;

/// <summary>
/// The flying slash projectile for AirSlash skill.
/// Flies straight, destroys on hit or when max range reached.
/// Distance is measured from PLAYER position, not spawn point.
/// </summary>
public class AirSlashProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float maxRange;
    private int damage;
    private float knockbackForce;
    private Transform playerTransform;
    private LayerMask enemyLayers;
    private GameObject damagePopupPrefab;

    // Track from PLAYER position, not spawn point
    private Vector3 playerStartPosition;
    private bool isInitialized = false;
    private Rigidbody2D rb;

    #region Unity Lifecycle

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Measure distance from PLAYER start position
        // This matches exactly where the arrow tip is
        float distanceTravelled = Vector3.Distance(playerStartPosition, transform.position);
        if (distanceTravelled >= maxRange)
        {
            DestroyProjectile();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInitialized) return;

        if (((1 << other.gameObject.layer) & enemyLayers) != 0)
        {
            HitEnemy(other);
            DestroyProjectile();
        }
    }

    #endregion

    #region Initialization

    public void Initialize(
        Vector3 dir,
        float spd,
        float range,
        int dmg,
        float kbForce,
        Transform player,
        LayerMask layers,
        GameObject popupPrefab)
    {
        direction = dir.normalized;
        speed = spd;
        maxRange = range;
        damage = dmg;
        knockbackForce = kbForce;
        playerTransform = player;
        enemyLayers = layers;
        damagePopupPrefab = popupPrefab;

        // Store PLAYER position as start, not firePoint position
        playerStartPosition = player.position;
        isInitialized = true;

        if (rb != null)
            rb.linearVelocity = direction * speed;
        else
            Debug.LogWarning("AirSlashProjectile: No Rigidbody2D found!");
    }

    #endregion

    #region Hit & Destroy

    private void HitEnemy(Collider2D enemy)
    {
        EnemiesHealth enemyHealth = enemy.GetComponent<EnemiesHealth>();
        if (enemyHealth != null)
            enemyHealth.ChangeHealth(-damage);

        EnemyKnockback enemyKnockback = enemy.GetComponent<EnemyKnockback>();
        if (enemyKnockback != null && playerTransform != null)
            enemyKnockback.Knockback(playerTransform, knockbackForce);

        ShowDamagePopup(enemy.transform.position);
    }

    private void ShowDamagePopup(Vector3 position)
    {
        if (damagePopupPrefab == null) return;

        Vector3 spawnPos = position + Vector3.up * 0.8f;
        GameObject popup = Instantiate(damagePopupPrefab, spawnPos, Quaternion.identity);
        popup.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = damage.ToString();
    }

    private void DestroyProjectile()
    {
        Destroy(gameObject);
    }

    #endregion
}