using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Attached to each Slash VFX prefab.
/// Handles hitbox detection, damage dealing and self destroy when animation ends.
/// </summary>
public class SlashHitbox : MonoBehaviour
{
    #region Private Fields

    private int damage;
    private float knockbackForce;
    private LayerMask enemyLayers;
    private Transform ownerTransform;
    private GameObject damagePopupPrefab;
    private PolygonCollider2D hitCollider;
    private HashSet<Collider2D> alreadyHit = new HashSet<Collider2D>();
    private Animator anim;
    private bool isActive = false;

    #endregion

    #region Initialization

    public void Initialize(
        int damage,
        float knockbackForce,
        LayerMask enemyLayers,
        Transform ownerTransform,
        GameObject damagePopupPrefab,
        float animationDuration)
    {
        this.damage = damage;
        this.knockbackForce = knockbackForce;
        this.enemyLayers = enemyLayers;
        this.ownerTransform = ownerTransform;
        this.damagePopupPrefab = damagePopupPrefab;

        hitCollider = GetComponent<PolygonCollider2D>();
        anim = GetComponent<Animator>();

        isActive = true;
        StartCoroutine(DestroyAfterAnimation(animationDuration));
    }

    #endregion

    #region Unity Lifecycle

    private void Update()
    {
        if (!isActive) return;

        CheckHits();
    }

    #endregion

    #region Hit Detection

    private void CheckHits()
    {
        if (hitCollider == null) return;

        // Get all enemies overlapping with polygon collider
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(enemyLayers);
        filter.useTriggers = true;

        List<Collider2D> hits = new List<Collider2D>();
        hitCollider.Overlap(filter, hits);

        foreach (Collider2D hit in hits)
        {
            if (alreadyHit.Contains(hit)) continue;

            alreadyHit.Add(hit);
            DamageEnemy(hit);
        }
    }

    private void DamageEnemy(Collider2D enemy)
    {
        EnemiesHealth enemyHealth = enemy.GetComponent<EnemiesHealth>();
        if (enemyHealth != null)
            enemyHealth.ChangeHealth(-damage);

        EnemyKnockback enemyKnockback = enemy.GetComponent<EnemyKnockback>();
        if (enemyKnockback != null && ownerTransform != null)
            enemyKnockback.Knockback(ownerTransform, knockbackForce);

        ShowDamagePopup(enemy.transform.position);
    }

    private void ShowDamagePopup(Vector3 position)
    {
        if (damagePopupPrefab == null) return;

        Vector3 spawnPos = position + Vector3.up * 0.8f;
        GameObject popup = Instantiate(damagePopupPrefab, spawnPos, Quaternion.identity);
        popup.GetComponentInChildren<TMP_Text>().text = damage.ToString();
    }

    #endregion

    #region Lifecycle

    private IEnumerator DestroyAfterAnimation(float duration)
    {
        yield return new WaitForSeconds(duration);

        isActive = false;
        Destroy(gameObject);
    }

    #endregion
}