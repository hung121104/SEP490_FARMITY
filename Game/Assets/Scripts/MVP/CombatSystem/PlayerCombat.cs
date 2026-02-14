using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public Transform attackPoint;
    public float attackRange = 1;
    public LayerMask enemyLayers;
    public int attackDamage = 1;

    public Animator anim;

    public float cooldownTime;
    private float timer;

    private SpriteRenderer spriteRenderer;
    private float originalAttackPointX; // Store the original local X position


    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Store the original local X position of the attack point
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

        // Flip attack point based on sprite direction
        if (attackPoint != null && spriteRenderer != null)
        {
            Vector3 attackPos = attackPoint.localPosition;
            
            // If sprite is flipped (facing left), mirror the X position
            if (spriteRenderer.flipX)
            {
                attackPos.x = -Mathf.Abs(originalAttackPointX);
            }
            else // Facing right
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
