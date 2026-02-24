using UnityEngine;
using TMPro;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    public Transform attackPoint;
    public LayerMask enemyLayers;
    public GameObject damagePopupPrefab;
    public Animator anim;

    [HideInInspector] public bool blockAttackDamage = false;

    private float timer;
    private StatsManager statsManager;

    #region Unity Lifecycle

    private void Start()
    {
        statsManager = StatsManager.Instance;
        if (statsManager == null)
            statsManager = FindObjectOfType<StatsManager>();
    }

    private void Update()
    {
        if (timer > 0)
            timer -= Time.deltaTime;
    }

    #endregion

    #region Attack

    public void Attack()
    {
        if (statsManager == null) return;

        if (timer <= 0)
        {
            anim.SetBool("isAttacking", true);
            timer = statsManager.cooldownTime;
        }
    }

    public void DealDamage()
    {
        if (statsManager == null) return;
        if (blockAttackDamage) return;
        if (attackPoint == null) return;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            attackPoint.position,
            statsManager.attackRange,
            enemyLayers
        );

        foreach (Collider2D enemy in hitEnemies)
        {
            EnemiesHealth enemyHealth = enemy.GetComponent<EnemiesHealth>();
            if (enemyHealth != null)
            {
                int damageDealt = statsManager.GetAttackDamage();
                enemyHealth.ChangeHealth(-damageDealt);

                EnemyKnockback enemyKnockback = enemy.GetComponent<EnemyKnockback>();
                if (enemyKnockback != null)
                    enemyKnockback.Knockback(transform, statsManager.knockbackForce);

                if (damagePopupPrefab != null)
                {
                    Vector3 spawnPos = enemy.transform.position + Vector3.up * 0.8f;
                    GameObject damagePopup = Instantiate(damagePopupPrefab, spawnPos, Quaternion.identity);
                    damagePopup.GetComponentInChildren<TMP_Text>().text = damageDealt.ToString();
                }
            }
        }
    }

    public void StopAttack()
    {
        anim.SetBool("isAttacking", false);
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        float range = statsManager != null ? statsManager.attackRange : 0f;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, range);
    }

    #endregion
}
