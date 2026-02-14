using UnityEngine;
using TMPro;

public class PlayerCombat : MonoBehaviour
{
    public Transform attackPoint;
    public float attackRange = 1;
    public float knockbackForce = 50;
    public LayerMask enemyLayers;
    public int attackDamage = 1;
    public GameObject damagePopupPrefab;
    
    public Animator anim;

    public float cooldownTime;
    private float timer;

    private SpriteRenderer spriteRenderer;
    private float originalAttackPointX; 


    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        
        if (attackPoint != null)
        {
            originalAttackPointX = attackPoint.localPosition.x;
        }
    }

    private void Update()
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;
        }

        
        if (attackPoint != null && spriteRenderer != null)
        {
            Vector3 attackPos = attackPoint.localPosition;
            
            
            if (spriteRenderer.flipX)
            {
                attackPos.x = -Mathf.Abs(originalAttackPointX);
            }
            else 
            {
                attackPos.x = Mathf.Abs(originalAttackPointX);
            }
            
            attackPoint.localPosition = attackPos;
        }
    }

    public void Attack()
    {
        if (timer <= 0)
        {
            anim.SetBool("isAttacking", true);
            timer = cooldownTime;
        }
       
    }

    public void DealDamage()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            EnemiesHealth enemyHealth = enemy.GetComponent<EnemiesHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.ChangeHealth(-attackDamage);

                // Knockback
                EnemyKnockback enemyKnockback = enemy.GetComponent<EnemyKnockback>();
                if (enemyKnockback != null)
                {
                    enemyKnockback.Knockback(transform, knockbackForce);
                }

                // Damage popup above enemy
                if (damagePopupPrefab != null)
                {
                    Vector3 spawnPos = enemy.transform.position + Vector3.up * 0.8f;
                    GameObject damagePopup = Instantiate(damagePopupPrefab, spawnPos, Quaternion.identity);
                    damagePopup.GetComponentInChildren<TMP_Text>().text = attackDamage.ToString();
                }
            }
        }
    }

    public void StopAttack()
    {
        anim.SetBool("isAttacking", false);
    }


    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
