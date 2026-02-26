using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// The flying slash projectile for AirSlash skill.
/// Flies straight, destroys on hit or when max range reached.
/// Uses OverlapCircle for hit detection - same as DoubleStrike.
/// </summary>
public class AirSlashProjectile : MonoBehaviour
{
    #region Private Fields

    private Vector3 direction;
    private float speed;
    private float maxRange;
    private int damage;
    private float knockbackForce;
    private Transform playerTransform;
    private LayerMask enemyLayers;
    private GameObject damagePopupPrefab;
    private float hitRadius = 0.5f;

    private Vector3 spawnPosition;
    private bool isInitialized = false;
    private bool isDestroyed = false;

    private HashSet<Collider2D> alreadyHit = new HashSet<Collider2D>();

    #endregion

    #region Unity Lifecycle

    private void Update()
    {
        if (!isInitialized || isDestroyed) return;

        transform.position += direction * speed * Time.deltaTime;

        float distanceTravelled = Vector3.Distance(spawnPosition, transform.position);
        if (distanceTravelled >= maxRange)
        {
            DestroyProjectile();
            return;
        }

        CheckHits();
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
        GameObject popupPrefab,
        float hitDetectionRadius = 0.5f)
    {
        direction = dir.normalized;
        speed = spd;
        maxRange = range;
        damage = dmg;
        knockbackForce = kbForce;
        playerTransform = player;
        enemyLayers = layers;
        damagePopupPrefab = popupPrefab;
        hitRadius = hitDetectionRadius;

        spawnPosition = transform.position;
        isInitialized = true;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    #endregion

    #region Hit Detection

    private void CheckHits()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            hitRadius,
            enemyLayers
        );

        foreach (Collider2D hit in hits)
        {
            if (alreadyHit.Contains(hit)) continue;

            alreadyHit.Add(hit);
            HitEnemy(hit);
            DestroyProjectile();
            return;
        }
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
        if (isDestroyed) return;

        isDestroyed = true;
        Destroy(gameObject);
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }

    #endregion
}